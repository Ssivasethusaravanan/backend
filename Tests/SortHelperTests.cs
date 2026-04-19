using identity_service.Helpers;

namespace identity_service.Tests;

public class SortHelperTests
{
    private static readonly HashSet<string> AllowedColumns = ["id", "name", "code", "created_at"];

    [Fact]
    public void BuildOrderByClause_NullSort_ReturnsDefault()
    {
        var result = SortHelper.BuildOrderByClause(null, AllowedColumns);

        Assert.Equal("name ASC", result);
    }

    [Fact]
    public void BuildOrderByClause_EmptySort_ReturnsDefault()
    {
        var result = SortHelper.BuildOrderByClause("", AllowedColumns);

        Assert.Equal("name ASC", result);
    }

    [Fact]
    public void BuildOrderByClause_WhitespaceSort_ReturnsDefault()
    {
        var result = SortHelper.BuildOrderByClause("   ", AllowedColumns);

        Assert.Equal("name ASC", result);
    }

    [Fact]
    public void BuildOrderByClause_SingleColumnAsc_ReturnsCorrectClause()
    {
        var result = SortHelper.BuildOrderByClause("name:asc", AllowedColumns);

        Assert.Equal("name ASC", result);
    }

    [Fact]
    public void BuildOrderByClause_SingleColumnDesc_ReturnsCorrectClause()
    {
        var result = SortHelper.BuildOrderByClause("name:desc", AllowedColumns);

        Assert.Equal("name DESC", result);
    }

    [Fact]
    public void BuildOrderByClause_SingleColumnNoDirection_DefaultsToAsc()
    {
        var result = SortHelper.BuildOrderByClause("code", AllowedColumns);

        Assert.Equal("code ASC", result);
    }

    [Fact]
    public void BuildOrderByClause_MultipleColumns_ReturnsCommaDelimited()
    {
        var result = SortHelper.BuildOrderByClause("name:asc,created_at:desc", AllowedColumns);

        Assert.Equal("name ASC, created_at DESC", result);
    }

    [Fact]
    public void BuildOrderByClause_DisallowedColumn_SkipsIt()
    {
        var result = SortHelper.BuildOrderByClause("evil_column:asc", AllowedColumns);

        Assert.Equal("name ASC", result); // Falls back to default
    }

    [Fact]
    public void BuildOrderByClause_MixedAllowedAndDisallowed_SkipsDisallowed()
    {
        var result = SortHelper.BuildOrderByClause("name:asc,evil:desc,code:desc", AllowedColumns);

        Assert.Equal("name ASC, code DESC", result);
    }

    [Fact]
    public void BuildOrderByClause_CustomDefault_UsesProvidedDefault()
    {
        var result = SortHelper.BuildOrderByClause(null, AllowedColumns, "id DESC");

        Assert.Equal("id DESC", result);
    }

    [Fact]
    public void BuildOrderByClause_CaseInsensitive_DescRecognized()
    {
        var result = SortHelper.BuildOrderByClause("name:DESC", AllowedColumns);

        Assert.Equal("name DESC", result);
    }

    [Fact]
    public void BuildOrderByClause_ColumnNameCaseInsensitive()
    {
        var result = SortHelper.BuildOrderByClause("NAME:asc", AllowedColumns);

        Assert.Equal("name ASC", result);
    }
}
