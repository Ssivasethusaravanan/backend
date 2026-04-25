using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FirebaseAdmin.Auth;
using identity_service.Data;
using identity_service.Entities;
using identity_service.Models;

namespace identity_service.Services;

/// <summary>Result of a successful email-link verification.</summary>
public record EmailLinkVerifyResult(UserDto User, bool IsNewUser);

/// <summary>
/// Handles the full Firebase email-link sign-in flow:
///   1. SendSignInLinkAsync  — generates a Firebase magic link and emails it.
///   2. VerifySignInLinkAsync — exchanges the oobCode for a Firebase ID token (via REST API),
///      verifies it with the Admin SDK, then creates/fetches the user in PostgreSQL.
///
/// Why the REST API for step 2?
///   The Firebase Admin SDK can generate email-action links but cannot directly redeem an
///   oobCode on the server (that is normally the client SDK's job). We call the Firebase
///   Auth REST endpoint signInWithEmailLink to exchange oobCode→idToken server-side,
///   then verify the returned idToken with the Admin SDK — keeping everything backend-driven.
/// </summary>
public class AuthEmailService(
    IConfiguration configuration,
    IEmailSender emailSender,
    IUserRepository userRepository,
    IHttpClientFactory httpClientFactory,
    ILogger<AuthEmailService> logger)
{
    private const string FirebaseSignInEndpoint =
        "https://identitytoolkit.googleapis.com/v1/accounts:signInWithEmailLink";

    // ── 1. Send sign-in email link ─────────────────────────────────────────────

    public async Task SendSignInLinkAsync(string email, CancellationToken ct = default)
    {
        var actionCodeSettings = BuildActionCodeSettings();

        var link = await FirebaseAuth.DefaultInstance
            .GenerateSignInWithEmailLinkAsync(email, actionCodeSettings, ct);

        logger.LogInformation("Firebase email sign-in link generated for {Email}.", email);

        var html = EmailTemplates.SignInLink(link);

        await emailSender.SendAsync(
            toEmail:  email,
            toName:   email,
            subject:  "Sign in to BillFlow",
            htmlBody: html,
            ct:       ct);

        logger.LogInformation("Sign-in link email dispatched to {Email}.", email);
    }

    // ── 2. Verify the oobCode and return a UserDto ─────────────────────────────

    public async Task<EmailLinkVerifyResult> VerifySignInLinkAsync(
        string email, string oobCode, CancellationToken ct = default)
    {
        // Exchange oobCode → Firebase ID token via REST API
        var idToken = await ExchangeOobCodeForIdTokenAsync(email, oobCode, ct);

        // Verify the ID token with the Admin SDK (validates signature, expiry, audience)
        var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, ct);

        var firebaseUid = decoded.Uid;
        logger.LogInformation("Firebase ID token verified. UID={Uid}, Email={Email}.", firebaseUid, email);

        // Create or fetch user in our PostgreSQL database
        var (userEntity, isNewUser) = await GetOrCreateUserAsync(firebaseUid, email, ct);

        var userDto = ToDto(userEntity);
        return new EmailLinkVerifyResult(userDto, isNewUser);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<string> ExchangeOobCodeForIdTokenAsync(
        string email, string oobCode, CancellationToken ct)
    {
        var apiKey = Environment.GetEnvironmentVariable("FIREBASE_WEB_API_KEY")
                     ?? configuration["Firebase:WebApiKey"]
                     ?? throw new InvalidOperationException(
                         "Firebase Web API Key is not configured. " +
                         "Set FIREBASE_WEB_API_KEY environment variable or Firebase:WebApiKey in appsettings.");

        var http = httpClientFactory.CreateClient("firebase-rest");

        var payload = new { email, oobCode };
        var response = await http.PostAsJsonAsync($"{FirebaseSignInEndpoint}?key={apiKey}", payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Firebase REST signInWithEmailLink failed ({Status}): {Body}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException("The sign-in link is invalid or has already expired.");
        }

        var result = await response.Content.ReadFromJsonAsync<FirebaseSignInResponse>(ct)
                     ?? throw new InvalidOperationException("Unexpected empty response from Firebase REST API.");

        return result.IdToken;
    }

    private async Task<(UserEntity entity, bool isNew)> GetOrCreateUserAsync(
        string firebaseUid, string email, CancellationToken ct)
    {
        var existing = await userRepository.GetByFirebaseUidAsync(firebaseUid, ct);
        if (existing is not null)
            return (existing, false);

        logger.LogInformation("Creating new user record for Firebase UID {Uid}.", firebaseUid);

        // Derive a display name from the email local-part for new users
        var displayName = email.Split('@')[0];
        displayName = char.ToUpperInvariant(displayName[0]) + displayName[1..];

        var newUser = new UserEntity
        {
            FirebaseUid = firebaseUid,
            Email       = email,
            Name        = displayName,
        };

        var created = await userRepository.CreateAsync(newUser, ct);

        // Send a welcome email asynchronously (fire-and-forget — don't block the auth response)
        _ = Task.Run(async () =>
        {
            try
            {
                await emailSender.SendAsync(email, displayName,
                    "Welcome to BillFlow 🎉",
                    EmailTemplates.Welcome(displayName));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send welcome email to {Email}.", email);
            }
        }, CancellationToken.None);

        return (created, true);
    }

    private ActionCodeSettings BuildActionCodeSettings()
    {
        var cfg = configuration.GetSection("Firebase:ActionCodeSettings");

        return new ActionCodeSettings
        {
            Url              = cfg["Url"] ?? "https://billflow.page.link/login",
            HandleCodeInApp  = bool.TryParse(cfg["HandleCodeInApp"], out var h) && h,
            AndroidPackageName     = cfg["AndroidPackageName"],
            AndroidInstallApp      = bool.TryParse(cfg["AndroidInstallApp"], out var ai) && ai,
            AndroidMinimumVersion  = cfg["AndroidMinimumVersion"],
            IosBundleId            = cfg["IosBundleId"],
        };
    }

    private static UserDto ToDto(UserEntity e) => new()
    {
        Id        = e.Id.ToString(),
        Email     = e.Email,
        Name      = e.Name,
        Role      = e.Role,
        TenantId  = e.TenantId,
        AvatarUrl = e.AvatarUrl,
    };

    // ── Internal Firebase REST response model ──────────────────────────────────

    private sealed class FirebaseSignInResponse
    {
        [JsonPropertyName("idToken")]
        public string IdToken { get; init; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; init; } = string.Empty;

        [JsonPropertyName("isNewUser")]
        public bool IsNewUser { get; init; }
    }
}
