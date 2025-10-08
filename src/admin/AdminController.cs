using System.Threading.Tasks;
using Comms.Constants;
using Comms.Guards;
using Comms.Models.DTOs;
using Comms.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comms.Controllers
{
    [ApiController]
    [Route(ApiConstants.API_VERSION + "/admin")]
    public class AdminController : ControllerBase
    {
        private readonly Admin_Service _adminService;
        private readonly ILogger<PushController> _logger;

        public AdminController(ILogger<PushController> logger, Admin_Service adminService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
        }

        [HttpPost("metadata")]
        [ServiceFilter(typeof(AdminGuard))]
        public async Task<IActionResult> UpdateMetaData([FromBody] AdminMetaDataDto metaDataDto)
        {
            try
            {
                _logger.LogInformation("Updating MetaData with {MetaData}", metaDataDto);
                var updatedMetaData = await _adminService.UpdateAdminMetaData(metaDataDto);
                return Ok(updatedMetaData);
            }
            catch (ArgumentNullException ex)
            {
                // Example: bad input
                _logger.LogError(ex, "Bad input for updating metadata: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(
                    ex,
                    "Business logic error while updating metadata: {Message}",
                    ex.Message
                );
                // Example: business logic error
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Unexpected errors
                _logger.LogError(ex, "Error updating metadata");
                return StatusCode(500, new { message = "An unexpected error occurred" });
            }
        }

        [HttpGet("metadata")]
        [ServiceFilter(typeof(AdminGuard))]
        public async Task<IActionResult> GetMetaData()
        {
            try
            {
                var metaData = await _adminService.GetAdminMetaData();
                return Ok(metaData);
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
                _logger.LogError(ex, "Error fetching metadata");
                return StatusCode(500, new { message = "An unexpected error occurred" });
            }
        }

        [HttpPost("vapid-public-key")]
        [ServiceFilter(typeof(AdminGuard))]
        public async Task<IActionResult> CreateKey([FromBody] CreatePushSubscriptionDto createDto)
        {
            try
            {
                var public_key = await _adminService.CreateOrUpdateVapidKeys(createDto.PublicKey);
                return Ok(new { public_key });
            }
            catch (Exception ex)
            {
                throw new Exception("", ex);
            }
        }
    }
}
