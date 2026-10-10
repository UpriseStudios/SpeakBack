using System;

namespace Uprise.SpeakBack.Core.Audio
{
    /// <summary>Peak, RMS and clipping ratio of a buffer, for session-only diagnostics (Phase 1 §7).</summary>
    public readonly struct LevelStats
    {
        /// <summary>Creates a set of level statistics.</summary>
        public LevelStats(float peak, float rms, float clippingRatio)
        {
            Peak = peak;
            Rms = rms;
            ClippingRatio = clippingRatio;
        }

        /// <summary>Largest absolute sample value.</summary>
        public float Peak { get; }

        /// <summary>Root mean square over all samples.</summary>
        public float Rms { get; }

        /// <summary>Fraction of samples at or above <see cref="LevelAnalyzer.ClippingThreshold"/> in magnitude.</summary>
        public float ClippingRatio { get; }
    }

    /// <summary>Measures signal levels. Pure maths; never inspects or keeps the audio beyond the call.</summary>
    public static class LevelAnalyzer
    {
        /// <summary>Magnitude treated as clipped on normalised float input.</summary>
        public const float ClippingThreshold = 0.99f;

        /// <summary>Level statistics over every sample of <paramref name="buffer"/>; all zero for an empty buffer.</summary>
        public static LevelStats Analyze(AudioBuffer buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            var samples = buffer.Samples;
            if (samples.Length == 0) return new LevelStats(0f, 0f, 0f);

            var peak = 0f;
            var sumSquares = 0.0;
            var clipped = 0;
            for (var i = 0; i < samples.Length; i++)
            {
                var magnitude = Math.Abs(samples[i]);
                if (magnitude > peak) peak = magnitude;
                if (magnitude >= ClippingThreshold) clipped++;
                sumSquares += (double)samples[i] * samples[i];
            }

            return new LevelStats(peak, (float)Math.Sqrt(sumSquares / samples.Length), (float)clipped / samples.Length);
        }

        /// <summary>Root mean square of <paramref name="samples"/>; zero when empty.</summary>
        public static float Rms(ReadOnlySpan<float> samples)
        {
            if (samples.Length == 0) return 0f;

            var sumSquares = 0.0;
            for (var i = 0; i < samples.Length; i++)
            {
                sumSquares += (double)samples[i] * samples[i];
            }

            return (float)Math.Sqrt(sumSquares / samples.Length);
        }
    }
}
