using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using Comms.Services;

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
                var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    context.Result = new UnauthorizedObjectResult(new { message = "Missing or invalid authorization header" });
                    return;
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var userResponse = await _adminGrpcService.ValidateAdminAccountAsync(token);
                if (userResponse == null || !userResponse.Success)
                {
                    // context.Result = new UnauthorizedResult();
                    context.Result = new JsonResult(new { message = userResponse?.Message ??""})
                        {
                            StatusCode = StatusCodes.Status401Unauthorized
                        };
                    return;

                    }
                // Add user info to context for use in controllers
                context.HttpContext.Items["AdminInfo"] = userResponse;
                context.HttpContext.Items["user_id"] = userResponse.User?.UserId;

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

