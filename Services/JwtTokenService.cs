using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using identity_service.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;

namespace identity_service.Services;

/// <summary>
/// Issues HS256 access tokens (15 min) and opaque refresh tokens (7 days).
/// Refresh tokens are tracked in the distributed cache and can be blocklisted on logout.
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(UserDto user);
    string GenerateRefreshToken();
    Task<bool> IsRefreshTokenBlockedAsync(string refreshToken, CancellationToken ct = default);
    Task BlockRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}

public class JwtTokenService(
    IConfiguration configuration,
    IDistributedCache cache,
    ILogger<JwtTokenService> logger) : IJwtTokenService
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);
    private const string BlocklistPrefix = "blocklist:refresh:";

    // ── Access Token ───────────────────────────────────────────────────────────

    public string GenerateAccessToken(UserDto user)
    {
        var key = GetSigningKey();
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.Role,               user.Role),
        };

        if (!string.IsNullOrEmpty(user.TenantId))
            claims.Add(new Claim("tenant_id", user.TenantId));

        var token = new JwtSecurityToken(
            issuer:             configuration["Jwt:Issuer"],
            audience:           configuration["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.Add(AccessTokenLifetime),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Refresh Token ──────────────────────────────────────────────────────────

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    // ── Blocklist ──────────────────────────────────────────────────────────────

    public async Task<bool> IsRefreshTokenBlockedAsync(string refreshToken, CancellationToken ct = default)
    {
        var cached = await cache.GetStringAsync(BlocklistKey(refreshToken), ct);
        return cached is not null;
    }

    public async Task BlockRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        logger.LogInformation("Blocklisting refresh token.");
        await cache.SetStringAsync(
            BlocklistKey(refreshToken),
            "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = RefreshTokenLifetime },
            ct);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private SymmetricSecurityKey GetSigningKey()
    {
        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT signing key is not configured.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    }

    private static string BlocklistKey(string token) => $"{BlocklistPrefix}{token}";
}
