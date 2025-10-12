// DTO for message with sender info

using Microsoft.CodeAnalysis;

namespace Comms.Models.DTOs
{
    public class CreateChatReponseDto
    {
        public string ChatId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; } = false;

        // Add other chat fields as needed
    }
}
