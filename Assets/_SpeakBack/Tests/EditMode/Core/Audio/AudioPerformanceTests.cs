using System;
using System.Diagnostics;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Core.Tests.Audio
{
    /// <summary>
    /// Desktop benchmark: excluded from normal runs. The 50 ms target applies to optimised code, so run it
    /// as <c>dotnet test Tools/DotnetTests -c Release --filter FullyQualifiedName~AudioPerformanceTests</c>.
    /// Debug builds and the Unity editor (Debug code optimisation) run several times slower and are not
    /// meaningful here. Device timings under IL2CPP are measured separately (P12).
    /// </summary>
    [Explicit("Benchmark; run on demand")]
    [Category("Perf")]
    public sealed class AudioPerformanceTests
    {
        [Test]
        public void Process_EightSecondStereo48k_UnderFiftyMilliseconds()
        {
            const int rate = 48000;
            var rng = new TestSignals.Rng(1);
            var left = SpeechLike(rate, 180, rng);
            var right = SpeechLike(rate, 240, rng);
            var capture = AudioBuffer.Create(TestSignals.Interleave(left, right), rate, 2);
            Assert.That(capture.DurationSeconds, Is.EqualTo(8.0).Within(1e-6));

            for (var i = 0; i < 3; i++) AudioProcessor.Process(capture, ProcessorConfig.Default);

            var timings = new double[9];
            for (var i = 0; i < timings.Length; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var result = AudioProcessor.Process(capture, ProcessorConfig.Default);
                stopwatch.Stop();
                Assert.That(result.Status, Is.EqualTo(AudioProcessStatus.Ok));
                timings[i] = stopwatch.Elapsed.TotalMilliseconds;
            }

            Array.Sort(timings);
            var median = timings[timings.Length / 2];
            TestContext.WriteLine($"AudioProcessor 8 s 48 kHz stereo → 16 kHz: median {median:F1} ms, " +
                                  $"min {timings[0]:F1} ms, max {timings[timings.Length - 1]:F1} ms");
            Assert.That(median, Is.LessThan(50.0));
        }

        /// <summary>8 s: 0.5 s silence, 35 × (150 ms voiced burst + 50 ms gap), 0.5 s silence.</summary>
        private static float[] SpeechLike(int rate, double frequency, TestSignals.Rng rng)
        {
            var parts = new System.Collections.Generic.List<float[]> { TestSignals.Silence(0.5, rate) };
            for (var i = 0; i < 35; i++)
            {
                parts.Add(TestSignals.Add(TestSignals.Sine(frequency, 0.3f, 0.15, rate), TestSignals.Noise(0.05f, 0.15, rate, rng)));
                parts.Add(TestSignals.Silence(0.05, rate));
            }

            parts.Add(TestSignals.Silence(0.5, rate));
            return TestSignals.Concat(parts.ToArray());
        }
    }
}
