using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using identity_service.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Testcontainers.PostgreSql;

namespace identity_service.Tests;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestJwtKey = "integration-test-signing-key-that-is-long-enough-for-hmac256";
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public HttpClient HttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var conn = new NpgsqlConnection(_container.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("""
            CREATE TABLE IF NOT EXISTS categories (
                id          SERIAL PRIMARY KEY,
                created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
                code        VARCHAR     NOT NULL UNIQUE,
                name        VARCHAR     NOT NULL UNIQUE
            );
            CREATE TABLE IF NOT EXISTS sub_categories (
                id          SERIAL PRIMARY KEY,
                created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
                code        VARCHAR     NOT NULL UNIQUE,
                name        VARCHAR     NOT NULL UNIQUE,
                category_id INT         NOT NULL REFERENCES categories(id) ON DELETE CASCADE
            );
            """, conn);
        await cmd.ExecuteNonQueryAsync();

        HttpClient = CreateClient();
        // Attach a valid JWT so authenticated endpoints work
        HttpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateTestToken());
    }

    public new async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = _container.GetConnectionString();

        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.UseSetting("Jwt:Key", TestJwtKey);
        builder.UseSetting("Jwt:Issuer", TestIssuer);
        builder.UseSetting("Jwt:Audience", TestAudience);

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            var dataSourceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(NpgsqlDataSource));
            if (dataSourceDescriptor != null)
                services.Remove(dataSourceDescriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        });
    }

    private static string GenerateTestToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: [new Claim(ClaimTypes.Name, "testuser"), new Claim(ClaimTypes.Role, "Admin")],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
