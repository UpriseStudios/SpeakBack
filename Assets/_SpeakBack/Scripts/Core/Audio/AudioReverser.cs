using System;

namespace Uprise.SpeakBack.Core.Audio
{
    /// <summary>
    /// Frame-correct time reversal (Phase 1 §9). Frames are reversed while each frame's channel order
    /// is kept, so stereo [L0,R0,L1,R1] becomes [L1,R1,L0,R0], never a channel swap.
    /// </summary>
    public static class AudioReverser
    {
        /// <summary>Returns a new buffer with the frames of <paramref name="input"/> in reverse order.</summary>
        public static AudioBuffer ReverseFrames(AudioBuffer input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var source = input.Samples;
            var channels = input.Channels;
            var frames = input.FrameCount;
            var output = new float[source.Length];

            for (var frame = 0; frame < frames; frame++)
            {
                var from = (frames - 1 - frame) * channels;
                var to = frame * channels;
                for (var channel = 0; channel < channels; channel++)
                {
                    output[to + channel] = source[from + channel];
                }
            }

            return AudioBuffer.FromOwnedArray(output, input.SampleRate, channels);
        }
    }
}
