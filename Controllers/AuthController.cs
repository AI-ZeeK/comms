using Comms.Constants;
using Comms.Data;
using Comms.Models;
using Comms.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comms.Controllers
{
    [ApiController] // tells ASP.NET this is a Web API controller
    [Route(ApiConstants.API_VERSION + "/[controller]")] // URL will be /auth
    public class AuthController(ILogger<AuthController> logger, CommunicationsDbContext context)
        : ControllerBase
    {
        private readonly ILogger<AuthController> _logger = logger;
        private readonly CommunicationsDbContext _context = context;

        // public AuthController(ILogger<AuthController> logger)
        // {
        //     _logger = logger;
        // }

        [HttpGet("dummy")] // GET /auth/dummy
        [AllowAnonymous] // allows anyone to call it, no JWT required
        public IActionResult GetDummyData()
        {
            var dummyUser = new
            {
                user_id = 1,
                username = "testuser",
                email = "test@example.com",
                role = "Admin",
                api_version = ApiConstants.API_VERSION,
            };

            return Ok(dummyUser); // returns JSON response
        }

        [HttpPost("login")] // POST /api/auth/login
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("======Login request received: {request}", request);
            // just return dummy token (normally you'd check DB + issue JWT)
            if (request.Username == "test" && request.Password == "password")
            {
                return Ok(new { Token = "dummy-jwt-token-123", ExpiresIn = 3600 });
            }

            return Unauthorized(new { message = "Invalid credentials" });
        }

        [HttpPost("push-subscription")] // POST /api/auth/push-subscription
        [AllowAnonymous]
        public async Task<IActionResult> SavePushSubscription(
            [FromBody] PushSubscriptionDto request
        )
        {
            try
            {
                _logger.LogInformation(
                    "Saving push subscription for user {UserId}",
                    request.UserId
                );

                var pushSubscription = new Models.PushSubscription
                {
                    UserId = request.UserId,
                    Endpoint = request.Subscription.Endpoint,
                    P256dh = request.Subscription.Keys.P256dh,
                    Auth = request.Subscription.Keys.Auth,
                    Platform = request.Platform,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                _context.PushSubscriptions.Add(pushSubscription);
                await _context.SaveChangesAsync();

                return Ok(
                    new
                    {
                        message = "Push subscription saved successfully",
                        subscriptionId = pushSubscription.SubscriptionId,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error saving push subscription for user {UserId}",
                    request.UserId
                );
                return StatusCode(500, "Internal server error");
            }
        }
    }

    // DTO for login
    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
