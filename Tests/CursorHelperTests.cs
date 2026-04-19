using identity_service.Helpers;

namespace identity_service.Tests;

public class CursorHelperTests
{
    [Fact]
    public void Encode_ReturnsBase64String()
    {
        var result = CursorHelper.Encode(42);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Decode_ValidCursor_ReturnsId()
    {
        var encoded = CursorHelper.Encode(42);

        var result = CursorHelper.Decode(encoded);

        Assert.Equal(42, result);
    }

    [Fact]
    public void Decode_Null_ReturnsNull()
    {
        var result = CursorHelper.Decode(null);

        Assert.Null(result);
    }

    [Fact]
    public void Decode_EmptyString_ReturnsNull()
    {
        var result = CursorHelper.Decode("");

        Assert.Null(result);
    }

    [Fact]
    public void Decode_WhitespaceString_ReturnsNull()
    {
        var result = CursorHelper.Decode("   ");

        Assert.Null(result);
    }

    [Fact]
    public void Decode_InvalidBase64_ReturnsNull()
    {
        var result = CursorHelper.Decode("not-valid-base64!!!");

        Assert.Null(result);
    }

    [Fact]
    public void Decode_ValidBase64ButInvalidJson_ReturnsNull()
    {
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("not json"));

        var result = CursorHelper.Decode(encoded);

        Assert.Null(result);
    }

    [Fact]
    public void Decode_ValidJsonButMissingIdProperty_ReturnsNull()
    {
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"name\":\"test\"}"));

        var result = CursorHelper.Decode(encoded);

        Assert.Null(result);
    }

    [Fact]
    public void Encode_ThenDecode_RoundTrips()
    {
        for (var i = 1; i <= 5; i++)
        {
            var encoded = CursorHelper.Encode(i);
            var decoded = CursorHelper.Decode(encoded);
            Assert.Equal(i, decoded);
        }
    }
}
