using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Comms.Services;

namespace Comms.Guards
{
    public class UserGuard : Attribute, IAsyncAuthorizationFilter
    {
        private readonly IProfileGrpcService _profileService;
        private readonly ILogger<UserGuard> _logger;

        public UserGuard(IProfileGrpcService profileService, ILogger<UserGuard> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            try
            {
                // Get JWT token from Authorization header
                var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    context.Result = new UnauthorizedObjectResult(new { message = "Missing or invalid authorization header" });
                    return;
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();

                // Validate token with profile service via gRPC
                var userResponse = await _profileService.ValidateUserAsync(token);

                if (!userResponse.Success)
                {
                    _logger.LogWarning("Profile service validation failed: {Error}", userResponse.Error);
                    context.Result = new UnauthorizedObjectResult(new { message = "Invalid user token" });
                    return;
                }

                // Extract user info from response and add to context
                context.HttpContext.Items["UserInfo"] = userResponse.User;
                context.HttpContext.Items["UserId"] = userResponse.User?.UserId;

                _logger.LogInformation("User validation successful for user: {UserId}", userResponse.User?.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user");
                context.Result = new StatusCodeResult(500);
            }
        }
    }
}

