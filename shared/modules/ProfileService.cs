using Comms.Helpers;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Profile; // this namespace comes from the proto file

namespace Comms.Services
{
    public interface IProfileGrpcService
    {
        Task<ValidateAccountResponse> ValidateAccountAsync(string token);
        Task<UserResponse> GetUserAsync(string userId);
        Task<UserResponse> GetUserByEmailAsync(string email);
    }

    public class ProfileGrpcService : IProfileGrpcService
    {
        private readonly ProfileService.ProfileServiceClient _client;
        private readonly ILogger<ProfileGrpcService> _logger;

        public ProfileGrpcService(IConfiguration configuration, ILogger<ProfileGrpcService> logger)
        {
            _logger = logger;

            // Get profile service URL from environment or configuration
            var profileServiceUrl =
                Environment.GetEnvironmentVariable("PROFILE_SERVICE_URL")
                ?? configuration["Microservices:ProfileService"]
                ?? "http://localhost:50051";

            // Ensure the URL has a valid scheme for gRPC
            if (
                !profileServiceUrl.StartsWith("http://")
                && !profileServiceUrl.StartsWith("https://")
            )
            {
                profileServiceUrl = $"http://localhost:50051";
            }

            var channel = GrpcChannel.ForAddress(profileServiceUrl);
            _client = new ProfileService.ProfileServiceClient(channel);
        }

        public async Task<ValidateAccountResponse> ValidateAccountAsync(string token)
        {
            try
            {
                // For now, we'll use GetUser to validate user access
                // You might need to implement a specific user validation method
                var request = new ValidateAccountRequest { Token = token };
                var texted = await _client.ValidateAccountAsync(request);
                return texted;
            }
            catch (RpcException ex)
            {
                _logger.LogInformation(
                    "RPC Exception during token validation: {Message}",
                    ex.Message
                );
                return new ValidateAccountResponse
                {
                    Success = false,
                    Message = ex.Status.Detail,
                    // only set fields that actually exist in your proto!
                    // e.g., maybe you have "AdminId" or "User" etc.
                };
            }
        }

        public async Task<UserResponse> GetUserAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Fetching user details for UserId: {UserId}", userId);
                var request = new GetUserRequest { UserId = userId };
                return await _client.GetUserAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user: {UserId}", userId);
                return new UserResponse { Success = false, Error = "Failed to get user" };
            }
        }

        public async Task<UserResponse> GetUserByEmailAsync(string email)
        {
            try
            {
                var request = new GetUserByEmailRequest { Email = email };
                var response = await _client.GetUserByEmailAsync(request);
                return new UserResponse
                {
                    Success = response.Success,
                    User = response.User,
                    Error = response.Error,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return new UserResponse { Success = false, Error = "Failed to get user by email" };
            }
        }
    }
}
