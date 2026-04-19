namespace identity_service.Models;

public class QueryParameters
{
    private int? _page;
    private int? _limit;

    public int? Page
    {
        get => _page ?? 1;
        set => _page = value is > 0 ? value : 1;
    }

    public int? Limit
    {
        get => _limit ?? 20;
        set => _limit = value is > 0 and <= 100 ? value : 20;
    }

    public string? Cursor { get; set; }
    public string? Sort { get; set; }
}

public class CategoryQueryParameters : QueryParameters
{
    public string? Name { get; set; }
    public string? Code { get; set; }
}

public class SubCategoryQueryParameters : QueryParameters
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public int? CategoryId { get; set; }
}
