using Grpc.Net.Client;
using Grpc.Core; 
using Admin; // this namespace comes from the proto file
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Comms.Helpers;

namespace Comms.Services
{
    public interface IAdminGrpcService
    {
        Task<UserResponse> ValidateAdminAsync(string token);
        Task<FetchAdminAccountResponse> ValidateAdminAccountAsync(string token);
        Task<FetchAdminAccountResponse> FetchAdminAccountAsync(string userId);
    }

    public class AdminGrpcService : IAdminGrpcService
    {
        private readonly AdminService.AdminServiceClient _client;
        private readonly ILogger<AdminGrpcService> _logger;

        public AdminGrpcService(IConfiguration configuration, ILogger<AdminGrpcService> logger)
        {
            _logger = logger;

            // Get admin service URL from environment or configuration
            var adminServiceUrl = Environment.GetEnvironmentVariable("ADMIN_SERVICE_URL")
                ?? configuration["Microservices:AdminService"]
                ?? "http://localhost:50057";

            // Ensure the URL has a valid scheme for gRPC
            if (!adminServiceUrl.StartsWith("http://") && !adminServiceUrl.StartsWith("https://"))
            {
                adminServiceUrl = $"http://{adminServiceUrl}";
            }

            _logger.LogInformation("Connecting to Admin Service at: {Url}", adminServiceUrl);

            var channel = GrpcChannel.ForAddress(adminServiceUrl);
            _client = new AdminService.AdminServiceClient(channel);
        }

        public async Task<UserResponse> ValidateAdminAsync(string token)
        {
            try
            {
                // For now, we'll use GetUser to validate admin access
                // You might need to implement a specific admin validation method
                var request = new GetUserRequest { UserId = token };
                return await _client.GetUserAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating admin with token: {Token}", token);
                return new UserResponse
                {
                    Success = false,
                    Error = "Admin validation failed"
                };
            }
        }


        public async Task<FetchAdminAccountResponse> FetchAdminAccountAsync(string userId)
        {
            try
            {
                var request = new FetchAdminAccountRequest { UserId = userId };
                var user = await _client.FetchAdminAccountAsync(request);
                _logger.LogInformation("Fetch admin account: {user})", user);


                return user;
            }
            catch (Exception ex)
            {
                return new FetchAdminAccountResponse
                {
                    Success = false,
                    Message = ex.Message
                    // only set fields that actually exist in your proto!
                    // e.g., maybe you have "AdminId" or "User" etc.
                };
            }
        }

        public async Task<FetchAdminAccountResponse> ValidateAdminAccountAsync(string token)
        {
            try
            {
                var request = new ValidateAdminAccountRequest { Token = token };
                var user =await _client.ValidateAdminAccountAsync(request);


                return new FetchAdminAccountResponse
                {
                    Success = true,
                    User = user.User
                };

            }
            catch (RpcException ex)
            {

                // You cannot return UserResponse here, must return FetchAdminAccountResponse
                // throw new FetchAdminAccountResponse
                return new FetchAdminAccountResponse
                {
                    Success = false,
                    Message = ex.Status.Detail
                    // only set fields that actually exist in your proto!
                    // e.g., maybe you have "AdminId" or "User" etc.
                };
            }
        }
    }
}
