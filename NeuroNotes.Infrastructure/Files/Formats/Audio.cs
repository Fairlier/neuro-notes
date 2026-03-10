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

    public class Webm : Audio
    {
        public Webm() : base(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, "audio/webm", "webm") { }
    }

    public abstract class IsoBaseMediaAudio : Audio
    {
        private readonly string[] _compatibleBrands;

        protected IsoBaseMediaAudio(string mediaType, string extension, string[] compatibleBrands)
            : base(Array.Empty<byte>(), mediaType, extension)
        {
            _compatibleBrands = compatibleBrands;
        }

        public override bool IsMatch(Stream stream)
        {
            if (stream == null || stream.Length < 12)
                return false;

            var buffer = new byte[12];
            var bytesRead = stream.Read(buffer, 0, 12);

            if (bytesRead < 12)
                return false;

            if (buffer[4] != 0x66 || 
                buffer[5] != 0x74 || 
                buffer[6] != 0x79 || 
                buffer[7] != 0x70)   
            {
                return false;
            }

            var brand = System.Text.Encoding.ASCII.GetString(buffer, 8, 4).TrimEnd('\0', ' ');

            return _compatibleBrands.Any(b => brand.StartsWith(b, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class M4a : IsoBaseMediaAudio
    {
        private static readonly string[] M4aBrands = new[]
        {
            "M4A",  
            "M4B",  
            "M4P",  
            "mp42"  
        };

        public M4a() : base("audio/mp4", "m4a", M4aBrands) { }
    }

    public class Mp4Audio : IsoBaseMediaAudio
    {
        private static readonly string[] Mp4Brands = new[]
        {
            "isom",  
            "iso2",  
            "mp41",  
            "mp42",  
            "avc1",  
            "dash",  
            "mmp4",  
            "MSNV",  
            "3gp",   
            "3g2",   
            "NDAS"   
        };

        public Mp4Audio() : base("audio/mp4", "mp4", Mp4Brands) { }
    }
}
