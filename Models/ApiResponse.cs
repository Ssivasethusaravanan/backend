namespace identity_service.Models;

public class ApiSuccessResponse<T>
{
    public bool Success { get; init; } = true;
    public T? Data { get; init; }
    public PaginationMeta? Meta { get; init; }
    public string Timestamp { get; init; } = DateTime.UtcNow.ToString("o");
    public string RequestId { get; init; } = string.Empty;
}

public class ApiErrorResponse
{
    public bool Success { get; init; } = false;
    public required ApiError Error { get; init; }
    public string Timestamp { get; init; } = DateTime.UtcNow.ToString("o");
    public string RequestId { get; init; } = string.Empty;
}

public class ApiError
{
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required int Status { get; init; }
    public required string Detail { get; init; }
    public required string Instance { get; init; }
    public List<FieldError>? Errors { get; init; }
}

public class FieldError
{
    public required string Field { get; init; }
    public required string Message { get; init; }
    public required string Code { get; init; }
}

public class PaginationMeta
{
    public int Page { get; init; }
    public int Limit { get; init; }
    public int Total { get; init; }
    public string? Cursor { get; init; }
    public bool HasMore { get; init; }
}
