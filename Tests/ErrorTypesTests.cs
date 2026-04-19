using identity_service.Constants;

namespace identity_service.Tests;

public class ErrorTypesTests
{
    [Fact]
    public void NotFound_HasRfc7231Uri()
    {
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.4", ErrorTypes.NotFound);
    }

    [Fact]
    public void Validation_HasRfc7231Uri()
    {
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", ErrorTypes.Validation);
    }

    [Fact]
    public void InternalServerError_HasRfc7231Uri()
    {
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.6.1", ErrorTypes.InternalServerError);
    }

    [Fact]
    public void TooManyRequests_HasRfc6585Uri()
    {
        Assert.Equal("https://tools.ietf.org/html/rfc6585#section-4", ErrorTypes.TooManyRequests);
    }
}
