using identity_service.Dtos;

namespace identity_service.Tests;

public class DtoTests
{
    // ───── CategoryDto ─────

    [Fact]
    public void CategoryDto_Inequality_ReturnsTrueForDifferent()
    {
        var a = new CategoryDto(1, DateTime.UtcNow, "A", "Alpha");
        var b = new CategoryDto(2, DateTime.UtcNow, "B", "Beta");
        Assert.True(a != b);
    }

    [Fact]
    public void CategoryDto_Inequality_ReturnsFalseForEqual()
    {
        var now = DateTime.UtcNow;
        var a = new CategoryDto(1, now, "A", "Alpha");
        var b = new CategoryDto(1, now, "A", "Alpha");
        Assert.False(a != b);
    }

    [Fact]
    public void CategoryDto_ToString_ContainsProperties()
    {
        var dto = new CategoryDto(1, DateTime.UtcNow, "CAT1", "Category 1");
        var str = dto.ToString();
        Assert.Contains("CAT1", str);
        Assert.Contains("Category 1", str);
    }

    [Fact]
    public void CategoryDto_Deconstruct_Works()
    {
        var now = DateTime.UtcNow;
        var dto = new CategoryDto(1, now, "CAT1", "Category 1");
        var (id, createdAt, code, name) = dto;
        Assert.Equal(1, id);
        Assert.Equal(now, createdAt);
        Assert.Equal("CAT1", code);
        Assert.Equal("Category 1", name);
    }

    // ───── CreateCategoryRequest ─────

    [Fact]
    public void CreateCategoryRequest_Inequality_Works()
    {
        var a = new CreateCategoryRequest("A", "Alpha");
        var b = new CreateCategoryRequest("B", "Beta");
        Assert.True(a != b);
    }

    // ───── UpdateCategoryRequest ─────

    [Fact]
    public void UpdateCategoryRequest_Inequality_Works()
    {
        var a = new UpdateCategoryRequest("A", "Alpha");
        var b = new UpdateCategoryRequest("B", "Beta");
        Assert.True(a != b);
    }

    // ───── SubCategoryDto ─────

    [Fact]
    public void SubCategoryDto_Inequality_ReturnsTrueForDifferent()
    {
        var a = new SubCategoryDto(1, DateTime.UtcNow, "A", "Alpha", 10);
        var b = new SubCategoryDto(2, DateTime.UtcNow, "B", "Beta", 20);
        Assert.True(a != b);
    }

    [Fact]
    public void SubCategoryDto_Inequality_ReturnsFalseForEqual()
    {
        var now = DateTime.UtcNow;
        var a = new SubCategoryDto(1, now, "A", "Alpha", 10);
        var b = new SubCategoryDto(1, now, "A", "Alpha", 10);
        Assert.False(a != b);
    }

    [Fact]
    public void SubCategoryDto_ToString_ContainsProperties()
    {
        var dto = new SubCategoryDto(1, DateTime.UtcNow, "SUB1", "Sub 1", 10);
        var str = dto.ToString();
        Assert.Contains("SUB1", str);
        Assert.Contains("Sub 1", str);
    }

    [Fact]
    public void SubCategoryDto_Deconstruct_Works()
    {
        var now = DateTime.UtcNow;
        var dto = new SubCategoryDto(1, now, "SUB1", "Sub 1", 10);
        var (id, createdAt, code, name, categoryId) = dto;
        Assert.Equal(1, id);
        Assert.Equal(now, createdAt);
        Assert.Equal("SUB1", code);
        Assert.Equal("Sub 1", name);
        Assert.Equal(10, categoryId);
    }

    // ───── CreateSubCategoryRequest ─────

    [Fact]
    public void CreateSubCategoryRequest_Inequality_Works()
    {
        var a = new CreateSubCategoryRequest("A", "Alpha", 1);
        var b = new CreateSubCategoryRequest("B", "Beta", 2);
        Assert.True(a != b);
    }

    // ───── UpdateSubCategoryRequest ─────

    [Fact]
    public void UpdateSubCategoryRequest_Inequality_Works()
    {
        var a = new UpdateSubCategoryRequest("A", "Alpha", 1);
        var b = new UpdateSubCategoryRequest("B", "Beta", 2);
        Assert.True(a != b);
    }
}
