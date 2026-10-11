using System;
using UnityEngine;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Audio
{
    /// <summary>Capture settings; defaults are the Phase 1 §7 provisional values.</summary>
    [Serializable]
    public sealed class CaptureSettings
    {
        [Tooltip("Requested capture rate (Hz). Used when the device reports 'any rate' or supports it.")]
        [SerializeField] private int requestedRate = ProcessorConfig.DefaultTargetRate;

        [Tooltip("Recording stops automatically after this many seconds (§7: 8 s).")]
        [SerializeField] private int maxSeconds = 8;

        [Tooltip("Extra clip length beyond the maximum so the clip never fills before auto-stop.")]
        [SerializeField] private int headroomSeconds = 1;

        [Tooltip("Give up if the microphone has not produced audio within this time (§7: 2 s).")]
        [SerializeField] private float startupDeadlineSeconds = 2f;

        [Tooltip("Window for the live amplitude meter.")]
        [SerializeField] private float amplitudeWindowSeconds = 0.05f;

        /// <summary>Creates settings with the §7 defaults.</summary>
        public CaptureSettings()
        {
        }

        /// <summary>Creates settings with explicit values (tests, config assets).</summary>
        public CaptureSettings(int requestedRate, int maxSeconds, int headroomSeconds, float startupDeadlineSeconds, float amplitudeWindowSeconds)
        {
            this.requestedRate = requestedRate;
            this.maxSeconds = maxSeconds;
            this.headroomSeconds = headroomSeconds;
            this.startupDeadlineSeconds = startupDeadlineSeconds;
            this.amplitudeWindowSeconds = amplitudeWindowSeconds;
        }

        /// <summary>Requested capture rate in Hz.</summary>
        public int RequestedRate => requestedRate;

        /// <summary>Maximum recording length in seconds.</summary>
        public int MaxSeconds => maxSeconds;

        /// <summary>Clip headroom beyond the maximum, in seconds.</summary>
        public int HeadroomSeconds => headroomSeconds;

        /// <summary>Microphone startup deadline in seconds.</summary>
        public float StartupDeadlineSeconds => startupDeadlineSeconds;

        /// <summary>Amplitude meter window in seconds.</summary>
        public float AmplitudeWindowSeconds => amplitudeWindowSeconds;
    }

    /// <summary>Outcome of a capture operation.</summary>
    public enum CaptureStatus
    {
        /// <summary>Started, or stopped with audio.</summary>
        Ok,

        /// <summary>Cancelled: released during startup, focus lost, paused or explicitly cancelled. Never scored.</summary>
        Cancelled,

        /// <summary>No microphone, or the OS refused to start it.</summary>
        DeviceUnavailable,

        /// <summary>The microphone did not produce audio before the startup deadline.</summary>
        StartTimeout,

        /// <summary>A capture is already in progress.</summary>
        AlreadyActive,

        /// <summary>Stop requested with nothing recording (e.g. a late release after auto-stop): no effect.</summary>
        NotActive,
    }

    /// <summary>Session-only numbers about one capture. Never contains audio (Phase 1 §15).</summary>
    public sealed class CaptureDiagnostics
    {
        internal CaptureDiagnostics(string deviceName, int requestedRate, int grantedRate, int channels)
        {
            DeviceName = deviceName;
            RequestedRate = requestedRate;
            GrantedRate = grantedRate;
            Channels = channels;
        }

        /// <summary>Microphone device name as reported by the OS.</summary>
        public string DeviceName { get; }

        /// <summary>Rate asked for.</summary>
        public int RequestedRate { get; }

        /// <summary>Rate of the clip Unity returned. Unity may resample internally, so this is not necessarily the hardware rate (P00 B3).</summary>
        public int GrantedRate { get; }

        /// <summary>Clip channel count.</summary>
        public int Channels { get; }

        /// <summary>Milliseconds from Microphone.Start to the first audio (P00 B1); null if it never arrived.</summary>
        public double? StartLatencyMilliseconds { get; internal set; }

        /// <summary>Frames extracted for the result.</summary>
        public int FramesCaptured { get; internal set; }

        /// <summary>True when the capture ended at the maximum length or because the OS stopped the device.</summary>
        public bool AutoStopped { get; internal set; }
    }

    /// <summary>Result of starting a capture.</summary>
    public sealed class CaptureStartResult
    {
        internal CaptureStartResult(CaptureStatus status, CaptureDiagnostics diagnostics)
        {
            Status = status;
            Diagnostics = diagnostics;
        }

        /// <summary>Ok when audio is flowing and the player can speak.</summary>
        public CaptureStatus Status { get; }

        /// <summary>Device and rate details when a device was opened; otherwise null.</summary>
        public CaptureDiagnostics Diagnostics { get; }
    }

    /// <summary>Result of ending a capture. <see cref="Audio"/> is set only when <see cref="Status"/> is Ok.</summary>
    public sealed class CaptureResult
    {
        internal CaptureResult(CaptureStatus status, AudioBuffer audio, CaptureDiagnostics diagnostics)
        {
            Status = status;
            Audio = audio;
            Diagnostics = diagnostics;
        }

        /// <summary>Ok, Cancelled or NotActive.</summary>
        public CaptureStatus Status { get; }

        /// <summary>Exactly the recorded frames (position × channels samples), unprocessed. Lives in memory only.</summary>
        public AudioBuffer Audio { get; }

        /// <summary>Capture numbers; null for NotActive.</summary>
        public CaptureDiagnostics Diagnostics { get; }
    }
}
