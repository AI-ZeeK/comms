using System.Threading.Tasks;
using Comms.Constants;
using Comms.Guards;
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

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto pushSubscription)
        {
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
