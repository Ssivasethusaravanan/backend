namespace identity_service.Entities;

/// <summary>
/// Persistent user record in PostgreSQL.
/// Maps to the 'users' table that stores profile data keyed by Firebase UID.
/// </summary>
public class UserEntity
{
    public Guid Id { get; set; }

    /// <summary>Firebase Authentication UID (sub claim from Firebase ID token).</summary>
    public required string FirebaseUid { get; set; }

    public required string Email { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Role — e.g. admin, provider, customer, waiter, kitchen, delivery.</summary>
    public string Role { get; set; } = "customer";

    public string? TenantId { get; set; }

    public string? AvatarUrl { get; set; }

    /// <summary>Firebase Cloud Messaging token for push notifications.</summary>
    public string? FcmToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
