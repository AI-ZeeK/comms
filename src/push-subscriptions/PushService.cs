using Comms.Data;
using Comms.Models;
using Comms.Models.DTOs;
using Comms.Constants;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;
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
            _logger.LogInformation("error  {existingKey}" ,existingKey);
                throw new InvalidOperationException("No VAPID key found");  
            }

            _logger.LogInformation("info  {existingKey}" ,existingKey);
            return existingKey?.PublicKey ?? "No Key Set";
         

        }

        public async Task<string> CreateOrUpdateVapidKeys(string publicKey)
        {
            var existingKey = await _context.VapidKeys.FirstOrDefaultAsync();
            // _logger.LogInformation("", publicKey);
            if (existingKey != null)
            {
                // Update existing key
                existingKey.PublicKey = publicKey;
                existingKey.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new key
                _context.VapidKeys.Add(new Models.VapidKeys
                {
                    PublicKey = publicKey,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            
            await _context.SaveChangesAsync();  
            return publicKey;
        }

        public async Task SavePushSubscription(PushSubscriptionDto pushSubscription)
        {
            try {
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
            var existingWebSubs = _context.PushSubscriptions
                .Where(x => x.UserId == pushSubscription.UserId && x.Platform == PushSubscriptionPlatform.WEB);

            _context.PushSubscriptions.RemoveRange(existingWebSubs);
        }

        // Check if a matching record already exists (for non-web platforms)
        var existing = await _context.PushSubscriptions
            .FirstOrDefaultAsync(x =>
                x.UserId == pushSubscription.UserId &&
                x.Endpoint == pushSubscription.Subscription.Endpoint &&
                x.Platform == pushSubscription.Platform);

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
            _context.PushSubscriptions.Add(new Models.PushSubscription
            {
                UserId = pushSubscription.UserId,
                Endpoint = pushSubscription.Subscription.Endpoint,
                P256dh = pushSubscription.Subscription.Keys.P256dh,
                Auth = pushSubscription.Subscription.Keys.Auth,
                Platform = pushSubscription.Platform,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
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