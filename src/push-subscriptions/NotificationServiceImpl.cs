using System.Threading.Tasks;
using Comms.Grpc;
using Comms.Models.DTOs;
using Comms.Services;
using Grpc.Core;

public class NotificationServiceImpl : Communications.CommunicationsBase
{
    private readonly PushService _pushService;

    public NotificationServiceImpl(PushService pushService)
    {
        _pushService = pushService;
    }

    public override async Task<PushSubscriptionResponse> SendAdminNotification(
        PushSubscriptionRequest request,
        ServerCallContext context
    )
    {
        try
        {
            var notificationData = new Comms.Models.DTOs.NotificationData
            {
                EntityId = request.Data?.EntityId ?? string.Empty,
                SenderId = request.Data?.SenderId ?? string.Empty,
                SenderName = request.Data?.SenderName ?? string.Empty,
                EntityType = (Comms.Models.NotificationType)
                    (NotificationType)(int)(request.Data?.EntityType ?? 0),
            };

            await _pushService.SendAdminNotificationToUserAsync(
                request.RecipientUserId,
                request.Title,
                request.Body,
                notificationData
            );

            return new PushSubscriptionResponse { Success = true, Message = "Notification sent" };
        }
        catch (Exception ex)
        {
            return new PushSubscriptionResponse { Success = false, Message = ex.Message };
        }
    }

    public override async Task<PushSubscriptionResponse> SendUserNotification(
        PushSubscriptionRequest request,
        ServerCallContext context
    )
    {
        try
        {
            var notificationData = new Comms.Models.DTOs.NotificationData
            {
                EntityId = request.Data?.EntityId ?? string.Empty,
                SenderId = request.Data?.SenderId ?? string.Empty,
                SenderName = request.Data?.SenderName ?? string.Empty,
                EntityType = (Comms.Models.NotificationType)
                    (NotificationType)(int)(request.Data?.EntityType ?? 0),
            };

            await _pushService.SendNotificationToUserAsync(
                request.RecipientUserId,
                request.Title,
                request.Body,
                notificationData
            );

            return new PushSubscriptionResponse { Success = true, Message = "Notification sent" };
        }
        catch (Exception ex)
        {
            return new PushSubscriptionResponse { Success = false, Message = ex.Message };
        }
    }
}
