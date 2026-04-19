namespace identity_service.Helpers;

public static class SortHelper
{
    public static string BuildOrderByClause(string? sort, HashSet<string> allowedColumns, string defaultSort = "name ASC")
    {
        if (string.IsNullOrWhiteSpace(sort))
            return defaultSort;

        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var clauses = new List<string>();

        foreach (var part in parts)
        {
            var tokens = part.Trim().Split(':');
            var column = tokens[0].Trim().ToLowerInvariant();
            var direction = tokens.Length > 1 &&
                            tokens[1].Trim().Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? "DESC"
                : "ASC";

            if (allowedColumns.Contains(column))
                clauses.Add($"{column} {direction}");
        }

        return clauses.Count > 0 ? string.Join(", ", clauses) : defaultSort;
    }
}
