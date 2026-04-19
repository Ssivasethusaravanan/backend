using identity_service.Models;

namespace identity_service.Tests;

public class QueryParametersTests
{
    // ───── Page ─────

    [Fact]
    public void Page_Default_Returns1()
    {
        var query = new QueryParameters();

        Assert.Equal(1, query.Page);
    }

    [Fact]
    public void Page_ValidValue_ReturnsSetValue()
    {
        var query = new QueryParameters { Page = 5 };

        Assert.Equal(5, query.Page);
    }

    [Fact]
    public void Page_Zero_ClampedTo1()
    {
        var query = new QueryParameters { Page = 0 };

        Assert.Equal(1, query.Page);
    }

    [Fact]
    public void Page_Negative_ClampedTo1()
    {
        var query = new QueryParameters { Page = -3 };

        Assert.Equal(1, query.Page);
    }

    // ───── Limit ─────

    [Fact]
    public void Limit_Default_Returns20()
    {
        var query = new QueryParameters();

        Assert.Equal(20, query.Limit);
    }

    [Fact]
    public void Limit_ValidValue_ReturnsSetValue()
    {
        var query = new QueryParameters { Limit = 50 };

        Assert.Equal(50, query.Limit);
    }

    [Fact]
    public void Limit_Zero_ClampedTo20()
    {
        var query = new QueryParameters { Limit = 0 };

        Assert.Equal(20, query.Limit);
    }

    [Fact]
    public void Limit_Negative_ClampedTo20()
    {
        var query = new QueryParameters { Limit = -5 };

        Assert.Equal(20, query.Limit);
    }

    [Fact]
    public void Limit_ExceedsMax100_ClampedTo20()
    {
        var query = new QueryParameters { Limit = 101 };

        Assert.Equal(20, query.Limit);
    }

    [Fact]
    public void Limit_Exactly100_Accepted()
    {
        var query = new QueryParameters { Limit = 100 };

        Assert.Equal(100, query.Limit);
    }

    [Fact]
    public void Limit_Exactly1_Accepted()
    {
        var query = new QueryParameters { Limit = 1 };

        Assert.Equal(1, query.Limit);
    }

    // ───── Cursor & Sort ─────

    [Fact]
    public void Cursor_Default_IsNull()
    {
        var query = new QueryParameters();

        Assert.Null(query.Cursor);
    }

    [Fact]
    public void Cursor_SetValue_ReturnsThatValue()
    {
        var query = new QueryParameters { Cursor = "abc123" };

        Assert.Equal("abc123", query.Cursor);
    }

    [Fact]
    public void Sort_Default_IsNull()
    {
        var query = new QueryParameters();

        Assert.Null(query.Sort);
    }

    [Fact]
    public void Sort_SetValue_ReturnsThatValue()
    {
        var query = new QueryParameters { Sort = "name:asc" };

        Assert.Equal("name:asc", query.Sort);
    }

    // ───── CategoryQueryParameters ─────

    [Fact]
    public void CategoryQueryParameters_FilterProperties_DefaultNull()
    {
        var query = new CategoryQueryParameters();

        Assert.Null(query.Name);
        Assert.Null(query.Code);
    }

    [Fact]
    public void CategoryQueryParameters_FilterProperties_SetValues()
    {
        var query = new CategoryQueryParameters { Name = "test", Code = "T1" };

        Assert.Equal("test", query.Name);
        Assert.Equal("T1", query.Code);
    }

    // ───── SubCategoryQueryParameters ─────

    [Fact]
    public void SubCategoryQueryParameters_FilterProperties_DefaultNull()
    {
        var query = new SubCategoryQueryParameters();

        Assert.Null(query.Name);
        Assert.Null(query.Code);
        Assert.Null(query.CategoryId);
    }

    [Fact]
    public void SubCategoryQueryParameters_FilterProperties_SetValues()
    {
        var query = new SubCategoryQueryParameters { Name = "sub", Code = "S1", CategoryId = 5 };

        Assert.Equal("sub", query.Name);
        Assert.Equal("S1", query.Code);
        Assert.Equal(5, query.CategoryId);
    }

    // ───── Null value edge cases ─────

    [Fact]
    public void Page_NullValue_ClampedTo1()
    {
        var query = new QueryParameters { Page = null };

        Assert.Equal(1, query.Page);
    }

    [Fact]
    public void Limit_NullValue_ClampedTo20()
    {
        var query = new QueryParameters { Limit = null };

        Assert.Equal(20, query.Limit);
    }
}
