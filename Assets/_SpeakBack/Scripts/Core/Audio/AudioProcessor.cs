using System;

namespace Uprise.SpeakBack.Core.Audio
{
    /// <summary>Outcome of processing one capture.</summary>
    public enum AudioProcessStatus
    {
        /// <summary>Voice found; all three attempt buffers are present.</summary>
        Ok,

        /// <summary>No voice in the capture. Unscored technical outcome, never a zero.</summary>
        NoVoice,

        /// <summary>Voice too short to score. Unscored technical outcome, never a zero.</summary>
        TooShort,
    }

    /// <summary>
    /// Numbers describing one processed capture, for session-only diagnostics. Never contains audio,
    /// transcripts or phonemes, so it is safe to keep for the session (Phase 1 §15).
    /// </summary>
    public sealed class ProcessDiagnostics
    {
        internal ProcessDiagnostics(AudioBuffer capture, LevelStats captureLevel, TrimResult trim, float appliedGain)
        {
            CaptureRate = capture.SampleRate;
            CaptureChannels = capture.Channels;
            CaptureSeconds = capture.DurationSeconds;
            CapturePeak = captureLevel.Peak;
            CaptureRms = captureLevel.Rms;
            ClippingRatio = captureLevel.ClippingRatio;
            NoiseFloorRms = trim.NoiseFloorRms;
            ThresholdRms = trim.ThresholdRms;
            VoicedWindowCount = trim.VoicedWindowCount;
            VoicedSeconds = (double)(trim.VoicedEndFrame - trim.VoicedStartFrame) / capture.SampleRate;
            RetainedSeconds = trim.Status == TrimStatus.NoVoice ? 0.0 : (double)trim.FrameCount / capture.SampleRate;
            AppliedGain = appliedGain;
        }

        /// <summary>Capture sample rate in Hz.</summary>
        public int CaptureRate { get; }

        /// <summary>Capture channel count.</summary>
        public int CaptureChannels { get; }

        /// <summary>Capture length before trimming.</summary>
        public double CaptureSeconds { get; }

        /// <summary>Peak of the raw capture (all channels).</summary>
        public float CapturePeak { get; }

        /// <summary>RMS of the raw capture (all channels).</summary>
        public float CaptureRms { get; }

        /// <summary>Fraction of raw capture samples at or above the clipping threshold.</summary>
        public float ClippingRatio { get; }

        /// <summary>Noise-floor estimate used by the trimmer (mono).</summary>
        public float NoiseFloorRms { get; }

        /// <summary>Voice threshold used by the trimmer (mono).</summary>
        public float ThresholdRms { get; }

        /// <summary>Windows judged voiced.</summary>
        public int VoicedWindowCount { get; }

        /// <summary>Voiced span length, padding excluded.</summary>
        public double VoicedSeconds { get; }

        /// <summary>Retained length including padding; 0 when no voice.</summary>
        public double RetainedSeconds { get; }

        /// <summary>Linear gain applied to the retained attempt (1 when gain is off).</summary>
        public float AppliedGain { get; }
    }

    /// <summary>
    /// Result of <see cref="AudioProcessor.Process"/>. The three buffers are named for which way the
    /// audio faces, and the same names are used everywhere (code, logs, UI, tests):
    /// <list type="bullet">
    /// <item><see cref="RawAttempt"/>: what the player actually said, unreversed (Hear Raw).</item>
    /// <item><see cref="FlippedAttempt"/>: RawAttempt reversed; the reveal, which should sound like the target (reveal, Replay, Compare).</item>
    /// <item><see cref="ScorerInput"/>: FlippedAttempt at the scorer rate; the only thing a scorer receives.</item>
    /// </list>
    /// </summary>
    public sealed class AudioProcessResult
    {
        private AudioProcessResult(
            AudioProcessStatus status, AudioBuffer rawAttempt, AudioBuffer flippedAttempt, AudioBuffer scorerInput,
            ProcessDiagnostics diagnostics)
        {
            Status = status;
            RawAttempt = rawAttempt;
            FlippedAttempt = flippedAttempt;
            ScorerInput = scorerInput;
            Diagnostics = diagnostics;
        }

