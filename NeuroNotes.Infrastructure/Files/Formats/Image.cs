using FileSignatures;

namespace NeuroNotes.Infrastructure.Files.Formats
{
    public abstract class Image : FileFormat
    {
        protected Image(byte[] signature, string mediaType, string extension)
            : base(signature, mediaType, extension) { }
    }

    public class Jpeg : Image
    {
        public Jpeg() : base(new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg", "jpg") { }
    }

    public class Png : Image
    {
        public Png() : base(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image/png", "png") { }
    }

    public class Gif : Image
    {
        public Gif() : base(new byte[] { 0x47, 0x49, 0x46, 0x38 }, "image/gif", "gif") { }
    }

    public class WebP : Image
    {
        public WebP() : base(new byte[] { 0x52, 0x49, 0x46, 0x46 }, "image/webp", "webp") { }
    }
}
