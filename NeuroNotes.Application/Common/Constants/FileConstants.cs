
namespace NeuroNotes.Application.Common.Constants
{
    public static class FileConstants
    {
        public static readonly IReadOnlyList<string> SupportedAudioExtensions = new[]
        {
            ".mp3", ".wav", ".ogg", ".m4a", ".flac"
        };

        public static string GetSupportedExtensionsString()
            => string.Join(", ", SupportedAudioExtensions);

        public const long MaxAudioUploadSizeBytes = 500 * 1024 * 1024;
        public const long MaxAudioApiProcessingSizeBytes = 10 * 1024 * 1024;

        public static string MaxAudioUploadSizeMbString => $"{MaxAudioUploadSizeBytes / 1024 / 1024} MB";
        public static string MaxAudioApiProcessingSizeMbString => $"{MaxAudioApiProcessingSizeBytes / 1024 / 1024} MB";
    }
}
