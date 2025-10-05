using Grpc.Net.Client;
using Profile; // this namespace comes from the proto file
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Comms.Helpers;

namespace Comms.Services
{
    public interface IProfileGrpcService
    {
        Task<UserResponse> ValidateUserAsync(string token);
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
            var profileServiceUrl = Environment.GetEnvironmentVariable("PROFILE_SERVICE_URL") 
                ?? configuration["Microservices:ProfileService"] 
                ?? "http://localhost:50051";
                
            // Ensure the URL has a valid scheme for gRPC
            if (!profileServiceUrl.StartsWith("http://") && !profileServiceUrl.StartsWith("https://"))
            {
                profileServiceUrl = $"http://{profileServiceUrl}";
            }
                
            _logger.LogInformation("Connecting to Profile Service at: {Url}", profileServiceUrl);
            
            var channel = GrpcChannel.ForAddress(profileServiceUrl);
            _client = new ProfileService.ProfileServiceClient(channel);
        }

        public async Task<UserResponse> ValidateUserAsync(string token)
        {
            try
            {
                // For now, we'll use GetUser to validate user access
                // You might need to implement a specific user validation method
                var request = new GetUserRequest { UserId = token };
                return await _client.GetUserAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user with token: {Token}", token);
                return new UserResponse 
                { 
                    Success = false, 
                    Error = "User validation failed" 
                };
            }
        }

        public async Task<UserResponse> GetUserAsync(string userId)
        {
            try
            {
                var request = new GetUserRequest { UserId = userId };
                return await _client.GetUserAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user: {UserId}", userId);
                return new UserResponse 
                { 
                    Success = false, 
                    Error = "Failed to get user" 
                };
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
                    Error = response.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return new UserResponse 
                { 
                    Success = false, 
                    Error = "Failed to get user by email" 
                };
            }
        }
    }
}
