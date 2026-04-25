namespace identity_service.Constants;

public static class ErrorTypes
{
    public const string NotFound            = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
    public const string Validation          = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
    public const string InternalServerError = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
    public const string TooManyRequests     = "https://tools.ietf.org/html/rfc6585#section-4";
    public const string Unauthorized        = "https://tools.ietf.org/html/rfc7235#section-3.1";
    public const string Forbidden           = "https://tools.ietf.org/html/rfc7231#section-6.5.3";
    public const string Conflict            = "https://tools.ietf.org/html/rfc7231#section-6.5.8";
    public const string BadRequest          = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
}
