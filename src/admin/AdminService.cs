using System.Text.Json;
using Comms.Constants;
using Comms.Data;
using Comms.Models;
using Comms.Models.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebPush;

namespace Comms.Services
{
    public class Admin_Service
    {
        private readonly CommunicationsDbContext _context;
        private readonly ILogger<PushService> _logger;

        public Admin_Service(CommunicationsDbContext context, ILogger<PushService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MetaData> GetAdminMetaData()
        {
            try
            {
                _logger.LogInformation("Fetching MetaData");
                var existingMeta = await _context.MetaData.FirstOrDefaultAsync();
                if (existingMeta == null)
                {
                    _logger.LogInformation("No MetaData found, creating default");
                    var newMeta = new MetaData
                    {
                        EnablePushNotifications = false,
                        CreatedAt = DateTime.UtcNow,
                    };
                    _context.MetaData.Add(newMeta);
                    await _context.SaveChangesAsync();
                    return newMeta;
                }

                return existingMeta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching MetaData");
                throw new Exception("Error fetching MetaData", ex);
            }
        }

        public async Task<MetaData> UpdateAdminMetaData(AdminMetaDataDto meta_data)
        {
            try
            {
                var existingMeta = await _context.MetaData.FirstOrDefaultAsync();
                if (existingMeta == null)
                {
                    _logger.LogInformation("No MetaData found, creating new one");
                    existingMeta = new MetaData
                    {
                        EnablePushNotifications = meta_data.EnablePushNotifications,
                        EnableNewUser = meta_data.EnableNewUser,
                        EnableSystemError = meta_data.EnableSystemError,
                        EnableWelcomeEmail = meta_data.EnableWelcomeEmail,
                        EnableFailedPayments = meta_data.EnableFailedPayments,
                        EnablePasswordReset = meta_data.EnablePasswordReset,
                        EnableNewsLetter = meta_data.EnableNewsLetter,
                        EnableSecurityAlerts = meta_data.EnableSecurityAlerts,
                        CreatedAt = DateTime.UtcNow,
                    };
                    _context.MetaData.Add(existingMeta);
                }
                else
                {
                    existingMeta.EnablePushNotifications = meta_data.EnablePushNotifications;
                    existingMeta.EnableNewUser = meta_data.EnableNewUser;
                    existingMeta.EnableSystemError = meta_data.EnableSystemError;
                    existingMeta.EnableWelcomeEmail = meta_data.EnableWelcomeEmail;
                    existingMeta.EnableFailedPayments = meta_data.EnableFailedPayments;
                    existingMeta.EnablePasswordReset = meta_data.EnablePasswordReset;
                    existingMeta.EnableNewsLetter = meta_data.EnableNewsLetter;
                    existingMeta.EnableSecurityAlerts = meta_data.EnableSecurityAlerts;
                    existingMeta.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return existingMeta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating MetaData");
                throw new Exception("Error updating MetaData", ex);
            }
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
                _context.VapidKeys.Add(
                    new Models.VapidKeys
                    {
                        PublicKey = publicKey,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }
                );
            }

            await _context.SaveChangesAsync();
            return publicKey;
        }
    }
}
