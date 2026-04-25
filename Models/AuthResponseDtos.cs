namespace identity_service.Models;

// ── Responses ─────────────────────────────────────────────────────────────

/// <summary>Returned by verify-email-link and refresh endpoints.</summary>
public class AuthTokenResponse
{
    public required string AccessToken  { get; init; }
    public required string RefreshToken { get; init; }
    public required UserDto User        { get; init; }
}

/// <summary>Serializable user payload included in auth responses and JWT claims.</summary>
public class UserDto
{
    public required string Id        { get; init; }
    public required string Email     { get; init; }
    public required string Name      { get; init; }
    public required string Role      { get; init; }
    public string?         TenantId  { get; init; }
    public string?         AvatarUrl { get; init; }
}

/// <summary>Simple status message response for non-data endpoints (e.g. login, logout).</summary>
public class MessageResponse
{
    public required string Message { get; init; }
}
