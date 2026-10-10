using System;

namespace Uprise.SpeakBack.Core.Audio
{
    /// <summary>Converts multi-channel audio to mono by averaging each frame's channels (Phase 1 §9).</summary>
    public static class Downmixer
    {
        /// <summary>
        /// Mono version of <paramref name="input"/>. A buffer that is already mono is returned as the
        /// same instance (buffers are immutable, so sharing it is safe).
        /// </summary>
        public static AudioBuffer ToMono(AudioBuffer input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Channels == 1) return input;

            var source = input.Samples;
            var channels = input.Channels;
            var frames = input.FrameCount;
            var output = new float[frames];

            for (var frame = 0; frame < frames; frame++)
            {
                var sum = 0f;
                var offset = frame * channels;
                for (var channel = 0; channel < channels; channel++)
                {
                    sum += source[offset + channel];
                }

                output[frame] = sum / channels;
            }

            return AudioBuffer.FromOwnedArray(output, input.SampleRate, 1);
        }
    }
}
