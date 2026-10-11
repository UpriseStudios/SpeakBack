using UnityEngine;

namespace Uprise.SpeakBack.Audio
{
    /// <summary>
    /// The single place in the codebase that calls Unity's <see cref="Microphone"/> API (CLAUDE.md: one
    /// microphone owner). It is created and used only by <see cref="MicrophoneRecorder"/>; an EditMode test
    /// fails if any other script mentions Microphone.
    /// </summary>
    internal sealed class UnityMicrophoneBackend : IMicrophoneBackend
    {
        public string[] Devices => Microphone.devices;

        public void GetDeviceCaps(string device, out int minFrequency, out int maxFrequency) =>
            Microphone.GetDeviceCaps(device, out minFrequency, out maxFrequency);

        public AudioClip Start(string device, bool loop, int lengthSeconds, int frequency) =>
            Microphone.Start(device, loop, lengthSeconds, frequency);

        public int GetPosition(string device) => Microphone.GetPosition(device);

        public bool IsRecording(string device) => Microphone.IsRecording(device);

        public void End(string device) => Microphone.End(device);
    }
}
