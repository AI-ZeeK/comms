using System.Text.Json;
using Comms.Constants;
using Comms.Data;
using Comms.Models;
using Comms.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace Comms.Services
{
    public class PushService
    {
        private readonly CommunicationsDbContext _context;
        private readonly ILogger<PushService> _logger;

        public PushService(CommunicationsDbContext context, ILogger<PushService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<object> GetVapidPublicKey()
        {
            var existingKey = await _context.VapidKeys.FirstOrDefaultAsync();
            if (existingKey == null)
            {
                _logger.LogInformation("error  {existingKey}", existingKey);
                throw new InvalidOperationException("No VAPID key found");
            }

            _logger.LogInformation("info  {existingKey}", existingKey);
            return existingKey?.PublicKey ?? "No Key Set";
        }

        public async Task SendAdminNotificationToUserAsync(
            string userId,
            string title,
            string body,
            NotificationData data
        // Dictionary<string, string>? data = null
        )
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new ArgumentException("Invalid user ID");
            }

            var subscriptions = await _context
                .PushSubscriptions.Where(s => s.UserId == userGuid && s.UserType == UserType.ADMIN)
                .ToListAsync();

            if (!subscriptions.Any())
            {
                _logger.LogWarning($"No push subscriptions found for user {userId}");
                return;
            }

            foreach (var sub in subscriptions)
            {
                try
                {
                    if (sub.Platform == PushSubscriptionPlatform.WEB)
                    {
                        await SendWebPushAsync(sub, title, body, data);
                    }
                    else if (
                        sub.Platform == PushSubscriptionPlatform.ANDROID
                        || sub.Platform == PushSubscriptionPlatform.IOS
                    )
                    {
                        // Call your Expo/Firebase push method
                        // await SendMobilePushAsync(sub.Endpoint, title, body, data);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        $"Failed to send notification to user {userId}, subscription {sub.SubscriptionId}"
                    );
                    if (ex.Message.Contains("410")) // expired
                    {
                        _context.PushSubscriptions.Remove(sub);
                        await _context.SaveChangesAsync();
                        _logger.LogWarning($"Deleted expired subscription {sub.SubscriptionId}");
                    }
                }
            }
        }

        public async Task SendNotificationToUserAsync(
            string userId,
            string title,
            string body,
            NotificationData data
        // Dictionary<string, string>? data = null
        )
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                // Handle invalid GUID
                throw new ArgumentException("Invalid user ID");
            }

            var subscriptions = await _context
                .PushSubscriptions.Where(s => s.UserId == userGuid && s.UserType == UserType.USER)
                .ToListAsync();

            if (!subscriptions.Any())
            {
                _logger.LogWarning($"No push subscriptions found for user {userId}");
                return;
            }

            foreach (var sub in subscriptions)
            {
                try
                {
                    if (sub.Platform == PushSubscriptionPlatform.WEB)
                    {
                        await SendWebPushAsync(sub, title, body, data);
                    }
                    else if (
                        sub.Platform == PushSubscriptionPlatform.ANDROID
                        || sub.Platform == PushSubscriptionPlatform.IOS
                    )
                    {
                        // Call your Expo/Firebase push method
                        // await SendMobilePushAsync(sub.Endpoint, title, body, data);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        $"Failed to send notification to user {userId}, subscription {sub.SubscriptionId}"
                    );
                    if (ex.Message.Contains("410")) // expired
                    {
                        _context.PushSubscriptions.Remove(sub);
                        await _context.SaveChangesAsync();
                        _logger.LogWarning($"Deleted expired subscription {sub.SubscriptionId}");
                    }
                }
            }
        }

        private async Task SendWebPushAsync(
            Models.PushSubscription sub,
            string title,
            string body,
            NotificationData data
        )
        {
            var subscription = new WebPush.PushSubscription
            {
                Endpoint = sub.Endpoint,
                P256DH = sub.P256dh,
                Auth = sub.Auth,
            };

            var Notification = await _context.Notification.AddAsync(
                new Notification
                {
                    RecipientUserId = sub.UserId,
                    SenderUserId = Guid.TryParse(data?.SenderId, out var senderGuid)
                        ? senderGuid
                        : null,
                    Text = body,
                    Type = data?.EntityType ?? NotificationType.NEW_MESSAGE,
                    LinkUrl = data?.EntityId,
                    CreatedAt = DateTime.UtcNow,
                }
            );

            var payload = new
            {
                title,
                body,
                icon = "/icon-192x192.png",
                badge = "/badge.png",
                tag = $"{data?.EntityType}_{data?.EntityId}", // e.g., "chat_12345"
                data = new
                {
                    notification_id = Notification.Entity.NotificationId,
                    entity_id = data?.EntityId,
                    sender_id = data?.SenderId,
                    sender_name = data?.SenderName,
                    entity_type = data?.EntityType,
                    created_at = Notification.Entity.CreatedAt,
                },
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var vapidKey = (await GetVapidPublicKey())?.ToString() ?? "";
            var client = new WebPushClient();
            await client.SendNotificationAsync(subscription, jsonPayload, vapidKey);
            _logger.LogInformation("Sent web push notification to {Endpoint}", sub.Endpoint);
        }

        public async Task SavePushSubscription(PushSubscriptionDto pushSubscription)
        {
            try
            {
                // _context.PushSubscriptions.Add(new Models.PushSubscription
                // {
                //     UserId = pushSubscription.UserId,
                //     Endpoint = pushSubscription.Subscription.Endpoint,
                //     P256dh = pushSubscription.Subscription.Keys.P256dh,
                //     Auth = pushSubscription.Subscription.Keys.Auth,
                //     Platform = pushSubscription.Platform,
                //     CreatedAt = DateTime.UtcNow,
                //     UpdatedAt = DateTime.UtcNow
                // });
                // await _context.SaveChangesAsync();
                // If the platform is WEB, clear any previous web subscriptions for this user
                if (pushSubscription.Platform == PushSubscriptionPlatform.WEB)
                {
                    var existingWebSubs = _context.PushSubscriptions.Where(x =>
                        x.UserId == pushSubscription.UserId
                        && x.Platform == PushSubscriptionPlatform.WEB
                    );

                    _context.PushSubscriptions.RemoveRange(existingWebSubs);
                }

                // Check if a matching record already exists (for non-web platforms)
                var existing = await _context.PushSubscriptions.FirstOrDefaultAsync(x =>
                    x.UserId == pushSubscription.UserId
                    && x.Endpoint == pushSubscription.Subscription.Endpoint
                    && x.Platform == pushSubscription.Platform
                );

                if (existing != null)
                {
                    // Update existing record
                    existing.P256dh = pushSubscription.Subscription.Keys.P256dh;
                    existing.Auth = pushSubscription.Subscription.Keys.Auth;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create a new one
                    _context.PushSubscriptions.Add(
                        new Models.PushSubscription
                        {
                            UserId = pushSubscription.UserId,
                            Endpoint = pushSubscription.Subscription.Endpoint,
                            P256dh = pushSubscription.Subscription.Keys.P256dh,
                            Auth = pushSubscription.Subscription.Keys.Auth,
                            Platform = pushSubscription.Platform,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        }
                    );
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Rethrow or wrap the exception for centralized logging
                throw new Exception($"Error saving push subscription: {ex.Message}", ex);
            }
        }
    }
}
