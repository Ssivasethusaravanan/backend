using System.Text;
using System.Text.Json;

namespace identity_service.Helpers;

public static class CursorHelper
{
    public static string Encode(int id)
    {
        var json = JsonSerializer.Serialize(new { id });
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public static int? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("id").GetInt32();
        }
        catch
        {
            return null;
        }
    }
}
