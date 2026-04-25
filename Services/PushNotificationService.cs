using FirebaseAdmin.Messaging;
using identity_service.Data;
using identity_service.Models;

namespace identity_service.Services;

/// <summary>
/// Wraps FirebaseMessaging with a clean interface:
/// — single device, multicast, and topic delivery
/// — automatic stale-token cleanup
/// </summary>
public class PushNotificationService(
    IUserRepository userRepository,
    ILogger<PushNotificationService> logger)
{
    // ── Single Device ──────────────────────────────────────────────────────────

    public async Task<string> SendToDeviceAsync(
        string fcmToken,
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        var message = BuildMessage(payload, token: fcmToken);

        try
        {
            var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
            logger.LogInformation("FCM single-device send. MessageId={MessageId}.", messageId);
            return messageId;
        }
        catch (FirebaseMessagingException ex) when (IsStaleToken(ex))
        {
            logger.LogWarning("Stale FCM token detected during single send. Clearing token.");
            await userRepository.ClearFcmTokenAsync(fcmToken, ct);
            throw new InvalidOperationException("The device token is no longer valid.", ex);
        }
    }

    // ── Multicast ──────────────────────────────────────────────────────────────

    public async Task<BatchResponse> SendToDevicesAsync(
        IEnumerable<string> fcmTokens,
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        var tokens = fcmTokens.ToList();
        var multicast = BuildMulticastMessage(payload, tokens);

        var batchResponse = await FirebaseMessaging.DefaultInstance
            .SendEachForMulticastAsync(multicast, ct);

        logger.LogInformation(
            "FCM multicast: {SuccessCount} success, {FailureCount} failures out of {Total}.",
            batchResponse.SuccessCount, batchResponse.FailureCount, tokens.Count);

        // Auto-clean stale tokens
        var staleTasks = batchResponse.Responses
            .Select((r, i) => (response: r, token: tokens[i]))
            .Where(x => !x.response.IsSuccess && IsStaleToken(x.response.Exception))
            .Select(x => userRepository.ClearFcmTokenAsync(x.token, ct));

        await Task.WhenAll(staleTasks);

        return batchResponse;
    }

    // ── Topic ──────────────────────────────────────────────────────────────────

    public async Task<string> SendToTopicAsync(
        string topic,
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        var message = BuildMessage(payload, topic: topic);
        var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
        logger.LogInformation("FCM topic send. Topic={Topic}, MessageId={MessageId}.", topic, messageId);
        return messageId;
    }

    public async Task SubscribeToTopicAsync(
        IEnumerable<string> fcmTokens,
        string topic,
        CancellationToken ct = default)
    {
        var result = await FirebaseMessaging.DefaultInstance
            .SubscribeToTopicAsync(fcmTokens, topic, ct);
        logger.LogInformation("FCM subscribe to '{Topic}': {Success} success, {Failure} failures.",
            topic, result.SuccessCount, result.FailureCount);
    }

    public async Task UnsubscribeFromTopicAsync(
        IEnumerable<string> fcmTokens,
        string topic,
        CancellationToken ct = default)
    {
        var result = await FirebaseMessaging.DefaultInstance
            .UnsubscribeFromTopicAsync(fcmTokens, topic, ct);
        logger.LogInformation("FCM unsubscribe from '{Topic}': {Success} success, {Failure} failures.",
            topic, result.SuccessCount, result.FailureCount);
    }

    // ── Builder Helpers ────────────────────────────────────────────────────────

    private static Message BuildMessage(NotificationPayload p, string? token = null, string? topic = null)
        => new()
        {
            Token   = token,
            Topic   = topic,
            Notification = new Notification
            {
                Title    = p.Title,
                Body     = p.Body,
                ImageUrl = p.ImageUrl,
            },
            Android = new AndroidConfig
            {
                Notification = new AndroidNotification
                {
                    ChannelId = p.ChannelId,
                    Sound     = "default",
                }
            },
            Apns = new ApnsConfig
            {
                Payload = new ApnsPayload
                {
                    Aps = new Aps
                    {
                        Badge = p.Badge is not null ? int.Parse(p.Badge) : null,
                        Sound = "default",
                    }
                }
            },
            Data = p.Data,
        };

    private static MulticastMessage BuildMulticastMessage(NotificationPayload p, List<string> tokens)
        => new()
        {
            Tokens = tokens,
            Notification = new Notification
            {
                Title    = p.Title,
                Body     = p.Body,
                ImageUrl = p.ImageUrl,
            },
            Android = new AndroidConfig
            {
                Notification = new AndroidNotification
                {
                    ChannelId = p.ChannelId,
                    Sound     = "default",
                }
            },
            Apns = new ApnsConfig
            {
                Payload = new ApnsPayload
                {
                    Aps = new Aps { Sound = "default" }
                }
            },
            Data = p.Data,
        };

    private static bool IsStaleToken(FirebaseMessagingException? ex)
        => ex?.MessagingErrorCode is
            MessagingErrorCode.Unregistered or
            MessagingErrorCode.InvalidArgument;
}
