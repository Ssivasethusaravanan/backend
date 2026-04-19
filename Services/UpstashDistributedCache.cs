using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace identity_service.Services;

/// <summary>
/// IDistributedCache implementation backed by the Upstash Redis REST API.
/// Uses HTTPS instead of the native Redis TCP protocol.
/// </summary>
public sealed class UpstashDistributedCache : IDistributedCache, IDisposable
{
    private readonly HttpClient _http;
    private readonly string _instanceName;

    public UpstashDistributedCache(string restUrl, string restToken, string instanceName)
        : this(CreateHttpClient(restUrl, restToken), instanceName) { }

    internal UpstashDistributedCache(HttpClient httpClient, string instanceName)
    {
        _http = httpClient;
        _instanceName = instanceName;
    }

    private static HttpClient CreateHttpClient(string restUrl, string restToken)
    {
        var client = new HttpClient { BaseAddress = new Uri(restUrl.TrimEnd('/')) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", restToken);
        return client;
    }

    private string PrefixKey(string key) => $"{_instanceName}{key}";

    // ───── GET ─────

    public byte[]? Get(string key) => GetAsync(key, CancellationToken.None).GetAwaiter().GetResult();

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        var body = JsonSerializer.Serialize(new[] { "GET", PrefixKey(key) });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        using var response = await _http.SendAsync(request, token);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(token), cancellationToken: token);
        var result = doc.RootElement.GetProperty("result");

        if (result.ValueKind == JsonValueKind.Null)
            return null;

        return Encoding.UTF8.GetBytes(result.GetString()!);
    }

    // ───── SET ─────

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => SetAsync(key, value, options, CancellationToken.None).GetAwaiter().GetResult();

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        var stringValue = Encoding.UTF8.GetString(value);
        var ttlSeconds = GetTtlSeconds(options);

        string[] command = ttlSeconds > 0
            ? ["SET", PrefixKey(key), stringValue, "EX", ttlSeconds.ToString()]
            : ["SET", PrefixKey(key), stringValue];

        var body = JsonSerializer.Serialize(command);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        using var response = await _http.SendAsync(request, token);
        response.EnsureSuccessStatusCode();
    }

    // ───── REMOVE ─────

    public void Remove(string key) => RemoveAsync(key, CancellationToken.None).GetAwaiter().GetResult();

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        var body = JsonSerializer.Serialize(new[] { "DEL", PrefixKey(key) });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        using var response = await _http.SendAsync(request, token);
        response.EnsureSuccessStatusCode();
    }

    // ───── REFRESH (no-op for absolute expiry) ─────

    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    // ───── Helpers ─────

    private static int GetTtlSeconds(DistributedCacheEntryOptions options)
    {
        if (options.AbsoluteExpirationRelativeToNow.HasValue)
            return (int)options.AbsoluteExpirationRelativeToNow.Value.TotalSeconds;

        if (options.AbsoluteExpiration.HasValue)
            return (int)(options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow).TotalSeconds;

        if (options.SlidingExpiration.HasValue)
            return (int)options.SlidingExpiration.Value.TotalSeconds;

        return 0;
    }

    public void Dispose() => _http.Dispose();
}
