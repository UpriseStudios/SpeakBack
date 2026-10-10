using NUnit.Framework;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Core.Tests.Audio
{
    public sealed class AudioProcessorTests
    {
        private const int CaptureRate = 48000;

        private static AudioBuffer StereoCapture()
        {
            var left = TestSignals.Concat(
                TestSignals.Silence(0.3, CaptureRate),
                TestSignals.Sine(440, 0.3f, 0.8, CaptureRate),
                TestSignals.Silence(0.3, CaptureRate));
            var right = TestSignals.Concat(
                TestSignals.Silence(0.3, CaptureRate),
                TestSignals.Sine(660, 0.2f, 0.8, CaptureRate),
                TestSignals.Silence(0.3, CaptureRate));
            return AudioBuffer.Create(TestSignals.Interleave(left, right), CaptureRate, 2);
        }

        [Test]
        public void EndToEnd_StereoCapture_ProducesConsistentAttempts()
        {
            var capture = StereoCapture();
            var config = ProcessorConfig.Default;

            var result = AudioProcessor.Process(capture, config);

            Assert.That(result.Status, Is.EqualTo(AudioProcessStatus.Ok));

            // RawAttempt == trim(mono)
            var mono = Downmixer.ToMono(capture);
            var trim = SilenceTrimmer.Trim(mono, config);
            var expectedRaw = mono.SliceFrames(trim.StartFrame, trim.FrameCount);
            Assert.That(result.RawAttempt.ToArray(), Is.EqualTo(expectedRaw.ToArray()));
            Assert.That(result.RawAttempt.SampleRate, Is.EqualTo(CaptureRate));
            Assert.That(result.RawAttempt.Channels, Is.EqualTo(1));

            // FlippedAttempt == reverse(RawAttempt)
            Assert.That(result.FlippedAttempt.ToArray(), Is.EqualTo(AudioReverser.ReverseFrames(result.RawAttempt).ToArray()));
            Assert.That(result.FlippedAttempt.SampleRate, Is.EqualTo(CaptureRate));

            // ScorerInput == FlippedAttempt at 16 kHz
            Assert.That(result.ScorerInput.SampleRate, Is.EqualTo(16000));
            Assert.That(result.ScorerInput.Channels, Is.EqualTo(1));
            Assert.That(result.ScorerInput.ToArray(), Is.EqualTo(Resampler.Resample(result.FlippedAttempt, 16000).ToArray()));
            Assert.That(result.ScorerInput.DurationSeconds, Is.EqualTo(result.FlippedAttempt.DurationSeconds).Within(1.0 / 16000));
        }

        [Test]
        public void EndToEnd_ReportsDiagnostics()
        {
            var result = AudioProcessor.Process(StereoCapture(), ProcessorConfig.Default);
            var d = result.Diagnostics;

            Assert.That(d.CaptureRate, Is.EqualTo(CaptureRate));
            Assert.That(d.CaptureChannels, Is.EqualTo(2));
            Assert.That(d.CaptureSeconds, Is.EqualTo(1.4).Within(1e-6));
            Assert.That(d.CapturePeak, Is.EqualTo(0.3f).Within(1e-3f));
            Assert.That(d.ClippingRatio, Is.EqualTo(0f));
            Assert.That(d.VoicedSeconds, Is.EqualTo(0.8).Within(0.02));
            Assert.That(d.RetainedSeconds, Is.EqualTo(1.0).Within(0.04));
            Assert.That(d.AppliedGain, Is.EqualTo(1f));
        }

        [Test]
        public void Silence_IsNoVoice_WithoutAttempts()
        {
            var result = AudioProcessor.Process(AudioBuffer.Silence(48000, CaptureRate, 1), ProcessorConfig.Default);

            Assert.That(result.Status, Is.EqualTo(AudioProcessStatus.NoVoice));
            Assert.That(result.RawAttempt, Is.Null);
            Assert.That(result.FlippedAttempt, Is.Null);
            Assert.That(result.ScorerInput, Is.Null);
            Assert.That(result.Diagnostics, Is.Not.Null);
        }

        [Test]
        public void ShortBlip_IsTooShort_WithoutAttempts()
        {
            var samples = TestSignals.Concat(
                TestSignals.Silence(0.4, CaptureRate),
                TestSignals.Sine(300, 0.3f, 0.1, CaptureRate),
                TestSignals.Silence(0.4, CaptureRate));

            var result = AudioProcessor.Process(TestSignals.Mono(samples, CaptureRate), ProcessorConfig.Default);

            Assert.That(result.Status, Is.EqualTo(AudioProcessStatus.TooShort));
            Assert.That(result.ScorerInput, Is.Null);
        }

        [Test]
        public void Gain_WhenEnabled_IsBoundedAndPeakProtected()
        {
            var samples = TestSignals.Concat(
                TestSignals.Silence(0.2, 16000),
                TestSignals.Sine(300, 0.05f, 0.6, 16000),
                TestSignals.Silence(0.2, 16000));
            var config = new ProcessorConfig(gainEnabled: true);

            var result = AudioProcessor.Process(TestSignals.Mono(samples, 16000), config);

            Assert.That(result.Status, Is.EqualTo(AudioProcessStatus.Ok));
            Assert.That(result.Diagnostics.AppliedGain, Is.EqualTo(config.GainMaxLinear).Within(1e-6f));
            Assert.That(LevelAnalyzer.Analyze(result.RawAttempt).Peak, Is.LessThanOrEqualTo(config.GainTargetPeak));
        }

        [Test]
        public void Gain_NeverTurnsSubThresholdNoiseIntoVoice()
        {
            var rng = new TestSignals.Rng(9);
            var quiet = TestSignals.Noise(0.004f, 1.0, 16000, rng);
            var config = new ProcessorConfig(gainEnabled: true);

            var result = AudioProcessor.Process(TestSignals.Mono(quiet, 16000), config);

            Assert.That(result.Status, Is.EqualTo(AudioProcessStatus.NoVoice));
        }

        [Test]
        public void Gain_LoudAttempt_IsNotAttenuated()
        {
            var samples = TestSignals.Concat(
                TestSignals.Silence(0.2, 16000),
                TestSignals.Sine(300, 0.95f, 0.6, 16000),
                TestSignals.Silence(0.2, 16000));

            var result = AudioProcessor.Process(TestSignals.Mono(samples, 16000), new ProcessorConfig(gainEnabled: true));

            Assert.That(result.Diagnostics.AppliedGain, Is.EqualTo(1f));
        }

        [Test]
        public void CaptureAlreadyAtScorerRate_ScorerInputIsTheFlippedAttempt()
        {
            var samples = TestSignals.Concat(
                TestSignals.Silence(0.2, 16000),
                TestSignals.Sine(300, 0.3f, 0.6, 16000),
                TestSignals.Silence(0.2, 16000));

            var result = AudioProcessor.Process(TestSignals.Mono(samples, 16000), ProcessorConfig.Default);

            Assert.That(result.ScorerInput, Is.SameAs(result.FlippedAttempt));
        }
    }
}
