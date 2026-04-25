using identity_service.Entities;

namespace identity_service.Data;

/// <summary>
/// Persistent user storage contract — CRUD operations keyed by Firebase UID.
/// </summary>
public interface IUserRepository
{
    Task<UserEntity?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default);
    Task<UserEntity?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<UserEntity> CreateAsync(UserEntity user, CancellationToken ct = default);
    Task<UserEntity> UpdateAsync(UserEntity user, CancellationToken ct = default);

    /// <summary>Stores or replaces the stored FCM token for a user.</summary>
    Task UpdateFcmTokenAsync(Guid userId, string? fcmToken, CancellationToken ct = default);

    /// <summary>Retrieves FCM tokens for a batch of internal user IDs (for multicast notifications).</summary>
    Task<List<string>> GetFcmTokensByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);

    /// <summary>Clears a stale / revoked FCM token across all user records.</summary>
    Task ClearFcmTokenAsync(string fcmToken, CancellationToken ct = default);
}
