using System.ComponentModel.DataAnnotations;

namespace identity_service.Models;

// ── Notification payload ──────────────────────────────────────────────────

/// <summary>Portable notification payload that maps onto FCM Message fields.</summary>
public class NotificationPayload
{
    [Required]
    public required string Title { get; init; }

    [Required]
    public required string Body { get; init; }

    public string? ImageUrl { get; init; }

    /// <summary>Arbitrary key-value data forwarded in the FCM data map.</summary>
    public Dictionary<string, string>? Data { get; init; }

    // Android
    public string? ChannelId { get; init; }

    // iOS
    public string? Badge { get; init; }
}

// ── Request DTOs ──────────────────────────────────────────────────────────

public class RegisterDeviceRequest
{
    [Required]
    public required string FcmToken { get; init; }

    /// <summary>"android" | "ios" | "web" — informational, stored for analytics.</summary>
    public string? Platform { get; init; }
}

public class SendNotificationRequest
{
    [Required, MinLength(1)]
    public required List<string> UserIds { get; init; }

    [Required]
    public required NotificationPayload Payload { get; init; }
}

public class SendTopicNotificationRequest
{
    [Required]
    public required string Topic { get; init; }

    [Required]
    public required NotificationPayload Payload { get; init; }
}

public class TopicSubscriptionRequest
{
    [Required]
    public required string Topic { get; init; }
}
