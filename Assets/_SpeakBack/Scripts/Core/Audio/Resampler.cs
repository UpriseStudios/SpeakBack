using System;

namespace Uprise.SpeakBack.Core.Audio
{
    /// <summary>
    /// Anti-aliased sample-rate conversion for mono audio (Phase 1 §9): a Kaiser-windowed sinc
    /// low-pass at <see cref="CutoffFraction"/> of the lower Nyquist frequency, evaluated as a
    /// polyphase filter. Deterministic: the same input always yields bit-identical output.
    /// </summary>
    public static class Resampler
    {
        /// <summary>Filter half-width in zero crossings of the sinc; sets the transition-band width.</summary>
        public const int ZeroCrossings = 32;

        /// <summary>Cut-off as a fraction of min(source, target) / 2.</summary>
        public const double CutoffFraction = 0.9;

        /// <summary>Kaiser window shape (≈ 80 dB stop-band).</summary>
        public const double KaiserBeta = 8.0;

        // Rate pairs needing more phases than this (an unusual reduced ratio) use the nearest of this
        // many phases instead: under 1/2048 of a sample of timing error, far below audibility.
        private const int MaxPhases = 2048;

        /// <summary>
        /// Converts <paramref name="mono"/> to <paramref name="targetRate"/>. The output has
        /// round(frames × target / source) frames, so the duration is preserved. When the rates already
        /// match, the input instance is returned unchanged.
        /// </summary>
        public static AudioBuffer Resample(AudioBuffer mono, int targetRate)
        {
            if (mono == null) throw new ArgumentNullException(nameof(mono));
            if (mono.Channels != 1) throw new ArgumentException("Resampler expects mono audio; downmix first.", nameof(mono));
            Guard.Positive(targetRate, nameof(targetRate));

            var sourceRate = mono.SampleRate;
            if (sourceRate == targetRate) return mono;

            var input = mono.Samples;
            var inFrames = input.Length;
            var outFrames = (int)(((long)inFrames * targetRate + sourceRate / 2) / sourceRate);
            var output = new float[outFrames];
            if (outFrames == 0 || inFrames == 0) return AudioBuffer.FromOwnedArray(output, targetRate, 1);

            // Cut-off in cycles per input sample; the kernel is defined on the input time axis.
            var cutoff = CutoffFraction * Math.Min(sourceRate, targetRate) / 2.0 / sourceRate;
            var halfWidth = ZeroCrossings / (2.0 * cutoff);
            var reach = (int)Math.Ceiling(halfWidth);
            var taps = 2 * reach + 1;

            // Output frame n sits at input position n × down / up.
            var gcd = Gcd(sourceRate, targetRate);
            long up = targetRate / gcd;
            long down = sourceRate / gcd;
            var exactPhases = up <= MaxPhases;
            var phases = exactPhases ? (int)up : MaxPhases;
            var weights = BuildPhaseWeights(phases, taps, reach, cutoff, halfWidth);

            for (var n = 0; n < outFrames; n++)
            {
                var position = n * down;
                var baseIndex = position / up;
                var remainder = position % up;

                int phase;
                if (exactPhases)
                {
                    phase = (int)remainder;
                }
                else
                {
                    phase = (int)Math.Round((double)remainder / up * phases);
                    if (phase == phases)
                    {
                        phase = 0;
                        baseIndex++;
                    }
                }

                var first = (int)baseIndex - reach;
                var weightOffset = phase * taps;

                if (first >= 0 && first + taps <= inFrames)
                {
                    var sum = 0f;
                    for (var j = 0; j < taps; j++)
                    {
                        sum += weights[weightOffset + j] * input[first + j];
                    }

                    output[n] = sum;
                }
                else
                {
                    // Near the ends some taps fall outside the signal; renormalise over the ones inside
                    // so the edges are not faded.
                    var sum = 0.0;
                    var weightSum = 0.0;
                    for (var j = 0; j < taps; j++)
                    {
                        var i = first + j;
                        if (i < 0 || i >= inFrames) continue;
                        sum += weights[weightOffset + j] * input[i];
                        weightSum += weights[weightOffset + j];
                    }

                    output[n] = weightSum > 1e-6 ? (float)(sum / weightSum) : 0f;
                }
            }

            return AudioBuffer.FromOwnedArray(output, targetRate, 1);
        }

        /// <summary>One row of <paramref name="taps"/> weights per fractional phase, each normalised to unit DC gain.</summary>
        private static float[] BuildPhaseWeights(int phases, int taps, int reach, double cutoff, double halfWidth)
        {
            var weights = new float[phases * taps];
            var row = new double[taps];
            var windowNorm = BesselI0(KaiserBeta);

            for (var p = 0; p < phases; p++)
            {
                var fraction = (double)p / phases;
                var sum = 0.0;
                for (var j = 0; j < taps; j++)
                {
                    row[j] = Kernel(j - reach - fraction, cutoff, halfWidth, windowNorm);
                    sum += row[j];
                }

                for (var j = 0; j < taps; j++)
                {
                    weights[p * taps + j] = (float)(row[j] / sum);
                }
            }

            return weights;
        }

        private static double Kernel(double x, double cutoff, double halfWidth, double windowNorm)
        {
            if (Math.Abs(x) >= halfWidth) return 0.0;

            var arg = 2.0 * cutoff * x;
            var sinc = arg == 0.0 ? 1.0 : Math.Sin(Math.PI * arg) / (Math.PI * arg);
            var r = x / halfWidth;
            var window = BesselI0(KaiserBeta * Math.Sqrt(1.0 - r * r)) / windowNorm;
            return 2.0 * cutoff * sinc * window;
        }

        /// <summary>Zeroth-order modified Bessel function of the first kind (power series).</summary>
        private static double BesselI0(double x)
        {
            var sum = 1.0;
            var term = 1.0;
            var halfX = x / 2.0;
            for (var k = 1; k < 64; k++)
            {
                term *= halfX / k;
                var squared = term * term;
                sum += squared;
                if (squared < sum * 1e-17) break;
            }

            return sum;
        }

        private static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                var t = a % b;
                a = b;
                b = t;
            }

            return a;
        }
    }
}
