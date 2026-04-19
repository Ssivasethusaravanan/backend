using identity_service.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace identity_service.Tests;

public class MiddlewareTests
{
    // ───── RequestIdMiddleware ─────

    [Fact]
    public async Task RequestIdMiddleware_SetsTraceIdentifierAndResponseHeader()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var logger = new Mock<ILogger<RequestIdMiddleware>>().Object;
        var middleware = new RequestIdMiddleware(next, logger);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.StartsWith("req_", context.TraceIdentifier);
        Assert.True(context.Response.Headers.ContainsKey("X-Request-Id"));
        Assert.Equal(context.TraceIdentifier, context.Response.Headers["X-Request-Id"].ToString());
    }

    // ───── RequestLoggingMiddleware ─────

    [Fact]
    public async Task RequestLoggingMiddleware_LogsRequestAndCallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        var nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; ctx.Response.StatusCode = 200; return Task.CompletedTask; };
        var logger = new Mock<ILogger<RequestLoggingMiddleware>>().Object;
        var middleware = new RequestLoggingMiddleware(next, logger);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_4xxStatus_LogsWarning()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        RequestDelegate next = ctx => { ctx.Response.StatusCode = 404; return Task.CompletedTask; };
        var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
        var middleware = new RequestLoggingMiddleware(next, loggerMock.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_5xxStatus_LogsError()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/test";
        RequestDelegate next = ctx => { ctx.Response.StatusCode = 500; return Task.CompletedTask; };
        var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
        var middleware = new RequestLoggingMiddleware(next, loggerMock.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
    }

    // ───── GlobalExceptionMiddleware ─────

    [Fact]
    public async Task GlobalExceptionMiddleware_NoException_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var logger = new Mock<ILogger<GlobalExceptionMiddleware>>().Object;
        var middleware = new GlobalExceptionMiddleware(next, logger);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_Exception_Returns500WithRfc7807()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/test";
        RequestDelegate next = _ => throw new InvalidOperationException("Test error");
        var logger = new Mock<ILogger<GlobalExceptionMiddleware>>().Object;
        var middleware = new GlobalExceptionMiddleware(next, logger);

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);

        // Read the response body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Internal Server Error", body);
        Assert.Contains("success", body);
    }
}
