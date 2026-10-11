using System;
using System.Text;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Core.Tests.Audio
{
    public sealed class WavEncoderTests
    {
        [Test]
        public void Header_DescribesMono16k()
        {
            var bytes = WavEncoder.EncodePcm16(AudioBuffer.Create(new[] { 0f, 0.5f, -0.5f }, 16000, 1));

            Assert.That(Encoding.ASCII.GetString(bytes, 0, 4), Is.EqualTo("RIFF"));
            Assert.That(BitConverter.ToInt32(bytes, 4), Is.EqualTo(36 + 6));
            Assert.That(Encoding.ASCII.GetString(bytes, 8, 8), Is.EqualTo("WAVEfmt "));
            Assert.That(BitConverter.ToInt32(bytes, 16), Is.EqualTo(16), "fmt size");
            Assert.That(BitConverter.ToInt16(bytes, 20), Is.EqualTo(1), "PCM");
            Assert.That(BitConverter.ToInt16(bytes, 22), Is.EqualTo(1), "channels");
            Assert.That(BitConverter.ToInt32(bytes, 24), Is.EqualTo(16000), "rate");
            Assert.That(BitConverter.ToInt32(bytes, 28), Is.EqualTo(32000), "byte rate");
            Assert.That(BitConverter.ToInt16(bytes, 32), Is.EqualTo(2), "block align");
            Assert.That(BitConverter.ToInt16(bytes, 34), Is.EqualTo(16), "bits");
            Assert.That(Encoding.ASCII.GetString(bytes, 36, 4), Is.EqualTo("data"));
            Assert.That(BitConverter.ToInt32(bytes, 40), Is.EqualTo(6));
            Assert.That(bytes.Length, Is.EqualTo(50));
        }

        [Test]
        public void Samples_ConvertSymmetrically()
        {
            var bytes = WavEncoder.EncodePcm16(AudioBuffer.Create(new[] { 1f, -1f, 0.5f, 0f }, 8000, 1));

            Assert.That(BitConverter.ToInt16(bytes, 44), Is.EqualTo(32767));
            Assert.That(BitConverter.ToInt16(bytes, 46), Is.EqualTo(-32767));
            Assert.That(BitConverter.ToInt16(bytes, 48), Is.EqualTo(16384));
            Assert.That(BitConverter.ToInt16(bytes, 50), Is.EqualTo(0));
        }

        [Test]
        public void OutOfRangeSamples_AreClampedNotWrapped()
        {
            var bytes = WavEncoder.EncodePcm16(AudioBuffer.Create(new[] { 1.7f, -3f }, 8000, 1));

            Assert.That(BitConverter.ToInt16(bytes, 44), Is.EqualTo(32767));
            Assert.That(BitConverter.ToInt16(bytes, 46), Is.EqualTo(-32767));
        }

        [TestCase(1, 16000)]
        [TestCase(2, 48000)]
        public void RoundTrip_PreservesShapeAndSamples(int channels, int rate)
        {
            var rng = new TestSignals.Rng(99);
            var samples = new float[300 * channels];
            for (var i = 0; i < samples.Length; i++) samples[i] = rng.NextSigned();
            var input = AudioBuffer.Create(samples, rate, channels);

            var output = WavEncoder.DecodePcm16(WavEncoder.EncodePcm16(input));

            Assert.That(output.SampleRate, Is.EqualTo(rate));
            Assert.That(output.Channels, Is.EqualTo(channels));
            Assert.That(output.ToArray(), Is.EqualTo(samples).Within(1f / 32767f));
        }

        [Test]
        public void Decode_SkipsUnknownChunks()
        {
            var plain = WavEncoder.EncodePcm16(AudioBuffer.Create(new[] { 0.25f, -0.25f }, 22050, 1));
            // Insert a 3-byte LIST chunk (odd size, so 1 pad byte) between fmt and data.
            var extra = new byte[] { (byte)'L', (byte)'I', (byte)'S', (byte)'T', 3, 0, 0, 0, 1, 2, 3, 0 };
            var bytes = new byte[plain.Length + extra.Length];
            Array.Copy(plain, 0, bytes, 0, 36);
            Array.Copy(extra, 0, bytes, 36, extra.Length);
            Array.Copy(plain, 36, bytes, 36 + extra.Length, plain.Length - 36);

            var output = WavEncoder.DecodePcm16(bytes);

            Assert.That(output.ToArray(), Is.EqualTo(new[] { 0.25f, -0.25f }).Within(1f / 32767f));
        }

        [Test]
        public void Decode_RejectsNonWav()
        {
            Assert.Throws<FormatException>(() => WavEncoder.DecodePcm16(Encoding.ASCII.GetBytes("not a wave file")));
        }

        [Test]
        public void Decode_RejectsNon16Bit()
        {
            var bytes = WavEncoder.EncodePcm16(AudioBuffer.Create(new[] { 0f }, 8000, 1));
            bytes[34] = 8;

            Assert.Throws<FormatException>(() => WavEncoder.DecodePcm16(bytes));
        }

        [Test]
        public void EmptyBuffer_EncodesHeaderOnly()
        {
            var bytes = WavEncoder.EncodePcm16(AudioBuffer.Silence(0, 16000, 1));

            Assert.That(bytes.Length, Is.EqualTo(44));
            Assert.That(WavEncoder.DecodePcm16(bytes).FrameCount, Is.EqualTo(0));
        }
    }
}
