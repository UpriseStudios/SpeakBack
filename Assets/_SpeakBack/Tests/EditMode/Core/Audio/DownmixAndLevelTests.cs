using NUnit.Framework;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Core.Tests.Audio
{
    public sealed class DownmixAndLevelTests
    {
        [Test]
        public void ToMono_AveragesChannels()
        {
            var input = AudioBuffer.Create(new[] { 0.2f, 0.4f, -1f, 1f, 0.5f, 0.5f }, 48000, 2);

            var mono = Downmixer.ToMono(input);

            Assert.That(mono.Channels, Is.EqualTo(1));
            Assert.That(mono.SampleRate, Is.EqualTo(48000));
            Assert.That(mono.ToArray(), Is.EqualTo(new[] { 0.3f, 0f, 0.5f }).Within(1e-6f));
        }

        [Test]
        public void ToMono_ReturnsSameInstance_WhenAlreadyMono()
        {
            var input = AudioBuffer.Create(new[] { 0.1f }, 16000, 1);

            Assert.That(Downmixer.ToMono(input), Is.SameAs(input));
        }

        [Test]
        public void Analyze_ReportsPeakRmsAndClipping()
        {
            var input = AudioBuffer.Create(new[] { 0.5f, -0.5f, 1f, -0.995f }, 16000, 1);

            var level = LevelAnalyzer.Analyze(input);

            Assert.That(level.Peak, Is.EqualTo(1f));
            Assert.That(level.Rms, Is.EqualTo(System.Math.Sqrt((0.25 + 0.25 + 1 + 0.990025) / 4)).Within(1e-6));
            Assert.That(level.ClippingRatio, Is.EqualTo(0.5f));
        }

        [Test]
        public void Analyze_EmptyBuffer_IsAllZero()
        {
            var level = LevelAnalyzer.Analyze(AudioBuffer.Silence(0, 16000, 1));

            Assert.That(level.Peak, Is.EqualTo(0f));
            Assert.That(level.Rms, Is.EqualTo(0f));
            Assert.That(level.ClippingRatio, Is.EqualTo(0f));
        }

        [Test]
        public void Rms_OfFullScaleSine_IsAboutOneOverRootTwo()
        {
            var sine = TestSignals.Sine(1000, 1f, 1.0, 48000);

            Assert.That(LevelAnalyzer.Rms(sine), Is.EqualTo(1.0 / System.Math.Sqrt(2.0)).Within(1e-3));
        }
    }
}
