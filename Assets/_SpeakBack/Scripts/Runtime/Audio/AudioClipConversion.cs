using UnityEngine;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Audio
{
    /// <summary>Converts between Core <see cref="AudioBuffer"/> and Unity <see cref="AudioClip"/>. Main thread only.</summary>
    public static class AudioClipConversion
    {
        /// <summary>A new clip holding <paramref name="buffer"/>; null for an empty buffer. The caller owns and must destroy it.</summary>
        public static AudioClip ToClip(AudioBuffer buffer, string name)
        {
            if (buffer == null || buffer.FrameCount == 0) return null;
            var clip = AudioClip.Create(name, buffer.FrameCount, buffer.Channels, buffer.SampleRate, false);
            clip.SetData(buffer.ToArray(), 0);
            return clip;
        }

        /// <summary>Destroys a clip created at runtime, in play mode or edit mode.</summary>
        public static void Destroy(AudioClip clip)
        {
            if (clip == null) return;
            if (Application.isPlaying) Object.Destroy(clip);
            else Object.DestroyImmediate(clip);
        }
    }
}
