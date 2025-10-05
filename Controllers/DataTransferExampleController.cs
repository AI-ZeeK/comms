using Microsoft.AspNetCore.Mvc;
using Comms.Constants;
using Comms.Helpers;

namespace Comms.Controllers
{
    [ApiController]
    [Route(ApiConstants.API_VERSION + "/[controller]")]
    public class DataTransferExampleController : ControllerBase
    {
        [HttpGet("test-naming")]
        public IActionResult TestNamingConvention()
        {
            // Example C# object with PascalCase
            var userData = new
            {
                UserId = "12345",
                FirstName = "John",
                LastName = "Doe",
                EmailAddress = "john.doe@example.com",
                PhoneNumber = "+1234567890",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                UserRole = "ADMIN"
            };

            // This will be automatically converted to snake_case in JSON response
            // {
            //   "user_id": "12345",
            //   "first_name": "John",
            //   "last_name": "Doe",
            //   "email_address": "john.doe@example.com",
            //   "phone_number": "+1234567890",
            //   "created_at": "2025-01-30T...",
            //   "is_active": true,
            //   "user_role": "admin"
            // }

            return Ok(new
            {
                message = "Data will be automatically converted to snake_case",
                data = userData,
                note = "This ensures compatibility with NestJS microservices"
            });
        }

        [HttpPost("test-receive")]
        public IActionResult TestReceiveSnakeCase([FromBody] TestDto testData)
        {
            // This endpoint can receive snake_case JSON and automatically convert to PascalCase
            return Ok(new
            {
                message = "Successfully received and converted snake_case to PascalCase",
                received_data = testData,
                converted_properties = new
                {
                    user_id = testData.UserId,
                    first_name = testData.FirstName,
                    email_address = testData.EmailAddress
                }
            });
        }
    }

    // DTO that can receive snake_case JSON
    public class TestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
    }
}
