using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using identity_service.Data;
using identity_service.Models;
using identity_service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace identity_service.Controllers;

/// <summary>
/// Device FCM token registration and push notification send endpoints.
///
/// — Regular users: register/unregister their FCM token, subscribe to topics.
/// — Admin users: send notifications to specific users or topics.
/// </summary>
[Route("identity/notifications")]
[Authorize]
public class NotificationController(
    PushNotificationService pushService,
    IUserRepository userRepository,
    ILogger<NotificationController> logger) : BaseApiController
{
    // ── POST /identity/notifications/register-device ──────────────────────────

    [HttpPost("register-device")]
    [ProducesResponseType(typeof(ApiSuccessResponse<MessageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterDevice(
        [FromBody] RegisterDeviceRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return ApiUnauthorized("Cannot identify authenticated user.");

        await userRepository.UpdateFcmTokenAsync(userId, request.FcmToken, ct);

        logger.LogInformation("FCM token registered for user {UserId}. Platform={Platform}.",
            userId, request.Platform ?? "unknown");

        return ApiOk(new MessageResponse { Message = "Device registered for push notifications." });
    }

    // ── DELETE /identity/notifications/unregister-device ──────────────────────

    [HttpDelete("unregister-device")]
    [ProducesResponseType(typeof(ApiSuccessResponse<MessageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnregisterDevice(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return ApiUnauthorized("Cannot identify authenticated user.");

        await userRepository.UpdateFcmTokenAsync(userId, null, ct);

        logger.LogInformation("FCM token removed for user {UserId}.", userId);

        return ApiOk(new MessageResponse { Message = "Device unregistered from push notifications." });
    }

    // ── POST /identity/notifications/send (Admin only) ────────────────────────

    [HttpPost("send")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiSuccessResponse<MessageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Send(
        [FromBody] SendNotificationRequest request,
        CancellationToken ct)
    {
        var userIds = request.UserIds
            .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();

        var tokens = await userRepository.GetFcmTokensByUserIdsAsync(userIds, ct);

        if (tokens.Count == 0)
        {
            logger.LogWarning("Send notification: no FCM tokens found for {Count} user IDs.", userIds.Count);
            return ApiOk(new MessageResponse { Message = "No registered devices found for the specified users." });
        }

        var batch = await pushService.SendToDevicesAsync(tokens, request.Payload, ct);

        return ApiOk(new MessageResponse
        {
            Message = $"Notification sent. {batch.SuccessCount} delivered, {batch.FailureCount} failed."
        });
    }

    // ── POST /identity/notifications/send-topic (Admin only) ──────────────────

    [HttpPost("send-topic")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiSuccessResponse<MessageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendToTopic(
        [FromBody] SendTopicNotificationRequest request,
        CancellationToken ct)
    {
        var messageId = await pushService.SendToTopicAsync(request.Topic, request.Payload, ct);

        return ApiOk(new MessageResponse { Message = $"Topic notification sent. MessageId={messageId}." });
    }

    // ── POST /identity/notifications/subscribe ────────────────────────────────

    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(ApiSuccessResponse<MessageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Subscribe(
        [FromBody] TopicSubscriptionRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return ApiUnauthorized("Cannot identify authenticated user.");

        var tokens = await userRepository.GetFcmTokensByUserIdsAsync([userId], ct);
        if (tokens.Count == 0)
            return ApiBadRequest("No registered device found. Please register your device first.");

        await pushService.SubscribeToTopicAsync(tokens, request.Topic, ct);

        return ApiOk(new MessageResponse { Message = $"Subscribed to topic '{request.Topic}'." });
    }

    // ── POST /identity/notifications/unsubscribe ──────────────────────────────

    [HttpPost("unsubscribe")]
    [ProducesResponseType(typeof(ApiSuccessResponse<MessageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Unsubscribe(
        [FromBody] TopicSubscriptionRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var tokens = await userRepository.GetFcmTokensByUserIdsAsync([userId], ct);

        if (tokens.Count == 0)
            return ApiBadRequest("No registered device found.");

        await pushService.UnsubscribeFromTopicAsync(tokens, request.Topic, ct);

        return ApiOk(new MessageResponse { Message = $"Unsubscribed from topic '{request.Topic}'." });
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
