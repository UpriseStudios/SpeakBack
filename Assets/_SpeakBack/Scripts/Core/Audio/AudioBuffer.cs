using System;

namespace Uprise.SpeakBack.Core.Audio
{
    /// <summary>
    /// Immutable block of interleaved PCM float samples (Phase 1 §5). One frame holds one sample per
    /// channel. Instances are safe to share across threads because nothing can change them: the
    /// factory copies its input and <see cref="Samples"/> is read-only. Processing always produces new buffers.
    /// </summary>
    public sealed class AudioBuffer
    {
        private readonly float[] _samples;

        private AudioBuffer(float[] samples, int sampleRate, int channels)
        {
            _samples = samples;
            SampleRate = sampleRate;
            Channels = channels;
        }

        /// <summary>Interleaved samples. Read-only view; use <see cref="ToArray"/> when an owned copy is needed (e.g. AudioClip.SetData).</summary>
        public ReadOnlySpan<float> Samples => _samples;

        /// <summary>Frames per second.</summary>
        public int SampleRate { get; }

        /// <summary>Samples per frame.</summary>
        public int Channels { get; }

        /// <summary>Total interleaved samples (FrameCount × Channels).</summary>
        public int SampleCount => _samples.Length;

        /// <summary>Number of frames (time steps).</summary>
        public int FrameCount => _samples.Length / Channels;

        /// <summary>Length in seconds.</summary>
        public double DurationSeconds => (double)FrameCount / SampleRate;

        /// <summary>
        /// Creates a buffer from a copy of <paramref name="interleaved"/>. Throws if the rate or channel
        /// count is not positive, the length is not a whole number of frames, or any sample is NaN/infinite.
        /// </summary>
        public static AudioBuffer Create(ReadOnlySpan<float> interleaved, int sampleRate, int channels)
        {
            Validate(interleaved, sampleRate, channels);
            return new AudioBuffer(interleaved.ToArray(), sampleRate, channels);
        }

        /// <inheritdoc cref="Create(ReadOnlySpan{float}, int, int)"/>
        public static AudioBuffer Create(float[] interleaved, int sampleRate, int channels)
        {
            if (interleaved == null) throw new ArgumentNullException(nameof(interleaved));
            return Create((ReadOnlySpan<float>)interleaved, sampleRate, channels);
        }

        /// <summary>A buffer of <paramref name="frameCount"/> silent frames.</summary>
        public static AudioBuffer Silence(int frameCount, int sampleRate, int channels)
        {
            if (frameCount < 0) throw new ArgumentOutOfRangeException(nameof(frameCount), frameCount, "Must not be negative.");
            Guard.Positive(sampleRate, nameof(sampleRate));
            Guard.Positive(channels, nameof(channels));
            return new AudioBuffer(new float[checked(frameCount * channels)], sampleRate, channels);
        }

        /// <summary>
        /// Wraps an array this assembly has just produced, without copying. The caller hands over
        /// ownership and must not keep or modify the array afterwards.
        /// </summary>
        internal static AudioBuffer FromOwnedArray(float[] samples, int sampleRate, int channels)
        {
            Validate(samples, sampleRate, channels);
            return new AudioBuffer(samples, sampleRate, channels);
        }

        /// <summary>A new array holding a copy of the interleaved samples.</summary>
        public float[] ToArray() => (float[])_samples.Clone();

        /// <summary>A new buffer holding a copy of the frames [<paramref name="startFrame"/>, startFrame + <paramref name="frameCount"/>).</summary>
        public AudioBuffer SliceFrames(int startFrame, int frameCount)
        {
            if (startFrame < 0 || frameCount < 0 || startFrame > FrameCount - frameCount)
            {
                throw new ArgumentOutOfRangeException(nameof(startFrame),
                    $"Frames [{startFrame}, {startFrame + frameCount}) are outside 0..{FrameCount}.");
            }

            var slice = new float[frameCount * Channels];
            Array.Copy(_samples, startFrame * Channels, slice, 0, slice.Length);
            return new AudioBuffer(slice, SampleRate, Channels);
        }

        private static void Validate(ReadOnlySpan<float> samples, int sampleRate, int channels)
        {
            Guard.Positive(sampleRate, nameof(sampleRate));
            Guard.Positive(channels, nameof(channels));

            if (samples.Length % channels != 0)
            {
                throw new ArgumentException(
                    $"Sample count {samples.Length} is not a whole number of {channels}-channel frames.", nameof(samples));
            }

            for (var i = 0; i < samples.Length; i++)
            {
                var s = samples[i];
                if (float.IsNaN(s) || float.IsInfinity(s))
                {
                    throw new ArgumentException($"Sample {i} is not a finite number.", nameof(samples));
                }
            }
        }
    }
}
