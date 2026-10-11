using System;
using System.Text;

namespace Uprise.SpeakBack.Core.Audio
{
    /// <summary>
    /// 16-bit PCM WAV encoding and decoding as pure byte work (no file IO). Used by dev tooling and
    /// fixtures; the player build never writes audio (Phase 1 §15).
    /// </summary>
    public static class WavEncoder
    {
        private const int HeaderBytes = 44;

        /// <summary>
        /// Encodes <paramref name="buffer"/> as a canonical 44-byte-header PCM WAV. Samples are clamped to
        /// [-1, 1] and scaled by 32767 with rounding, so encoding is symmetric and never wraps.
        /// </summary>
        public static byte[] EncodePcm16(AudioBuffer buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            var samples = buffer.Samples;
            var dataBytes = checked(samples.Length * 2);
            var bytes = new byte[HeaderBytes + dataBytes];
            var blockAlign = buffer.Channels * 2;

            WriteAscii(bytes, 0, "RIFF");
            WriteInt32(bytes, 4, 36 + dataBytes);
            WriteAscii(bytes, 8, "WAVE");
            WriteAscii(bytes, 12, "fmt ");
            WriteInt32(bytes, 16, 16);
            WriteInt16(bytes, 20, 1);
            WriteInt16(bytes, 22, (short)buffer.Channels);
            WriteInt32(bytes, 24, buffer.SampleRate);
            WriteInt32(bytes, 28, buffer.SampleRate * blockAlign);
            WriteInt16(bytes, 32, (short)blockAlign);
            WriteInt16(bytes, 34, 16);
            WriteAscii(bytes, 36, "data");
            WriteInt32(bytes, 40, dataBytes);

            var offset = HeaderBytes;
            for (var i = 0; i < samples.Length; i++)
            {
                var clamped = Math.Max(-1f, Math.Min(1f, samples[i]));
                WriteInt16(bytes, offset, (short)Math.Round(clamped * 32767f));
                offset += 2;
            }

            return bytes;
        }

        /// <summary>
        /// Decodes a 16-bit PCM WAV, skipping unknown chunks. Throws <see cref="FormatException"/> for
        /// anything else (compressed, 8/24/32-bit, truncated).
        /// </summary>
        public static AudioBuffer DecodePcm16(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length < 12 || ReadAscii(bytes, 0) != "RIFF" || ReadAscii(bytes, 8) != "WAVE")
            {
                throw new FormatException("Not a RIFF/WAVE file.");
            }

            int channels = 0, rate = 0, bits = 0;
            var position = 12;
            while (position + 8 <= bytes.Length)
            {
                var id = ReadAscii(bytes, position);
                var size = ReadInt32(bytes, position + 4);
                var body = position + 8;
                if (size < 0 || body + size > bytes.Length) throw new FormatException($"Chunk '{id}' runs past the end of the file.");

                if (id == "fmt ")
                {
                    if (size < 16) throw new FormatException("fmt chunk too short.");
                    if (ReadInt16(bytes, body) != 1) throw new FormatException("Only uncompressed PCM is supported.");
                    channels = ReadInt16(bytes, body + 2);
                    rate = ReadInt32(bytes, body + 4);
                    bits = ReadInt16(bytes, body + 14);
                }
                else if (id == "data")
                {
                    if (channels <= 0 || rate <= 0) throw new FormatException("data chunk before fmt chunk.");
                    if (bits != 16) throw new FormatException($"Only 16-bit PCM is supported (got {bits}-bit).");
                    var count = size / 2;
                    var samples = new float[count - count % channels];
                    for (var i = 0; i < samples.Length; i++)
                    {
                        samples[i] = Math.Max(-1f, ReadInt16(bytes, body + i * 2) / 32767f);
                    }

                    return AudioBuffer.FromOwnedArray(samples, rate, channels);
                }

                position = body + size + (size & 1);
            }

            throw new FormatException("No data chunk.");
        }

        private static void WriteAscii(byte[] bytes, int offset, string text) => Encoding.ASCII.GetBytes(text, 0, 4, bytes, offset);

        private static string ReadAscii(byte[] bytes, int offset) => Encoding.ASCII.GetString(bytes, offset, 4);

        private static void WriteInt32(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            bytes[offset + 2] = (byte)(value >> 16);
            bytes[offset + 3] = (byte)(value >> 24);
        }

        private static void WriteInt16(byte[] bytes, int offset, short value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
        }

        private static int ReadInt32(byte[] bytes, int offset) =>
            bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24);

        private static short ReadInt16(byte[] bytes, int offset) => (short)(bytes[offset] | (bytes[offset + 1] << 8));
    }
}
