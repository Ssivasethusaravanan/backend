using System.Text.Json;
using identity_service.Constants;
using identity_service.Models;

namespace identity_service.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path} | RequestId: {RequestId}",
                context.Request.Method, context.Request.Path, context.TraceIdentifier);

            if (!context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var response = new ApiErrorResponse
                {
                    Error = new ApiError
                    {
                        Type = ErrorTypes.InternalServerError,
                        Title = "Internal Server Error",
                        Status = 500,
                        Detail = "An unexpected error occurred while processing your request.",
                        Instance = context.Request.Path
                    },
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    RequestId = context.TraceIdentifier
                };

                await context.Response.WriteAsJsonAsync(response, JsonOptions);
            }
        }
    }
}
