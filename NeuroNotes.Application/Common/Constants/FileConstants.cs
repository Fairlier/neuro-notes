
namespace NeuroNotes.Application.Common.Constants
{
    public static class FileConstants
    {
        public static readonly IReadOnlyList<string> SupportedAudioExtensions = new[]
        {
            ".mp3", ".wav", ".ogg", ".m4a", ".flac", ".webm", ".mp4"
        };

        public static readonly IReadOnlyList<string> SupportedAudioContentTypes = new[]
        {
            "audio/mpeg",
            "audio/wav",
            "audio/wave",
            "audio/x-wav",
            "audio/ogg",
            "audio/mp4",
            "audio/x-m4a",
            "audio/flac",
            "audio/webm"
        };

        private static readonly IReadOnlyDictionary<string, string> AudioExtensionToContentType = new Dictionary<string, string>
        {
            { ".mp3", "audio/mpeg" },
            { ".wav", "audio/wav" },
            { ".ogg", "audio/ogg" },
            { ".m4a", "audio/mp4" },
            { ".flac", "audio/flac" },
            { ".webm", "audio/webm" },
            { ".mp4", "audio/mp4" }
        };

        public const long MaxAudioUploadSizeBytes = 500 * 1024 * 1024;
        public const long MaxAudioApiProcessingSizeBytes = 10 * 1024 * 1024;

        public static string GetSupportedAudioExtensionsString()
            => string.Join(", ", SupportedAudioExtensions);

        public static string GetSupportedAudioContentTypesString()
            => string.Join(", ", SupportedAudioContentTypes);

        public static string MaxAudioUploadSizeMbString => $"{MaxAudioUploadSizeBytes / 1024 / 1024} MB";
        public static string MaxAudioApiProcessingSizeMbString => $"{MaxAudioApiProcessingSizeBytes / 1024 / 1024} MB";

        public static readonly IReadOnlyList<string> SupportedImageExtensions = new[]
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        public static readonly IReadOnlyList<string> SupportedImageContentTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp"
        };

        private static readonly IReadOnlyDictionary<string, string> ImageExtensionToContentType = new Dictionary<string, string>
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".webp", "image/webp" }
        };

        public const long MaxImageUploadSizeBytes = 10 * 1024 * 1024;

        public static string GetSupportedImageExtensionsString()
            => string.Join(", ", SupportedImageExtensions);

        public static string GetSupportedImageContentTypesString()
            => string.Join(", ", SupportedImageContentTypes);

        public static string MaxImageUploadSizeMbString => $"{MaxImageUploadSizeBytes / 1024 / 1024} MB";

        public static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            if (AudioExtensionToContentType.TryGetValue(ext, out var audioType))
                return audioType;

            if (ImageExtensionToContentType.TryGetValue(ext, out var imageType))
                return imageType;

            return "application/octet-stream";
        }
    }
}
