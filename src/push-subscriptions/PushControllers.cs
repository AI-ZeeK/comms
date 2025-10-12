using Comms.Constants;
using Comms.Guards;
using Comms.Models;
using Comms.Models.DTOs;
using Comms.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comms.Controllers
{
    [ApiController]
    [Route(ApiConstants.API_VERSION + "/push-subscription")]
    public class PushController : ControllerBase
    {
        private readonly PushService _pushService;
        private readonly ILogger<PushController> _logger;

        public PushController(ILogger<PushController> logger, PushService pushService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pushService = pushService ?? throw new ArgumentNullException(nameof(pushService));
        }

        [HttpPost("admin/subscribe")]
        [ServiceFilter(typeof(AdminGuard))]
        public async Task<IActionResult> AdminSubscribe(
            [FromBody] PushSubscriptionDto pushSubscription
        )
        {
            // Retrieve admin user ID added by the AdminGuard
            var user_id = HttpContext.Items["user_id"]?.ToString();

            if (string.IsNullOrEmpty(user_id))
            {
                return Unauthorized(new { message = "Admin ID missing from context" });
            }

            // Inject the ID and set user type to ADMIN
            pushSubscription.UserId = Guid.Parse(user_id);
            pushSubscription.UserType = UserType.ADMIN;
            await _pushService.SavePushSubscription(pushSubscription);
            return Ok();
        }

        [HttpPost("subscribe")]
        [ServiceFilter(typeof(UserGuard))]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto pushSubscription)
        {
            var user_id = HttpContext.Items["user_id"]?.ToString();

            if (string.IsNullOrEmpty(user_id))
            {
                return Unauthorized(new { message = "User ID missing from context" });
            }

            // Inject the ID and set user type to ADMIN
            pushSubscription.UserId = Guid.Parse(user_id);
            await _pushService.SavePushSubscription(pushSubscription);
            return Ok();
        }

        [HttpGet("vapid-public-key")]
        public async Task<IActionResult> GetVapidPublicKey()
        {
            try
            {
                var con = await _pushService.GetVapidPublicKey();
                _logger.LogInformation("Logger ==== {con}", con);
                return Ok(new { public_key = con });
            }
            catch (ArgumentNullException ex)
            {
                // Example: bad input
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Example: business logic error
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Unexpected errors
                _logger.LogError(ex, "Error saving push subscription");
                return StatusCode(500, new { message = $"An unexpected error occurred , {ex}" });
            }
        }
    }
}
