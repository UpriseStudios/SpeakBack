using NUnit.Framework;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Core.Tests.Audio
{
    /// <summary>Invariants checked over many seeded random buffers.</summary>
    public sealed class AudioPropertyTests
    {
        private const int Cases = 200;
        private static readonly int[] Rates = { 8000, 16000, 22050, 44100, 48000 };

        [Test]
        public void DoubleReversal_IsIdentity_ForRandomBuffers()
        {
            var rng = new TestSignals.Rng(20261010);
            for (var c = 0; c < Cases; c++)
            {
                var buffer = RandomBuffer(rng, out _);

                var twice = AudioReverser.ReverseFrames(AudioReverser.ReverseFrames(buffer));

                Assert.That(twice.ToArray(), Is.EqualTo(buffer.ToArray()), $"case {c}");
                Assert.That(twice.Channels, Is.EqualTo(buffer.Channels), $"case {c}");
            }
        }

        [Test]
        public void Trimming_NeverExtendsLength_ForRandomBuffers()
        {
            var rng = new TestSignals.Rng(777);
            for (var c = 0; c < Cases; c++)
            {
                var buffer = RandomBuffer(rng, out var hasVoice);

                var trim = SilenceTrimmer.Trim(buffer, ProcessorConfig.Default);

                if (trim.Status == TrimStatus.NoVoice) continue;
                Assert.That(trim.StartFrame, Is.GreaterThanOrEqualTo(0), $"case {c}");
                Assert.That(trim.EndFrame, Is.LessThanOrEqualTo(buffer.FrameCount), $"case {c}");
                Assert.That(trim.FrameCount, Is.LessThanOrEqualTo(buffer.FrameCount), $"case {c}");
                Assert.That(trim.StartFrame, Is.LessThanOrEqualTo(trim.VoicedStartFrame), $"case {c}");
                Assert.That(trim.EndFrame, Is.GreaterThanOrEqualTo(trim.VoicedEndFrame), $"case {c}");
            }
        }

        [Test]
        public void Processing_RandomCaptures_KeepsDirectionalInvariants()
        {
            var rng = new TestSignals.Rng(4242);
            for (var c = 0; c < Cases; c++)
            {
                var buffer = RandomBuffer(rng, out _);

                var result = AudioProcessor.Process(buffer, ProcessorConfig.Default);

                if (result.Status != AudioProcessStatus.Ok)
                {
                    Assert.That(result.ScorerInput, Is.Null, $"case {c}");
                    continue;
                }

                Assert.That(result.RawAttempt.FrameCount, Is.LessThanOrEqualTo(buffer.FrameCount), $"case {c}");
                Assert.That(AudioReverser.ReverseFrames(result.FlippedAttempt).ToArray(),
                    Is.EqualTo(result.RawAttempt.ToArray()), $"case {c}");
                Assert.That(result.ScorerInput.SampleRate, Is.EqualTo(ProcessorConfig.DefaultTargetRate), $"case {c}");
            }
        }

        /// <summary>Random channels, rate and length; about half contain a voiced burst between silences.</summary>
        private static AudioBuffer RandomBuffer(TestSignals.Rng rng, out bool hasVoice)
        {
            var rate = Rates[rng.Next(Rates.Length)];
            var channels = 1 + rng.Next(3);
            var frames = rng.Next(rate / 2);
            hasVoice = rng.Next(2) == 0;

            var voiceStart = frames == 0 ? 0 : rng.Next(frames);
            var voiceLength = frames - voiceStart == 0 ? 0 : rng.Next(frames - voiceStart);
            var amplitude = 0.05f + 0.5f * (rng.NextUInt() / 4294967296f);

            var samples = new float[frames * channels];
            for (var f = 0; f < frames; f++)
            {
                var inVoice = hasVoice && f >= voiceStart && f < voiceStart + voiceLength;
                for (var ch = 0; ch < channels; ch++)
                {
                    var noise = 0.002f * rng.NextSigned();
                    samples[f * channels + ch] = inVoice ? amplitude * rng.NextSigned() + noise : noise;
                }
            }

            return AudioBuffer.Create(samples, rate, channels);
        }
    }
}
