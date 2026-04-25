using System.ComponentModel.DataAnnotations;

namespace identity_service.Models;

// ── Requests ──────────────────────────────────────────────────────────────

/// <summary>POST /identity/auth/login — sends a sign-in email link.</summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    public required string Email { get; init; }
}

/// <summary>POST /identity/auth/verify-email-link — exchanges the oobCode for JWT tokens.</summary>
public class VerifyEmailLinkRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    public required string Email { get; init; }

    /// <summary>
    /// The one-time code extracted from the Firebase email-link URL.
    /// The Flutter app reads this from the deep-link URL query parameter 'oobCode'.
    /// </summary>
    [Required(ErrorMessage = "oobCode is required.")]
    public required string OobCode { get; init; }
}

/// <summary>POST /identity/auth/refresh — issues a new access/refresh token pair.</summary>
public class RefreshTokenRequest
{
    [Required(ErrorMessage = "refresh_token is required.")]
    public required string RefreshToken { get; init; }
}
