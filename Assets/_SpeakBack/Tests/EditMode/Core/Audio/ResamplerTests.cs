using System;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Core.Tests.Audio
{
    public sealed class ResamplerTests
    {
        [TestCase(48000, 16000, 1.0)]
        [TestCase(44100, 16000, 1.0)]
        [TestCase(48000, 16000, 0.7731)]
        [TestCase(44100, 16000, 2.3457)]
        [TestCase(22050, 16000, 0.5)]
        [TestCase(16000, 48000, 0.3333)]
        [TestCase(8000, 16000, 1.25)]
        public void KeepsDuration_WithinOneFrame(int source, int target, double seconds)
        {
            var input = TestSignals.Mono(TestSignals.Sine(440, 0.5f, seconds, source), source);

            var output = Resampler.Resample(input, target);

            var expectedFrames = (double)input.FrameCount * target / source;
            Assert.That(output.SampleRate, Is.EqualTo(target));
            Assert.That(output.Channels, Is.EqualTo(1));
            Assert.That(Math.Abs(output.FrameCount - expectedFrames), Is.LessThanOrEqualTo(1.0));
            Assert.That(Math.Abs(output.DurationSeconds - input.DurationSeconds), Is.LessThanOrEqualTo(1.0 / target));
        }

        [TestCase(48000)]
        [TestCase(44100)]
        public void PassBandTone_KeepsFrequencyAndLevel(int source)
        {
            var input = TestSignals.Mono(TestSignals.Sine(1000, 0.5f, 1.0, source), source);

            var output = Resampler.Resample(input, 16000);

            var samples = output.Samples;
            var start = 1600;
            var end = samples.Length - 1600;
            Assert.That(TestSignals.ZeroCrossingFrequency(samples, start, end, 16000), Is.EqualTo(1000.0).Within(1.0));
            var gainDb = 20.0 * Math.Log10(TestSignals.Rms(samples, start, end) / (0.5 / Math.Sqrt(2.0)));
            Assert.That(gainDb, Is.EqualTo(0.0).Within(0.1));
        }

        [Test]
        public void ToneAboveCutOff_IsStronglyAttenuated_48kTo16k()
        {
            // Cut-off is 0.9 × 8 kHz = 7.2 kHz; 7.5 kHz sits in the transition band above it.
            var output = Resampler.Resample(TestSignals.Mono(TestSignals.Sine(7500, 0.5f, 1.0, 48000), 48000), 16000);

            var attenuationDb = AttenuationDb(output, 0.5f);
            TestContext.WriteLine($"7.5 kHz after 48k→16k: {attenuationDb:F1} dB");
            Assert.That(attenuationDb, Is.LessThan(-20.0));
        }

        [TestCase(48000, 10000)]
        [TestCase(48000, 12000)]
        [TestCase(44100, 11000)]
        public void ToneAboveTargetNyquist_DoesNotAlias(int source, double frequency)
        {
            // Above 8 kHz a tone would fold back into the band; the filter must remove it.
            var output = Resampler.Resample(TestSignals.Mono(TestSignals.Sine(frequency, 0.5f, 1.0, source), source), 16000);

            var attenuationDb = AttenuationDb(output, 0.5f);
            TestContext.WriteLine($"{frequency} Hz after {source}→16k: {attenuationDb:F1} dB");
            Assert.That(attenuationDb, Is.LessThan(-60.0));
        }

        [Test]
        public void Upsampling_KeepsFrequency()
        {
            var output = Resampler.Resample(TestSignals.Mono(TestSignals.Sine(1000, 0.5f, 1.0, 16000), 16000), 48000);

            var samples = output.Samples;
            Assert.That(TestSignals.ZeroCrossingFrequency(samples, 4800, samples.Length - 4800, 48000), Is.EqualTo(1000.0).Within(1.0));
        }

        [Test]
        public void SameRate_ReturnsInputInstance()
        {
            var input = TestSignals.Mono(TestSignals.Sine(440, 0.5f, 0.1, 16000), 16000);

            Assert.That(Resampler.Resample(input, 16000), Is.SameAs(input));
        }

        [Test]
        public void IsDeterministic()
        {
            var input = TestSignals.Mono(TestSignals.Noise(0.2f, 0.5, 44100, new TestSignals.Rng(7)), 44100);

            var first = Resampler.Resample(input, 16000).ToArray();
            var second = Resampler.Resample(input, 16000).ToArray();

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void DcSignal_StaysAtSameLevel_IncludingEdges()
        {
            var input = TestSignals.Mono(Fill(0.25f, 4800), 48000);

            var output = Resampler.Resample(input, 16000);

            Assert.That(output.ToArray(), Is.All.EqualTo(0.25f).Within(1e-4f));
        }

        [Test]
        public void RejectsMultiChannelInput()
        {
            var stereo = AudioBuffer.Create(new float[8], 48000, 2);

            Assert.Throws<ArgumentException>(() => Resampler.Resample(stereo, 16000));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void RejectsNonPositiveTarget(int target)
        {
            var input = AudioBuffer.Create(new float[8], 48000, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => Resampler.Resample(input, target));
        }

        [Test]
        public void Empty_StaysEmpty()
        {
            var output = Resampler.Resample(AudioBuffer.Silence(0, 48000, 1), 16000);

            Assert.That(output.FrameCount, Is.EqualTo(0));
            Assert.That(output.SampleRate, Is.EqualTo(16000));
        }

        private static double AttenuationDb(AudioBuffer output, float inputAmplitude)
        {
            var samples = output.Samples;
            var rms = TestSignals.Rms(samples, 1600, samples.Length - 1600);
            return 20.0 * Math.Log10(Math.Max(rms, 1e-12) / (inputAmplitude / Math.Sqrt(2.0)));
        }

        private static float[] Fill(float value, int count)
        {
            var samples = new float[count];
            for (var i = 0; i < count; i++) samples[i] = value;
            return samples;
        }
    }
}
