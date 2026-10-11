using UnityEngine;

namespace Uprise.SpeakBack.Audio
{
    /// <summary>
    /// Thin seam over Unity's Microphone API so <see cref="MicrophoneCapture"/> can be tested with a fake.
    /// Production uses <see cref="UnityMicrophoneBackend"/>, the only code allowed to touch Microphone.*.
    /// </summary>
    public interface IMicrophoneBackend
    {
        /// <summary>Available input devices; empty when none.</summary>
        string[] Devices { get; }

        /// <summary>Supported rate range; (0, 0) means any rate.</summary>
        void GetDeviceCaps(string device, out int minFrequency, out int maxFrequency);

        /// <summary>Starts recording into a new clip; null on failure.</summary>
        AudioClip Start(string device, bool loop, int lengthSeconds, int frequency);

        /// <summary>Current write position in sample frames (not interleaved samples).</summary>
        int GetPosition(string device);

        /// <summary>Whether the device is still recording.</summary>
        bool IsRecording(string device);

        /// <summary>Stops recording.</summary>
        void End(string device);
    }
}
