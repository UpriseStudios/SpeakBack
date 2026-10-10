using System;

namespace Uprise.SpeakBack.Core.Audio
{
    /// <summary>Outcome of outer-silence trimming.</summary>
    public enum TrimStatus
    {
        /// <summary>Voice found; the trim bounds are valid.</summary>
        Ok,

        /// <summary>Fewer voiced windows than the minimum: silence or a lone click.</summary>
        NoVoice,

        /// <summary>Voice found, but the voiced span is shorter than the minimum retained duration.</summary>
        TooShort,
    }

    /// <summary>
    /// Result of <see cref="SilenceTrimmer.Trim"/>: status, retained frame range and the numbers behind
    /// the decision. Contains no audio.
    /// </summary>
    public sealed class TrimResult
    {
        internal TrimResult(
            TrimStatus status, int startFrame, int endFrame, int voicedStartFrame, int voicedEndFrame,
            int voicedWindowCount, int windowCount, float peak, float rms, float noiseFloorRms, float thresholdRms)
        {
            Status = status;
            StartFrame = startFrame;
            EndFrame = endFrame;
            VoicedStartFrame = voicedStartFrame;
            VoicedEndFrame = voicedEndFrame;
            VoicedWindowCount = voicedWindowCount;
            WindowCount = windowCount;
            Peak = peak;
            Rms = rms;
            NoiseFloorRms = noiseFloorRms;
            ThresholdRms = thresholdRms;
        }

        /// <summary>Whether voice was found and long enough.</summary>
        public TrimStatus Status { get; }

        /// <summary>First retained frame (voiced span minus padding, clipped to 0). Meaningful when <see cref="Status"/> is Ok.</summary>
        public int StartFrame { get; }

        /// <summary>One past the last retained frame (voiced span plus padding, clipped to the capture).</summary>
        public int EndFrame { get; }

        /// <summary>Retained frame count.</summary>
        public int FrameCount => EndFrame - StartFrame;

        /// <summary>Start of the first voiced window; 0 when none.</summary>
        public int VoicedStartFrame { get; }

        /// <summary>End of the last voiced window; 0 when none.</summary>
        public int VoicedEndFrame { get; }

        /// <summary>Windows above the threshold anywhere in the capture.</summary>
        public int VoicedWindowCount { get; }

        /// <summary>Windows analysed.</summary>
        public int WindowCount { get; }

        /// <summary>Largest absolute sample in the capture.</summary>
        public float Peak { get; }

        /// <summary>RMS of the whole capture.</summary>
        public float Rms { get; }

        /// <summary>Noise-floor estimate: the configured percentile of window RMS values.</summary>
        public float NoiseFloorRms { get; }

        /// <summary>RMS a window had to exceed to count as voiced.</summary>
        public float ThresholdRms { get; }
    }

    /// <summary>
    /// Finds the outer bounds of voice (Phase 1 §9). A window is voiced when its RMS exceeds
    /// max(base threshold, noise floor × factor). The retained range runs from the first to the last
    /// voiced window plus padding, so everything in between, including pauses, is kept. Quiet
    /// consonants next to the voiced span are protected by the padding rather than by a
    /// peak-relative threshold.
    /// </summary>
    public static class SilenceTrimmer
    {
        /// <summary>Analyses <paramref name="buffer"/> (any channel count) and returns the bounds to keep.</summary>
        public static TrimResult Trim(AudioBuffer buffer, ProcessorConfig config)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (config == null) throw new ArgumentNullException(nameof(config));

            var level = LevelAnalyzer.Analyze(buffer);
            var frames = buffer.FrameCount;
            var channels = buffer.Channels;
            var windowFrames = Math.Max(1, (int)Math.Round(config.WindowSeconds * buffer.SampleRate));
            var windowCount = (frames + windowFrames - 1) / windowFrames;

            if (windowCount == 0)
            {
                return new TrimResult(TrimStatus.NoVoice, 0, 0, 0, 0, 0, 0, 0f, 0f, 0f, config.BaseRmsThreshold);
            }

            var samples = buffer.Samples;
            var windowRms = new float[windowCount];
            for (var w = 0; w < windowCount; w++)
            {
                var startFrame = w * windowFrames;
                var length = Math.Min(windowFrames, frames - startFrame);
                windowRms[w] = LevelAnalyzer.Rms(samples.Slice(startFrame * channels, length * channels));
            }

            var noiseFloor = Percentile(windowRms, config.NoiseFloorPercentile);
            var threshold = Math.Max(config.BaseRmsThreshold, noiseFloor * config.NoiseFloorFactor);

            var voiced = 0;
            var firstVoiced = -1;
            var lastVoiced = -1;
            for (var w = 0; w < windowCount; w++)
            {
                if (windowRms[w] <= threshold) continue;
                voiced++;
                if (firstVoiced < 0) firstVoiced = w;
                lastVoiced = w;
            }

            if (voiced < config.MinVoicedWindows)
            {
                return new TrimResult(TrimStatus.NoVoice, 0, 0, 0, 0, voiced, windowCount,
                    level.Peak, level.Rms, noiseFloor, threshold);
            }

            var voicedStart = firstVoiced * windowFrames;
            var voicedEnd = Math.Min(frames, (lastVoiced + 1) * windowFrames);
            var voicedSeconds = (double)(voicedEnd - voicedStart) / buffer.SampleRate;
            var padding = (int)Math.Round(config.PaddingSeconds * buffer.SampleRate);
            var start = Math.Max(0, voicedStart - padding);
            var end = Math.Min(frames, voicedEnd + padding);

            var status = voicedSeconds < config.MinRetainedSeconds ? TrimStatus.TooShort : TrimStatus.Ok;
            return new TrimResult(status, start, end, voicedStart, voicedEnd, voiced, windowCount,
                level.Peak, level.Rms, noiseFloor, threshold);
        }

        private static float Percentile(float[] values, double fraction)
        {
            var sorted = (float[])values.Clone();
            Array.Sort(sorted);
            var index = (int)Math.Floor(fraction * (sorted.Length - 1));
            return sorted[index];
        }
    }
}
