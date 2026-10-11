using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Uprise.SpeakBack.Audio
{
    /// <summary>
    /// The game's single microphone owner (Phase 1 §6, §8; CLAUDE.md). Hosts <see cref="MicrophoneCapture"/>,
    /// drives it every frame and cancels on pause, focus loss, disable or destroy so a held microphone is
    /// never resumed. Nothing else may record. Never logs or stores audio.
    /// </summary>
    public sealed class MicrophoneRecorder : MonoBehaviour
    {
        [SerializeField] private CaptureSettings settings = new CaptureSettings();

        private MicrophoneCapture _capture;

        /// <summary>Raised once when a capture ends (Ok with audio, or Cancelled).</summary>
        public event Action<CaptureResult> Completed;

        /// <summary>Whether a capture is starting or recording.</summary>
        public bool IsActive => Capture.IsActive;

        /// <summary>Whether audio is flowing.</summary>
        public bool IsRecording => Capture.IsRecording;

        /// <summary>Live RMS level for the meter.</summary>
        public float Amplitude => Capture.Amplitude;

        /// <summary>Recorded seconds so far.</summary>
        public double ElapsedSeconds => Capture.ElapsedSeconds;

        /// <summary>Maximum recording length in seconds.</summary>
        public int MaxSeconds => Capture.MaxSeconds;

        /// <summary>Whether any microphone exists, for permission checks on platforms without runtime permissions.</summary>
        public bool HasAnyDevice
        {
            get
            {
                var devices = (Backend ?? InitialiseDefault()).Devices;
                return devices != null && devices.Length > 0;
            }
        }

        private IMicrophoneBackend Backend { get; set; }

        private MicrophoneCapture Capture
        {
            get
            {
                if (_capture == null) InitialiseDefault();
                return _capture;
            }
        }

        private IMicrophoneBackend InitialiseDefault()
        {
            Initialise(new UnityMicrophoneBackend(), null);
            return Backend;
        }

        /// <summary>Replaces the backend and clock (tests). Must be called before the first capture.</summary>
        public void Initialise(IMicrophoneBackend backend, Func<double> clock, CaptureSettings captureSettings = null)
        {
            if (_capture != null && _capture.IsActive) throw new InvalidOperationException("Cannot re-initialise during a capture.");
            if (_capture != null) _capture.Completed -= OnCaptureCompleted;

            Backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _capture = new MicrophoneCapture(backend, captureSettings ?? settings, clock);
            _capture.Completed += OnCaptureCompleted;
        }

        /// <summary>Opens the microphone; completes when the player can speak. See <see cref="MicrophoneCapture.StartCaptureAsync"/>.</summary>
        public Task<CaptureStartResult> StartCaptureAsync(CancellationToken token) => Capture.StartCaptureAsync(token);

        /// <summary>Player released. See <see cref="MicrophoneCapture.StopCapture"/>.</summary>
        public CaptureResult StopCapture() => Capture.StopCapture();

        /// <summary>Discards the capture in progress, if any.</summary>
        public void CancelCapture()
        {
            if (_capture != null) _capture.CancelCapture();
        }

        private void Update()
        {
            if (_capture != null) _capture.Tick();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) CancelCapture();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) CancelCapture();
        }

        private void OnDisable() => CancelCapture();

        private void OnDestroy()
        {
            CancelCapture();
            if (_capture != null) _capture.Completed -= OnCaptureCompleted;
        }

        private void OnCaptureCompleted(CaptureResult result) => Completed?.Invoke(result);
    }
}
