using System;

namespace Uprise.SpeakBack.Core.Audio
{
    /// <summary>
    /// Audio-processing settings. Defaults are the Phase 1 §7 provisional values: test values, not
    /// established thresholds. Immutable; <see cref="Default"/> is shared safely.
    /// </summary>
    public sealed class ProcessorConfig
    {
        /// <summary>Scorer input rate (§7: 16,000 Hz mono).</summary>
        public const int DefaultTargetRate = 16000;

        /// <summary>RMS analysis window (§7: 20 ms).</summary>
        public const double DefaultWindowSeconds = 0.020;

        /// <summary>Base RMS voice threshold on normalised float input (§7: 0.008).</summary>
        public const float DefaultBaseRmsThreshold = 0.008f;

        /// <summary>Minimum voiced windows across the capture before it counts as voice (§7: 3).</summary>
        public const int DefaultMinVoicedWindows = 3;

        /// <summary>Minimum voiced span, padding excluded (§7: 0.25 s; P00 Q5).</summary>
        public const double DefaultMinRetainedSeconds = 0.25;

        /// <summary>Padding kept around the voiced span, clipped to the capture (§7: 100 ms).</summary>
        public const double DefaultPaddingSeconds = 0.100;

        /// <summary>
        /// Percentile of window RMS values taken as the noise floor. Low (10th) so the estimate still
        /// lands on silence when the player fills most of the capture with speech.
        /// </summary>
        public const double DefaultNoiseFloorPercentile = 0.10;

        /// <summary>Voice threshold multiple of the noise floor (×3 ≈ +9.5 dB); never below the base threshold.</summary>
        public const float DefaultNoiseFloorFactor = 3f;

        /// <summary>Optional gain is off until quiet/loud testing justifies it (§7 "Level normalisation").</summary>
        public const bool DefaultGainEnabled = false;

        /// <summary>Peak the optional gain aims for (−1 dBFS).</summary>
        public const float DefaultGainTargetPeak = 0.891f;

        /// <summary>Largest linear gain the optional gain may apply (+12 dB).</summary>
        public const float DefaultGainMaxLinear = 3.981f;

        /// <summary>The §7 defaults.</summary>
        public static readonly ProcessorConfig Default = new ProcessorConfig();

        /// <summary>Creates a configuration; every argument defaults to its §7 value.</summary>
        public ProcessorConfig(
            int targetRate = DefaultTargetRate,
            double windowSeconds = DefaultWindowSeconds,
            float baseRmsThreshold = DefaultBaseRmsThreshold,
            int minVoicedWindows = DefaultMinVoicedWindows,
            double minRetainedSeconds = DefaultMinRetainedSeconds,
            double paddingSeconds = DefaultPaddingSeconds,
            double noiseFloorPercentile = DefaultNoiseFloorPercentile,
            float noiseFloorFactor = DefaultNoiseFloorFactor,
            bool gainEnabled = DefaultGainEnabled,
            float gainTargetPeak = DefaultGainTargetPeak,
            float gainMaxLinear = DefaultGainMaxLinear)
        {
            TargetRate = Guard.Positive(targetRate, nameof(targetRate));
            WindowSeconds = RequireRange(windowSeconds, 0.001, 1.0, nameof(windowSeconds));
            BaseRmsThreshold = (float)RequireRange(baseRmsThreshold, 0.0, 1.0, nameof(baseRmsThreshold));
            MinVoicedWindows = Guard.Positive(minVoicedWindows, nameof(minVoicedWindows));
            MinRetainedSeconds = RequireRange(minRetainedSeconds, 0.0, 60.0, nameof(minRetainedSeconds));
            PaddingSeconds = RequireRange(paddingSeconds, 0.0, 5.0, nameof(paddingSeconds));
            NoiseFloorPercentile = RequireRange(noiseFloorPercentile, 0.0, 1.0, nameof(noiseFloorPercentile));
            NoiseFloorFactor = (float)RequireRange(noiseFloorFactor, 1.0, 100.0, nameof(noiseFloorFactor));
            GainEnabled = gainEnabled;
            GainTargetPeak = (float)RequireRange(gainTargetPeak, 0.01, 1.0, nameof(gainTargetPeak));
            GainMaxLinear = (float)RequireRange(gainMaxLinear, 1.0, 100.0, nameof(gainMaxLinear));
        }

        /// <summary>Scorer input rate in Hz.</summary>
        public int TargetRate { get; }

        /// <summary>RMS analysis window length in seconds.</summary>
        public double WindowSeconds { get; }

        /// <summary>Lowest RMS a window may need to count as voiced.</summary>
        public float BaseRmsThreshold { get; }

        /// <summary>Voiced windows required for a capture to contain voice.</summary>
        public int MinVoicedWindows { get; }

        /// <summary>Shortest voiced span (padding excluded) that is scored rather than reported TooShort.</summary>
        public double MinRetainedSeconds { get; }

        /// <summary>Padding added before and after the voiced span.</summary>
        public double PaddingSeconds { get; }

        /// <summary>Percentile (0–1) of window RMS used as the noise-floor estimate.</summary>
        public double NoiseFloorPercentile { get; }

        /// <summary>Multiple of the noise floor a window must exceed to be voiced.</summary>
        public float NoiseFloorFactor { get; }

        /// <summary>Whether the optional bounded, peak-protected gain is applied.</summary>
        public bool GainEnabled { get; }

        /// <summary>Peak the optional gain raises the attempt towards.</summary>
        public float GainTargetPeak { get; }

        /// <summary>Upper bound on the optional gain (linear).</summary>
        public float GainMaxLinear { get; }

        private static double RequireRange(double value, double min, double max, string name)
        {
            if (double.IsNaN(value) || value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(name, value, $"Must be between {min} and {max}.");
            }

            return value;
        }
    }
}
