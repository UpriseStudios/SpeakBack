using NUnit.Framework;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Core.Tests.Audio
{
    public sealed class SilenceTrimmerTests
    {
        private const int Rate = 16000;
        private static readonly ProcessorConfig Config = ProcessorConfig.Default;

        [Test]
        public void RemovesOuterSilence_KeepingPadding()
        {
            var samples = TestSignals.Concat(
                TestSignals.Silence(0.5, Rate),
                TestSignals.Sine(220, 0.3f, 0.6, Rate),
                TestSignals.Silence(0.7, Rate));

            var trim = SilenceTrimmer.Trim(TestSignals.Mono(samples, Rate), Config);

            Assert.That(trim.Status, Is.EqualTo(TrimStatus.Ok));
            // Voice spans [0.5 s, 1.1 s); padding is 100 ms; windows are 20 ms (320 frames).
            Assert.That(trim.StartFrame, Is.EqualTo(8000 - 1600).Within(320));
            Assert.That(trim.EndFrame, Is.EqualTo(17600 + 1600).Within(320));
            Assert.That(trim.StartFrame, Is.LessThanOrEqualTo(8000 - 1600));
            Assert.That(trim.EndFrame, Is.GreaterThanOrEqualTo(17600 + 1600));
        }

        [Test]
        public void KeepsInternalPause()
        {
            var samples = TestSignals.Concat(
                TestSignals.Silence(0.3, Rate),
                TestSignals.Sine(220, 0.3f, 0.3, Rate),
                TestSignals.Silence(0.3, Rate),
                TestSignals.Sine(330, 0.3f, 0.3, Rate),
                TestSignals.Silence(0.3, Rate));

            var trim = SilenceTrimmer.Trim(TestSignals.Mono(samples, Rate), Config);

            Assert.That(trim.Status, Is.EqualTo(TrimStatus.Ok));
            Assert.That(trim.StartFrame, Is.LessThanOrEqualTo((int)(0.3 * Rate)));
            Assert.That(trim.EndFrame, Is.GreaterThanOrEqualTo((int)(1.2 * Rate)));
            Assert.That(trim.VoicedEndFrame - trim.VoicedStartFrame, Is.GreaterThanOrEqualTo((int)(0.9 * Rate)));
        }

        [Test]
        public void KeepsQuietConsonantAtBoundary()
        {
            // A quiet fricative-like burst just above the base threshold, right before a loud vowel.
            var rng = new TestSignals.Rng(11);
            var consonantStart = (int)(0.4 * Rate);
            var samples = TestSignals.Concat(
                TestSignals.Silence(0.4, Rate),
                TestSignals.Noise(0.012f, 0.08, Rate, rng),
                TestSignals.Sine(200, 0.4f, 0.4, Rate),
                TestSignals.Silence(0.4, Rate));

            var trim = SilenceTrimmer.Trim(TestSignals.Mono(samples, Rate), Config);

            Assert.That(trim.Status, Is.EqualTo(TrimStatus.Ok));
            Assert.That(trim.VoicedStartFrame, Is.LessThanOrEqualTo(consonantStart + 320));
            Assert.That(trim.StartFrame, Is.LessThanOrEqualTo(consonantStart));
        }

        [Test]
        public void QuietConsonantBelowThreshold_IsStillRetainedByPadding()
        {
            // Even when a short consonant is under the threshold, the 100 ms padding keeps it.
            var rng = new TestSignals.Rng(12);
            var consonantStart = (int)(0.4 * Rate);
            var samples = TestSignals.Concat(
                TestSignals.Silence(0.4, Rate),
                TestSignals.Noise(0.005f, 0.06, Rate, rng),
                TestSignals.Sine(200, 0.4f, 0.4, Rate),
                TestSignals.Silence(0.4, Rate));

            var trim = SilenceTrimmer.Trim(TestSignals.Mono(samples, Rate), Config);

            Assert.That(trim.StartFrame, Is.LessThanOrEqualTo(consonantStart));
        }

        [Test]
        public void PureSilence_IsNoVoice()
        {
            var trim = SilenceTrimmer.Trim(TestSignals.Mono(TestSignals.Silence(2.0, Rate), Rate), Config);

            Assert.That(trim.Status, Is.EqualTo(TrimStatus.NoVoice));
            Assert.That(trim.VoicedWindowCount, Is.EqualTo(0));
        }

        [Test]
        public void LoneClick_IsNoVoice()
        {
            var samples = TestSignals.Concat(
                TestSignals.Silence(0.5, Rate),
                TestSignals.Sine(1000, 0.8f, 0.02, Rate),
                TestSignals.Silence(0.5, Rate));

            var trim = SilenceTrimmer.Trim(TestSignals.Mono(samples, Rate), Config);

            Assert.That(trim.Status, Is.EqualTo(TrimStatus.NoVoice));
        }

        [Test]
        public void TenthOfASecondOfVoice_IsTooShort()
        {
            var samples = TestSignals.Concat(
                TestSignals.Silence(0.5, Rate),
                TestSignals.Sine(220, 0.3f, 0.1, Rate),
                TestSignals.Silence(0.5, Rate));

            var trim = SilenceTrimmer.Trim(TestSignals.Mono(samples, Rate), Config);

            Assert.That(trim.Status, Is.EqualTo(TrimStatus.TooShort));
        }

        [Test]
        public void FastButCompleteAttempt_IsScored()
        {
            // P00 Q5: only the absolute 0.25 s floor applies, never the phrase's expected duration.
            var samples = TestSignals.Concat(
                TestSignals.Silence(0.2, Rate),
                TestSignals.Sine(220, 0.3f, 0.3, Rate),
                TestSignals.Silence(0.2, Rate));

            var trim = SilenceTrimmer.Trim(TestSignals.Mono(samples, Rate), Config);

            Assert.That(trim.Status, Is.EqualTo(TrimStatus.Ok));
        }

        [Test]
        public void NoisyRoom_TrimsToVoice_AboveNoiseFloor()
        {
            var rng = new TestSignals.Rng(3);
            var noise = TestSignals.Noise(0.03f, 2.0, Rate, rng);
            var voice = TestSignals.Concat(
                TestSignals.Silence(0.7, Rate),
                TestSignals.Sine(220, 0.35f, 0.6, Rate),
                TestSignals.Silence(0.7, Rate));
            var samples = TestSignals.Add(noise, voice);

            var trim = SilenceTrimmer.Trim(TestSignals.Mono(samples, Rate), Config);

            Assert.That(trim.Status, Is.EqualTo(TrimStatus.Ok));
            Assert.That(trim.NoiseFloorRms, Is.EqualTo(0.03f).Within(0.006f));
            Assert.That(trim.ThresholdRms, Is.GreaterThan(0.03f));
            Assert.That(trim.VoicedStartFrame, Is.EqualTo((int)(0.7 * Rate)).Within(320));
            Assert.That(trim.VoicedEndFrame, Is.EqualTo((int)(1.3 * Rate)).Within(320));
        }

        [Test]
        public void SpeechFillingWholeCapture_KeepsEverything()
        {
            // Hold-to-record with no silence either side: the start must not be cut.
            var rng = new TestSignals.Rng(5);
            var samples = TestSignals.Concat(
                TestSignals.Noise(0.02f, 0.06, Rate, rng),
                TestSignals.Sine(200, 0.4f, 0.3, Rate),
                TestSignals.Noise(0.02f, 0.06, Rate, rng),
                TestSignals.Sine(260, 0.4f, 0.3, Rate),
                TestSignals.Noise(0.02f, 0.06, Rate, rng));

            var trim = SilenceTrimmer.Trim(TestSignals.Mono(samples, Rate), Config);

            Assert.That(trim.Status, Is.EqualTo(TrimStatus.Ok));
            Assert.That(trim.StartFrame, Is.EqualTo(0));
            Assert.That(trim.EndFrame, Is.EqualTo(samples.Length));
        }

        [Test]
        public void EmptyCapture_IsNoVoice()
        {
            var trim = SilenceTrimmer.Trim(AudioBuffer.Silence(0, Rate, 1), Config);

            Assert.That(trim.Status, Is.EqualTo(TrimStatus.NoVoice));
            Assert.That(trim.WindowCount, Is.EqualTo(0));
        }

        [Test]
        public void PaddingIsClippedToCaptureBounds()
        {
            // Voice starts 40 ms in and ends 40 ms before the end: padding would reach past both ends.
            var samples = TestSignals.Concat(
                TestSignals.Silence(0.04, Rate),
                Syllables(4),
                TestSignals.Silence(0.04, Rate));

            var trim = SilenceTrimmer.Trim(TestSignals.Mono(samples, Rate), Config);

            Assert.That(trim.Status, Is.EqualTo(TrimStatus.Ok));
            Assert.That(trim.StartFrame, Is.EqualTo(0));
            Assert.That(trim.EndFrame, Is.EqualTo(samples.Length));
        }

        [Test]
        public void PerfectlySteadySignal_FillingWholeCapture_IsTreatedAsNoise()
        {
            // Documented limit: with no quieter window, the noise-floor estimate equals the signal, so a
            // constant tone or steady fan noise is NoVoice (unscored) rather than scored as 0. Real speech
            // varies by syllable; see SpeechFillingWholeCapture_KeepsEverything.
            var trim = SilenceTrimmer.Trim(TestSignals.Mono(TestSignals.Sine(220, 0.3f, 0.5, Rate), Rate), Config);

            Assert.That(trim.Status, Is.EqualTo(TrimStatus.NoVoice));
        }

        /// <summary>Speech-like level pattern: 120 ms tone bursts separated by 40 ms near-silences.</summary>
        private static float[] Syllables(int count)
        {
            var parts = new float[count * 2 - 1][];
            for (var i = 0; i < count; i++)
            {
                parts[i * 2] = TestSignals.Sine(180 + 30 * i, 0.3f, 0.12, Rate);
                if (i < count - 1) parts[i * 2 + 1] = TestSignals.Silence(0.04, Rate);
            }

            return TestSignals.Concat(parts);
        }
    }
}
