using System.Net;
using System.Text;
using identity_service.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace identity_service.Tests;

public class UpstashDistributedCacheTests : IDisposable
{
    private readonly UpstashDistributedCache _cache;
    private readonly MockHttpHandler _handler = new();

    public UpstashDistributedCacheTests()
    {
        var client = new HttpClient(_handler) { BaseAddress = new Uri("https://fake.upstash.io") };
        _cache = new UpstashDistributedCache(client, "test:");
    }

    public void Dispose() => _cache.Dispose();

    // ───── GetAsync ─────

    [Fact]
    public async Task GetAsync_CacheHit_ReturnsBytesFromResult()
    {
        _handler.Response = """{"result":"hello world"}""";

        var result = await _cache.GetAsync("mykey", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("hello world", Encoding.UTF8.GetString(result!));
        Assert.Contains("\"GET\"", _handler.LastRequestBody);
        Assert.Contains("test:mykey", _handler.LastRequestBody);
    }

    [Fact]
    public async Task GetAsync_CacheMiss_ReturnsNull()
    {
        _handler.Response = """{"result":null}""";

        var result = await _cache.GetAsync("missing", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public void Get_Sync_DelegatesToAsync()
    {
        _handler.Response = """{"result":"sync-value"}""";

        var result = _cache.Get("synckey");

        Assert.NotNull(result);
        Assert.Equal("sync-value", Encoding.UTF8.GetString(result!));
    }

    // ───── SetAsync ─────

    [Fact]
    public async Task SetAsync_WithAbsoluteExpiry_SendsSetExCommand()
    {
        _handler.Response = """{"result":"OK"}""";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
        };

        await _cache.SetAsync("mykey", Encoding.UTF8.GetBytes("data"), options, CancellationToken.None);

        Assert.Contains("\"SET\"", _handler.LastRequestBody);
        Assert.Contains("\"EX\"", _handler.LastRequestBody);
        Assert.Contains("\"60\"", _handler.LastRequestBody);
        Assert.Contains("test:mykey", _handler.LastRequestBody);
    }

    [Fact]
    public async Task SetAsync_WithAbsoluteExpirationDatetime_SendsSetExCommand()
    {
        _handler.Response = """{"result":"OK"}""";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(300)
        };

        await _cache.SetAsync("mykey", Encoding.UTF8.GetBytes("data"), options, CancellationToken.None);

        Assert.Contains("\"EX\"", _handler.LastRequestBody);
    }

    [Fact]
    public async Task SetAsync_WithSlidingExpiry_SendsSetExCommand()
    {
        _handler.Response = """{"result":"OK"}""";
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromSeconds(90)
        };

        await _cache.SetAsync("mykey", Encoding.UTF8.GetBytes("data"), options, CancellationToken.None);

        Assert.Contains("\"EX\"", _handler.LastRequestBody);
        Assert.Contains("\"90\"", _handler.LastRequestBody);
    }

    [Fact]
    public async Task SetAsync_NoExpiry_SendsSetWithoutEx()
    {
        _handler.Response = """{"result":"OK"}""";
        var options = new DistributedCacheEntryOptions();

        await _cache.SetAsync("mykey", Encoding.UTF8.GetBytes("data"), options, CancellationToken.None);

        Assert.DoesNotContain("\"EX\"", _handler.LastRequestBody);
    }

    [Fact]
    public void Set_Sync_DelegatesToAsync()
    {
        _handler.Response = """{"result":"OK"}""";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
        };

        _cache.Set("synckey", Encoding.UTF8.GetBytes("val"), options);

        Assert.Contains("test:synckey", _handler.LastRequestBody);
    }

    // ───── RemoveAsync ─────

    [Fact]
    public async Task RemoveAsync_SendsDelCommand()
    {
        _handler.Response = """{"result":1}""";

        await _cache.RemoveAsync("delkey", CancellationToken.None);

        Assert.Contains("\"DEL\"", _handler.LastRequestBody);
        Assert.Contains("test:delkey", _handler.LastRequestBody);
    }

    [Fact]
    public void Remove_Sync_DelegatesToAsync()
    {
        _handler.Response = """{"result":1}""";

        _cache.Remove("syncdelkey");

        Assert.Contains("test:syncdelkey", _handler.LastRequestBody);
    }

    // ───── Refresh (no-op) ─────

    [Fact]
    public void Refresh_DoesNothing()
    {
        _cache.Refresh("anykey"); // Should not throw
    }

    [Fact]
    public async Task RefreshAsync_DoesNothing()
    {
        await _cache.RefreshAsync("anykey", CancellationToken.None); // Should not throw
    }

    // ───── Constructor ─────

    [Fact]
    public void Constructor_WithUrlAndToken_CreatesClient()
    {
        using var cache = new UpstashDistributedCache("https://example.upstash.io", "token123", "prefix:");
        // Should not throw — validates the public constructor path
    }

    // ───── Mock HTTP Handler ─────

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        public string Response { get; set; } = """{"result":null}""";
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content != null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Response, Encoding.UTF8, "application/json")
            };
        }
    }
}
