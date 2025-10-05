using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using System.Reflection;

public class RejectExtraPropertiesFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionArguments.Count == 0)
        {
            await next();
            return;
        }

        var httpContext = context.HttpContext;
        httpContext.Request.EnableBuffering(); // allow multiple reads

        using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        httpContext.Request.Body.Position = 0;

        if (!string.IsNullOrEmpty(body))
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(body);
                
                // Find the DTO that matches the JSON structure
                var targetDto = FindMatchingDto(context.ActionArguments.Values, jsonDoc.RootElement);
                if (targetDto != null)
                {
                    var validationResult = ValidateDto(targetDto, jsonDoc.RootElement, "");
                    if (!validationResult.IsValid)
                    {
                        context.Result = new BadRequestObjectResult(new
                        {
                            message = validationResult.ErrorMessage
                        });
                        return;
                    }
                }
            }
            catch (JsonException)
            {
                // If JSON is invalid, let the model binding handle it
                await next();
                return;
            }
        }

        await next();
    }

    private object? FindMatchingDto(System.Collections.Generic.ICollection<object?> actionArguments, JsonElement jsonElement)
    {
        // Get all property names from JSON
        var jsonPropertyNames = jsonElement.EnumerateObject()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Find DTO that has properties matching the JSON
        foreach (var dto in actionArguments)
        {
            if (dto == null) continue;

            var dtoType = dto.GetType();
            var dtoPropertyNames = dtoType.GetProperties()
                .Where(p => p.CanWrite)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Check if all JSON properties exist in DTO
            if (jsonPropertyNames.IsSubsetOf(dtoPropertyNames))
            {
                return dto;
            }
        }

        // If no exact match, return the first DTO (fallback)
        return actionArguments.OfType<object>().FirstOrDefault();
    }

    private (bool IsValid, string ErrorMessage) ValidateDto(object dto, JsonElement jsonElement, string path)
    {
        if (dto == null) return (true, "");

        var dtoType = dto.GetType();
        var dtoProperties = dtoType.GetProperties()
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var prop in jsonElement.EnumerateObject())
        {
            var propertyPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
            
            if (!dtoProperties.ContainsKey(prop.Name))
            {
                return (false, $"Unexpected property '{propertyPath}' in request payload");
            }

            var dtoProperty = dtoProperties[prop.Name];
            var dtoPropertyType = dtoProperty.PropertyType;

            // Check nested objects
            if (prop.Value.ValueKind == JsonValueKind.Object && 
                !dtoPropertyType.IsPrimitive && 
                dtoPropertyType != typeof(string) &&
                dtoPropertyType != typeof(Guid) &&
                dtoPropertyType != typeof(DateTime) &&
                !dtoPropertyType.IsGenericType) // Avoid collections
            {
                // Create instance of nested DTO for validation
                var nestedDto = Activator.CreateInstance(dtoPropertyType);
                var nestedValidation = ValidateDto(nestedDto!, prop.Value, propertyPath);
                if (!nestedValidation.IsValid)
                {
                    return nestedValidation;
                }
            }
        }

        return (true, "");
    }
}
