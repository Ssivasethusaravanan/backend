using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace identity_service.Services;

/// <summary>
/// Bootstraps the Firebase Admin SDK singleton at application startup.
/// Reads credentials from a service account JSON file or falls back to
/// Application Default Credentials (useful in Cloud Run / GKE).
/// </summary>
public class FirebaseInitializer
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FirebaseInitializer> _logger;

    public FirebaseInitializer(IConfiguration configuration, ILogger<FirebaseInitializer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void Initialize()
    {
        try
        {
            if (FirebaseApp.DefaultInstance != null)
            {
                _logger.LogInformation("FirebaseApp is already initialized.");
                return;
            }

            var serviceAccountPath =
                Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_KEY_PATH")
                ?? _configuration["Firebase:ServiceAccountKeyPath"];

            var projectId =
                Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")
                ?? _configuration["Firebase:ProjectId"];

            GoogleCredential credential;

            if (!string.IsNullOrEmpty(serviceAccountPath) && File.Exists(serviceAccountPath))
            {
                credential = GoogleCredential.FromFile(serviceAccountPath);
                _logger.LogInformation("FirebaseApp: using service account key file at {Path}.", serviceAccountPath);
            }
            else
            {
                credential = GoogleCredential.GetApplicationDefault();
                _logger.LogWarning("Firebase service account key file not found at '{Path}'. " +
                                   "Falling back to Application Default Credentials.", serviceAccountPath);
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = credential,
                ProjectId = projectId
            });

            _logger.LogInformation("FirebaseApp initialized successfully for project '{ProjectId}'.", projectId);
        }
        catch (Exception ex)
        {
            // Log but don't crash — callers will get FirebaseException if they try to use Firebase APIs
            _logger.LogError(ex, "Failed to initialize FirebaseApp. Firebase features will be unavailable.");
        }
    }
}
