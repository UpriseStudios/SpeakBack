using UnityEngine;
using Uprise.SpeakBack.Audio;

namespace Uprise.SpeakBack.Tests
{
    /// <summary>
    /// Scriptable stand-in for Unity's Microphone. Tests set <see cref="Position"/> and <see cref="Recording"/>
    /// to simulate device behaviour, including position resetting to 0 after End or when the clip fills.
    /// </summary>
    internal sealed class FakeMicrophoneBackend : IMicrophoneBackend
    {
        public string[] Devices { get; set; } = { "Fake microphone" };
        public int MinRate { get; set; }
        public int MaxRate { get; set; }
        public int Channels { get; set; } = 1;
        public bool StartReturnsNull { get; set; }
        public int Position { get; set; }
        public bool Recording { get; set; }
        public AudioClip Clip { get; private set; }
        public float[] ClipData { get; private set; }
        public int StartCalls { get; private set; }
        public int EndCalls { get; private set; }
        public int RequestedRate { get; private set; }
        public bool RequestedLoop { get; private set; }
        public int RequestedLengthSeconds { get; private set; }

        public void GetDeviceCaps(string device, out int minFrequency, out int maxFrequency)
        {
            minFrequency = MinRate;
            maxFrequency = MaxRate;
        }

        public AudioClip Start(string device, bool loop, int lengthSeconds, int frequency)
        {
            StartCalls++;
            RequestedRate = frequency;
            RequestedLoop = loop;
            RequestedLengthSeconds = lengthSeconds;
            if (StartReturnsNull) return null;

            var frames = lengthSeconds * frequency;
            ClipData = new float[frames * Channels];
            for (var i = 0; i < ClipData.Length; i++) ClipData[i] = (i % 1000) / 1000f - 0.5f;
            Clip = AudioClip.Create("FakeMicClip", frames, Channels, frequency, false);
            Clip.SetData(ClipData, 0);
            Recording = true;
            return Clip;
        }

        public int GetPosition(string device) => Position;

        public bool IsRecording(string device) => Recording;

        public void End(string device)
        {
            EndCalls++;
            Recording = false;
            Position = 0; // Unity reports 0 once the microphone has ended.
        }
    }
}
