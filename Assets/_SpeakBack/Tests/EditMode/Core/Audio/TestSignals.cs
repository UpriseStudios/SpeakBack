using System;
using System.Collections.Generic;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Core.Tests.Audio
{
    /// <summary>Synthetic fixtures. Uses its own xorshift RNG so Unity (Mono) and .NET produce identical signals.</summary>
    internal static class TestSignals
    {
        public static float[] Sine(double frequency, float amplitude, double seconds, int rate)
        {
            var frames = (int)Math.Round(seconds * rate);
            var samples = new float[frames];
            for (var i = 0; i < frames; i++)
            {
                samples[i] = amplitude * (float)Math.Sin(2.0 * Math.PI * frequency * i / rate);
            }

            return samples;
        }

        public static float[] Silence(double seconds, int rate) => new float[(int)Math.Round(seconds * rate)];

        /// <summary>Uniform white noise with the given RMS (uniform on [-a, a] has RMS a/√3).</summary>
        public static float[] Noise(float rms, double seconds, int rate, Rng rng)
        {
            var frames = (int)Math.Round(seconds * rate);
            var amplitude = rms * (float)Math.Sqrt(3.0);
            var samples = new float[frames];
            for (var i = 0; i < frames; i++)
            {
                samples[i] = amplitude * rng.NextSigned();
            }

            return samples;
        }

        public static float[] Concat(params float[][] parts)
        {
            var total = 0;
            foreach (var part in parts) total += part.Length;
            var result = new float[total];
            var offset = 0;
            foreach (var part in parts)
            {
                Array.Copy(part, 0, result, offset, part.Length);
                offset += part.Length;
            }

            return result;
        }

        public static float[] Add(float[] a, float[] b)
        {
            var result = new float[Math.Max(a.Length, b.Length)];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = (i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] : 0f);
            }

            return result;
        }

        /// <summary>Interleaves equal-length channel arrays into frames.</summary>
        public static float[] Interleave(params float[][] channels)
        {
            var frames = channels[0].Length;
            var result = new float[frames * channels.Length];
            for (var f = 0; f < frames; f++)
            {
                for (var c = 0; c < channels.Length; c++)
                {
                    result[f * channels.Length + c] = channels[c][f];
                }
            }

            return result;
        }

        public static AudioBuffer Mono(float[] samples, int rate) => AudioBuffer.Create(samples, rate, 1);

        /// <summary>RMS over [start, end) of the samples, skipping filter edge effects at the ends.</summary>
        public static double Rms(ReadOnlySpan<float> samples, int start, int end)
        {
            var sum = 0.0;
            for (var i = start; i < end; i++) sum += (double)samples[i] * samples[i];
            return Math.Sqrt(sum / (end - start));
        }

        /// <summary>Frequency estimated from rising zero crossings over [start, end).</summary>
        public static double ZeroCrossingFrequency(ReadOnlySpan<float> samples, int start, int end, int rate)
        {
            var crossings = new List<int>();
            for (var i = start + 1; i < end; i++)
            {
                if (samples[i - 1] < 0f && samples[i] >= 0f) crossings.Add(i);
            }

            if (crossings.Count < 2) return 0.0;
            var periods = crossings.Count - 1;
            return (double)periods * rate / (crossings[crossings.Count - 1] - crossings[0]);
        }

        /// <summary>Deterministic xorshift32 generator.</summary>
        internal sealed class Rng
        {
            private uint _state;

            public Rng(uint seed)
            {
                _state = seed == 0 ? 0x9E3779B9u : seed;
            }

            public uint NextUInt()
            {
                var x = _state;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                _state = x;
                return x;
            }

            /// <summary>Uniform in [0, maxExclusive).</summary>
            public int Next(int maxExclusive) => (int)(NextUInt() % (uint)maxExclusive);

            /// <summary>Uniform in [-1, 1).</summary>
            public float NextSigned() => (float)(NextUInt() / 4294967296.0 * 2.0 - 1.0);
        }
    }
}
