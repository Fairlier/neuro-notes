

namespace NeuroNotes.Application.Common.Options
{
    public class MinioOptions
    {
        public const string SectionName = "MinioSettings";

        public string Endpoint { get; set; } = string.Empty; 
        public string AccessKey { get; set; } = string.Empty; 
        public string SecretKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public bool WithSSL { get; set; } = false;
    }
}
