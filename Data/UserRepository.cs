using identity_service.Entities;
using Microsoft.EntityFrameworkCore;

namespace identity_service.Data;

/// <summary>
/// EF Core implementation of IUserRepository.
/// All writes set UpdatedAt explicitly so the PostgreSQL trigger acts as a safety net.
/// </summary>
public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<UserEntity?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default)
        => db.Users.AsNoTracking()
              .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid, ct);

    public Task<UserEntity?> GetByEmailAsync(string email, CancellationToken ct = default)
        => db.Users.AsNoTracking()
              .FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<UserEntity> CreateAsync(UserEntity user, CancellationToken ct = default)
    {
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<UserEntity> UpdateAsync(UserEntity user, CancellationToken ct = default)
    {
        user.UpdatedAt = DateTime.UtcNow;
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdateFcmTokenAsync(Guid userId, string? fcmToken, CancellationToken ct = default)
    {
        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(u => u.FcmToken, fcmToken)
                 .SetProperty(u => u.UpdatedAt, DateTime.UtcNow), ct);
    }

    public async Task<List<string>> GetFcmTokensByUserIdsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        return await db.Users
            .Where(u => userIds.Contains(u.Id) && u.FcmToken != null)
            .Select(u => u.FcmToken!)
            .ToListAsync(ct);
    }

    public async Task ClearFcmTokenAsync(string fcmToken, CancellationToken ct = default)
    {
        await db.Users
            .Where(u => u.FcmToken == fcmToken)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(u => u.FcmToken, (string?)null)
                 .SetProperty(u => u.UpdatedAt, DateTime.UtcNow), ct);
    }
}
