
namespace markdown_app_aspnet.Utilities
{
    public static class MultipartRequestHelper
    {
        public static bool IsMultipartContentType(string? contentType) =>
            !string.IsNullOrEmpty(contentType) &&
            contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

}
