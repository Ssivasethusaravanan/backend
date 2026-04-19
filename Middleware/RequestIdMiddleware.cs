namespace identity_service.Middleware;

public class RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = $"req_{Guid.NewGuid():N}";
        context.TraceIdentifier = requestId;
        context.Response.Headers["X-Request-Id"] = requestId;

        logger.LogDebug("Assigned RequestId {RequestId} to {Method} {Path}",
            requestId, context.Request.Method, context.Request.Path);

        await next(context);
    }
}
