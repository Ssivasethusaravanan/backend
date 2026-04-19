using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using identity_service.Constants;
using identity_service.Data;
using identity_service.Middleware;
using identity_service.Models;
using identity_service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Load .env file (local development secrets) ----------
var envFile = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            continue;

        var parts = line.Split('=', 2);
        if (parts.Length == 2 && !string.IsNullOrEmpty(parts[1]))
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
    }
}

// ---------- Serilog (async file + console logging) ----------
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ---------- Database (shared NpgsqlDataSource) ----------
var connectionString = Environment.GetEnvironmentVariable("POSTGRES_DB_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Database connection string not configured. Set the DATABASE_URL environment variable.");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
var dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource));

// ---------- Services (DI) ----------
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ---------- Distributed Cache (Upstash REST API → fallback: in-memory) ----------
var upstashUrl = Environment.GetEnvironmentVariable("UPSTASH_REDIS_REST_URL")
    ?? builder.Configuration["Redis:RestUrl"];

var upstashToken = Environment.GetEnvironmentVariable("UPSTASH_REDIS_REST_TOKEN")
    ?? builder.Configuration["Redis:RestToken"];

var instanceName = builder.Configuration["Redis:InstanceName"] ?? "dotnetapi:";

if (!string.IsNullOrEmpty(upstashUrl) && !string.IsNullOrEmpty(upstashToken))
{
    builder.Services.AddSingleton<IDistributedCache>(
        new UpstashDistributedCache(upstashUrl, upstashToken, instanceName));
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// ---------- JWT Authentication ----------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT signing key is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ReadAccess", policy => policy.RequireAuthenticatedUser());
});

// ---------- CORS ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowTrustedOrigins", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ---------- Rate Limiting (Token Bucket) ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Standard tier: 100 requests/minute
    options.AddPolicy("standard", httpContext =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = 100,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Premium tier: 1000 requests/minute
    options.AddPolicy("premium", httpContext =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1000,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = 1000,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // RFC 7807 error response for rate limit exceeded
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var response = new ApiErrorResponse
        {
            Error = new ApiError
            {
                Type = ErrorTypes.TooManyRequests,
                Title = "Too Many Requests",
                Status = 429,
                Detail = "Rate limit exceeded. Please try again later.",
                Instance = context.HttpContext.Request.Path
            },
            Timestamp = DateTime.UtcNow.ToString("o"),
            RequestId = context.HttpContext.TraceIdentifier
        };

        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken: cancellationToken);
    };
});

// ---------- Controllers & JSON ----------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ---------- Validation error response (RFC 7807) ----------
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(err => new FieldError
            {
                Field = e.Key,
                Message = err.ErrorMessage,
                Code = "VALIDATION_ERROR"
            }))
            .ToList();

        var response = new ApiErrorResponse
        {
            Error = new ApiError
            {
                Type = ErrorTypes.Validation,
                Title = "Validation Error",
                Status = 422,
                Detail = "The request body contains invalid fields",
                Instance = context.HttpContext.Request.Path,
                Errors = errors
            },
            Timestamp = DateTime.UtcNow.ToString("o"),
            RequestId = context.HttpContext.TraceIdentifier
        };

        return new UnprocessableEntityObjectResult(response);
    };
});

// ---------- OpenAPI ----------
builder.Services.AddOpenApi();

// ---------- Response Compression ----------
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);

// ---------- Health Checks ----------
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString);

// ---------- Request Timeouts ----------
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
});

// ---------- Output Caching ----------
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy.NoCache());
    options.AddPolicy("CachePublicGet", policy =>
        policy.Expire(TimeSpan.FromSeconds(60)).Tag("public"));
});

// ---------- Suppress detailed errors in production ----------
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddProblemDetails();
}

var app = builder.Build();

// ---------- Middleware Pipeline ----------

// 1. Request ID (must be first to tag all logs)
app.UseMiddleware<RequestIdMiddleware>();

// 2. Global exception handler
app.UseMiddleware<GlobalExceptionMiddleware>();

// 3. Response compression (early to compress all downstream output)
app.UseResponseCompression();

// 4. Request logging
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    await next();
});

app.UseHttpsRedirection();
app.UseCors("AllowTrustedOrigins");
app.UseRateLimiter();
app.UseRequestTimeouts();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("standard");
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

// Make the implicit Program class accessible for WebApplicationFactory<Program>
public partial class Program { }
