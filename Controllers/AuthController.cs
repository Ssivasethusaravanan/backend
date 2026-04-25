using System.Security.Claims;
using identity_service.Models;
using identity_service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace identity_service.Controllers;

/// <summary>
/// Email-link sign-in endpoints.
///
/// Flow:
///   1. POST /identity/auth/login        — generate a Firebase email sign-in link and email it
///   2. (user clicks the link in email — Flutter app intercepts the deep link)
///   3. POST /identity/auth/verify-email-link — server verifies the oobCode, issues JWT tokens
///   4. POST /identity/auth/refresh      — rotate access + refresh tokens
///   5. POST /identity/auth/logout       — blocklist the refresh token
/// </summary>
[Route("identity/auth")]
public class AuthController(
    AuthEmailService authEmailService,
    IJwtTokenService jwtTokenService,
    ILogger<AuthController> logger) : BaseApiController
{
    // ── POST /identity/auth/login ──────────────────────────────────────────────

    /// <summary>
    /// Generates a Firebase email sign-in link and sends it to the user.
    /// Does NOT reveal whether the email is registered or not (security best practice).
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("standard")]
    [ProducesResponseType(typeof(ApiSuccessResponse<MessageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        try
        {
            await authEmailService.SendSignInLinkAsync(request.Email, ct);
        }
        catch (Exception ex)
        {
            // Swallow silently to prevent email enumeration attacks
            logger.LogError(ex, "Failed to send sign-in link to {Email}.", request.Email);
        }

        return ApiOk(new MessageResponse
        {
            Message = "If an account with that email exists, a sign-in link has been sent. Please check your inbox."
        });
    }

    // ── POST /identity/auth/verify-email-link ──────────────────────────────────

    /// <summary>
    /// Verifies the oobCode extracted from the Firebase email link.
    /// Returns access and refresh JWT tokens alongside the user profile.
    /// </summary>
    [HttpPost("verify-email-link")]
    [EnableRateLimiting("standard")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyEmailLink(
        [FromBody] VerifyEmailLinkRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await authEmailService.VerifySignInLinkAsync(request.Email, request.OobCode, ct);

            var accessToken  = jwtTokenService.GenerateAccessToken(result.User);
            var refreshToken = jwtTokenService.GenerateRefreshToken();

            logger.LogInformation(
                "User {UserId} authenticated via email link. IsNewUser={IsNew}.",
                result.User.Id, result.IsNewUser);

            return ApiOk(new AuthTokenResponse
            {
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
                User         = result.User,
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Email link verification failed for {Email}.", request.Email);
            return ApiUnauthorized("The sign-in link is invalid or has expired. Please request a new one.");
        }
    }

    // ── POST /identity/auth/refresh ────────────────────────────────────────────

    /// <summary>
    /// Issues a new access + refresh token pair if the supplied refresh token is not blocklisted.
    /// The old refresh token is immediately blocklisted (single-use rotation).
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("standard")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        if (await jwtTokenService.IsRefreshTokenBlockedAsync(request.RefreshToken, ct))
        {
            logger.LogWarning("Blocked refresh token used. Possible token theft.");
            return ApiUnauthorized("The refresh token is no longer valid.");
        }

        // Blocklist the old token immediately (single-use rotation anti-replay)
        await jwtTokenService.BlockRefreshTokenAsync(request.RefreshToken, ct);

        // In a full implementation, you'd decode the refresh token to get the user,
        // then re-fetch their profile. For simplicity we read the current authenticated user.
        // The access token must still be valid to call this endpoint.
        var userId    = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        var userRole  = User.FindFirstValue(ClaimTypes.Role);

        if (userId is null || userEmail is null)
            return ApiUnauthorized("Cannot identify user from token.");

        var userDto = new UserDto
        {
            Id    = userId,
            Email = userEmail,
            Name  = User.FindFirstValue("name") ?? userEmail,
            Role  = userRole ?? "customer",
        };

        var newAccess  = jwtTokenService.GenerateAccessToken(userDto);
        var newRefresh = jwtTokenService.GenerateRefreshToken();

        return ApiOk(new AuthTokenResponse
        {
            AccessToken  = newAccess,
            RefreshToken = newRefresh,
            User         = userDto,
        });
    }

    // ── POST /identity/auth/logout ─────────────────────────────────────────────

    /// <summary>
    /// Blocklists the supplied refresh token.
    /// The client must also discard its access token locally.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiSuccessResponse<MessageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        await jwtTokenService.BlockRefreshTokenAsync(request.RefreshToken, ct);

        logger.LogInformation("User {UserId} logged out.", User.FindFirstValue(ClaimTypes.NameIdentifier));

        return ApiOk(new MessageResponse { Message = "Logged out successfully." });
    }
}