        /// <summary>Ok, or the reason there is nothing to score.</summary>
        public AudioProcessStatus Status { get; }

        /// <summary>Trimmed mono capture at the capture rate, unreversed. Null unless Ok.</summary>
        public AudioBuffer RawAttempt { get; }

        /// <summary><see cref="RawAttempt"/> reversed, at the capture rate. Null unless Ok.</summary>
        public AudioBuffer FlippedAttempt { get; }

        /// <summary><see cref="FlippedAttempt"/> resampled to the scorer rate, mono. Null unless Ok.</summary>
        public AudioBuffer ScorerInput { get; }

        /// <summary>Numeric diagnostics; always present.</summary>
        public ProcessDiagnostics Diagnostics { get; }

        internal static AudioProcessResult Ok(AudioBuffer raw, AudioBuffer flipped, AudioBuffer scorerInput, ProcessDiagnostics diagnostics) =>
            new AudioProcessResult(AudioProcessStatus.Ok, raw, flipped, scorerInput, diagnostics);

        internal static AudioProcessResult Failed(AudioProcessStatus status, ProcessDiagnostics diagnostics) =>
            new AudioProcessResult(status, null, null, null, diagnostics);
    }

    /// <summary>
    /// Turns one capture into the round's three attempt buffers (Phase 1 §9):
    /// validate → downmix → trim outer silence → optional bounded gain → reverse → resample for the scorer.
    /// Pure and deterministic; safe to run off the main thread.
    /// </summary>
    public static class AudioProcessor
    {
        /// <summary>Processes <paramref name="capture"/> with <paramref name="config"/>.</summary>
        public static AudioProcessResult Process(AudioBuffer capture, ProcessorConfig config)
        {
            if (capture == null) throw new ArgumentNullException(nameof(capture));
            if (config == null) throw new ArgumentNullException(nameof(config));

            var captureLevel = LevelAnalyzer.Analyze(capture);
            var mono = Downmixer.ToMono(capture);
            var trim = SilenceTrimmer.Trim(mono, config);

            if (trim.Status != TrimStatus.Ok)
            {
                var status = trim.Status == TrimStatus.TooShort ? AudioProcessStatus.TooShort : AudioProcessStatus.NoVoice;
                return AudioProcessResult.Failed(status, new ProcessDiagnostics(capture, captureLevel, trim, 1f));
            }

            var raw = mono.SliceFrames(trim.StartFrame, trim.FrameCount);

            // Gain comes after the voice decision, so it can never turn silence into a valid attempt.
            var gain = 1f;
            if (config.GainEnabled)
            {
                gain = BoundedGain(raw, config);
                if (gain > 1f) raw = Scale(raw, gain);
            }

            var flipped = AudioReverser.ReverseFrames(raw);
            var scorerInput = Resampler.Resample(flipped, config.TargetRate);
            return AudioProcessResult.Ok(raw, flipped, scorerInput, new ProcessDiagnostics(capture, captureLevel, trim, gain));
        }

        /// <summary>Gain that lifts the peak towards the target without exceeding it or the configured maximum; never below 1.</summary>
        private static float BoundedGain(AudioBuffer buffer, ProcessorConfig config)
        {
            var peak = LevelAnalyzer.Analyze(buffer).Peak;
            if (peak <= 0f) return 1f;
            return Math.Max(1f, Math.Min(config.GainMaxLinear, config.GainTargetPeak / peak));
        }

        private static AudioBuffer Scale(AudioBuffer buffer, float gain)
        {
            var samples = buffer.ToArray();
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] *= gain;
            }

            return AudioBuffer.FromOwnedArray(samples, buffer.SampleRate, buffer.Channels);
        }
    }
}
