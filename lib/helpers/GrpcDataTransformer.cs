using System.Text.Json;
using System.Text.Json.Serialization;

namespace Comms.Helpers
{
    public static class GrpcDataTransformer
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Converts a gRPC response to JSON with snake_case naming
        /// </summary>
        public static string ToSnakeCaseJson<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, JsonOptions);
        }

        /// <summary>
        /// Deserializes JSON with snake_case to C# object with PascalCase
        /// </summary>
        public static T? FromSnakeCaseJson<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        /// <summary>
        /// Converts user data from gRPC response to API response format
        /// </summary>
        public static object TransformUserResponse(object userData)
        {
            var json = JsonSerializer.Serialize(userData, JsonOptions);
            return JsonSerializer.Deserialize<object>(json, JsonOptions) ?? new { };
        }
    }
}
