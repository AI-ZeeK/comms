using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using Comms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Comms.Guards
{
    public class AdminGuard : Attribute, IAsyncAuthorizationFilter
    {
        private readonly IAdminGrpcService _adminGrpcService;
        private readonly ILogger<AdminGuard> _logger;

        public AdminGuard(IAdminGrpcService adminGrpcService, ILogger<AdminGuard> logger)
        {
            _adminGrpcService = adminGrpcService;
            _logger = logger;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            try
            {
                // Get JWT token from Authorization header
                var authHeader = context
                    .HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault();

                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    context.Result = new UnauthorizedObjectResult(
                        new { message = "Missing or invalid authorization header" }
                    );
                    return;
                }
                _logger.LogInformation("Auth Header: {AuthHeader}", authHeader);

                var token = authHeader.Substring("Bearer ".Length).Trim();
                _logger.LogInformation("Token: {Token}", token);
                var userResponse = await _adminGrpcService.ValidateAdminAccountAsync(token);
                _logger.LogWarning("User Response: {UserResponse}", userResponse);
                if (userResponse == null || !userResponse.Success)
                {
                    _logger.LogWarning("Admin validation failed: {Message}", userResponse?.Message);
                    // context.Result = new UnauthorizedResult();
                    context.Result = new JsonResult(new { message = userResponse?.Message ?? "" })
                    {
                        StatusCode = StatusCodes.Status401Unauthorized,
                    };
                    return;
                }
                _logger.LogInformation("Admin validated: {UserId}", userResponse.User?.UserId);

                var user = userResponse.User;

                var username =
                    !string.IsNullOrWhiteSpace(user?.FirstName)
                    && !string.IsNullOrWhiteSpace(user?.LastName)
                        ? $"{user.FirstName} {user.LastName}"
                    : !string.IsNullOrWhiteSpace(user?.Email) ? user.Email
                    : "";
                var avatar_url = !string.IsNullOrWhiteSpace(user?.AvatarUrl) ? user.AvatarUrl : "";
                // Add user info to context for use in controllers
                context.HttpContext.Items["user_id"] = userResponse.User?.UserId;
                context.HttpContext.Items["avatar_url"] = avatar_url;
                context.HttpContext.Items["username"] = username;

                // _logger.LogInformation("Admin validation successful for store: {StoreId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating admin");
                context.Result = new StatusCodeResult(500);
            }
        }
    }
}
