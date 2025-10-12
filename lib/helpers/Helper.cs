using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Comms.Helpers
{
    public class RoutePrefixConvention : IApplicationModelConvention
    {
        private readonly AttributeRouteModel _routePrefix;

        public RoutePrefixConvention(string prefix)
        {
            _routePrefix = new AttributeRouteModel(new RouteAttribute(prefix));
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                foreach (var selector in controller.Selectors)
                {
                    if (selector.AttributeRouteModel != null)
                    {
                        // Combine prefix + existing route
                        selector.AttributeRouteModel =
                            AttributeRouteModel.CombineAttributeRouteModel(
                                _routePrefix,
                                selector.AttributeRouteModel
                            );
                    }
                    else
                    {
                        // No route set → just apply prefix
                        selector.AttributeRouteModel = _routePrefix;
                    }
                }
            }
        }
    }

    public class FileValidator
    {
        // ✅ Check if a string is a valid image URL or Base64 image
        public bool IsValidImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            // Check for Base64 images first
            if (url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                return true;

            try
            {
                var parsedUrl = new Uri(url);
                var path = parsedUrl.AbsolutePath.ToLowerInvariant();

                string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };

                return imageExtensions.Any(ext => path.EndsWith(ext));
            }
            catch
            {
                // If it's not a valid URL, just return false
                return false;
            }
        }

        // ✅ Check if a file path or URL points to an audio file
        public bool IsAudioFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            path = path.ToLowerInvariant();

            string[] audioExtensions = { ".mp3", ".wav", ".ogg", ".webm", ".m4a" };
            bool hasAudioExtension = audioExtensions.Any(ext => path.EndsWith(ext));

            bool isSupabaseAudio = path.Contains("supabase") && path.Contains("/audio/");

            return hasAudioExtension || isSupabaseAudio;
        }
    }
}
