namespace identity_service.Models;

public class PagedResult<T>
{
    public required List<T> Items { get; init; }
    public int Total { get; init; }
    public string? Cursor { get; init; }
    public bool HasMore { get; init; }
}
