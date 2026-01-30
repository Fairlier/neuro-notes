using FileSignatures;

namespace NeuroNotes.Infrastructure.Files.Formats
{
    public abstract class Audio : FileFormat
    {
        protected Audio(byte[] signature, string mediaType, string extension)
            : base(signature, mediaType, extension) { }
    }

    public class Mp3Id3 : Audio
    {
        public Mp3Id3() : base(new byte[] { 0x49, 0x44, 0x33 }, "audio/mpeg", "mp3") { }
    }

    public class Mp3Mpeg : Audio
    {
        public Mp3Mpeg() : base(new byte[] { 0xFF, 0xFB }, "audio/mpeg", "mp3") { }
    }

    public class Wav : Audio
    {
        public Wav() : base(new byte[] { 0x52, 0x49, 0x46, 0x46 }, "audio/wav", "wav") { }
    }

    public class Ogg : Audio
    {
        public Ogg() : base(new byte[] { 0x4F, 0x67, 0x67, 0x53 }, "audio/ogg", "ogg") { }
    }

    public class Flac : Audio
    {
        public Flac() : base(new byte[] { 0x66, 0x4C, 0x61, 0x43 }, "audio/flac", "flac") { }
    }

    public class M4a : Audio
    {
        public M4a() : base(new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x4D, 0x34, 0x41 }, "audio/mp4", "m4a") { }
    }
}
