using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Uprise.SpeakBack.Core.Audio;
using Object = UnityEngine.Object;

namespace Uprise.SpeakBack.Audio
{
    /// <summary>
    /// Single-capture state machine implementing Phase 1 §8. Main-thread only; driven by
    /// <see cref="Tick"/> once per frame (from <see cref="MicrophoneRecorder"/>), so it never blocks or spins.
    /// <list type="number">
    /// <item>Default device; request the configured rate if supported ((0,0) = any), else the nearest supported rate.</item>
    /// <item>Non-looping clip of max + headroom seconds; actual clip rate and channels recorded.</item>
    /// <item>Awaits position &gt; 0 with a deadline.</item>
    /// <item>Positions are sample frames; the last valid position is cached because it can reset when the clip fills.</item>
    /// <item>On stop, position is read before the microphone is ended, then exactly frames × channels samples are extracted.</item>
    /// <item>The microphone is always ended and the clip destroyed on stop, cancel, error or pause.</item>
    /// </list>
    /// Every capture ends exactly once with <see cref="Completed"/> (Ok or Cancelled), whether stopped by the
    /// player, by the maximum length or by cancellation. A later stop request has no effect.
    /// </summary>
    public sealed class MicrophoneCapture : IDisposable
    {
        private enum Phase
        {
            Idle,
            Starting,
            Recording,
        }

        private readonly IMicrophoneBackend _backend;
        private readonly CaptureSettings _settings;
        private readonly Func<double> _clock;

        private Phase _phase;
        private string _device;
        private AudioClip _clip;
        private int _channels;
        private int _rate;
        private int _maxFrames;
        private int _lastPosition;
        private double _startTime;
        private CaptureDiagnostics _diagnostics;
        private TaskCompletionSource<CaptureStartResult> _startCompletion;
        private CancellationTokenRegistration _cancellationRegistration;
        private volatile bool _cancelRequested;
        private float[] _amplitudeScratch;

        /// <summary>Creates a capture over <paramref name="backend"/>; <paramref name="clock"/> returns seconds (defaults to realtime).</summary>
        public MicrophoneCapture(IMicrophoneBackend backend, CaptureSettings settings, Func<double> clock = null)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _clock = clock ?? (() => Time.realtimeSinceStartupAsDouble);
        }

        /// <summary>Raised once when a started capture ends: Ok with audio, or Cancelled.</summary>
        public event Action<CaptureResult> Completed;

        /// <summary>Whether a capture is starting or recording.</summary>
        public bool IsActive => _phase != Phase.Idle;

        /// <summary>Whether audio is flowing (startup finished).</summary>
        public bool IsRecording => _phase == Phase.Recording;

        /// <summary>RMS of the most recent audio window, for the level meter; 0 when not recording.</summary>
        public float Amplitude { get; private set; }

        /// <summary>Recorded length so far in seconds.</summary>
        public double ElapsedSeconds => _phase == Phase.Recording && _rate > 0 ? (double)_lastPosition / _rate : 0.0;

        /// <summary>Maximum recording length in seconds.</summary>
        public int MaxSeconds => _settings.MaxSeconds;

        /// <summary>
        /// Opens the microphone and completes when audio is flowing (Ok), or with DeviceUnavailable,
        /// StartTimeout, Cancelled or AlreadyActive. Cancelling <paramref name="token"/> cancels the capture.
        /// </summary>
        public Task<CaptureStartResult> StartCaptureAsync(CancellationToken token)
        {
            if (_phase != Phase.Idle) return Task.FromResult(new CaptureStartResult(CaptureStatus.AlreadyActive, null));
            if (token.IsCancellationRequested) return Task.FromResult(new CaptureStartResult(CaptureStatus.Cancelled, null));

            var devices = _backend.Devices;
            if (devices == null || devices.Length == 0)
            {
                return Task.FromResult(new CaptureStartResult(CaptureStatus.DeviceUnavailable, null));
            }

            _device = devices[0];
            _backend.GetDeviceCaps(_device, out var minRate, out var maxRate);
            var rate = minRate == 0 && maxRate == 0
                ? _settings.RequestedRate
                : Mathf.Clamp(_settings.RequestedRate, minRate, maxRate);

            _startTime = _clock();
            AudioClip clip;
            try
            {
                clip = _backend.Start(_device, false, _settings.MaxSeconds + _settings.HeadroomSeconds, rate);
            }
            catch (Exception)
            {
                clip = null;
            }

            if (clip == null)
            {
                SafeEnd();
                return Task.FromResult(new CaptureStartResult(CaptureStatus.DeviceUnavailable, null));
            }

            _clip = clip;
            _rate = clip.frequency;
            _channels = clip.channels;
            _maxFrames = _settings.MaxSeconds * _rate;
            _lastPosition = 0;
            _cancelRequested = false;
            Amplitude = 0f;
            _diagnostics = new CaptureDiagnostics(_device, rate, _rate, _channels);
            _phase = Phase.Starting;
            _startCompletion = new TaskCompletionSource<CaptureStartResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            // The callback may run on another thread; it only sets a flag that Tick acts on.
            _cancellationRegistration = token.Register(() => _cancelRequested = true);
            return _startCompletion.Task;
        }

        /// <summary>Advances startup, tracks position and amplitude, and auto-stops at the maximum length. Call once per frame.</summary>
        public void Tick()
        {
            if (_phase == Phase.Idle) return;

            if (_cancelRequested)
            {
                CancelCapture();
                return;
            }

            var position = _backend.GetPosition(_device);

            if (_phase == Phase.Starting)
            {
                if (position > 0)
                {
                    _lastPosition = position;
                    _diagnostics.StartLatencyMilliseconds = (_clock() - _startTime) * 1000.0;
                    _phase = Phase.Recording;
                    _startCompletion.TrySetResult(new CaptureStartResult(CaptureStatus.Ok, _diagnostics));
                }
                else if (_clock() - _startTime > _settings.StartupDeadlineSeconds || !_backend.IsRecording(_device))
                {
                    var status = _backend.IsRecording(_device) ? CaptureStatus.StartTimeout : CaptureStatus.DeviceUnavailable;
                    var diagnostics = _diagnostics;
                    Teardown();
                    _startCompletion.TrySetResult(new CaptureStartResult(status, diagnostics));
                }

                return;
            }

            // A non-looping clip can report 0 once full, so only ever move forwards.
            if (position > _lastPosition) _lastPosition = position;
            UpdateAmplitude();

            if (_lastPosition >= _maxFrames || !_backend.IsRecording(_device))
            {
                Finish(autoStopped: true);
            }
        }

        /// <summary>
        /// Player released: ends the capture and returns its result (also raised via <see cref="Completed"/>).
        /// During startup this cancels. When nothing is recording it returns NotActive and does nothing.
        /// </summary>
        public CaptureResult StopCapture()
        {
            switch (_phase)
            {
                case Phase.Starting:
                    return CancelCapture();
                case Phase.Recording:
                    var position = _backend.GetPosition(_device);
                    if (position > _lastPosition) _lastPosition = position;
                    return Finish(autoStopped: false);
                default:
                    return new CaptureResult(CaptureStatus.NotActive, null, null);
            }
        }

        /// <summary>Ends the microphone and discards the audio. Safe to call at any time.</summary>
        public CaptureResult CancelCapture()
        {
            if (_phase == Phase.Idle) return new CaptureResult(CaptureStatus.NotActive, null, null);

            var wasStarting = _phase == Phase.Starting;
            var diagnostics = _diagnostics;
            Teardown();
            var result = new CaptureResult(CaptureStatus.Cancelled, null, diagnostics);

            if (wasStarting)
            {
                _startCompletion.TrySetResult(new CaptureStartResult(CaptureStatus.Cancelled, diagnostics));
            }
            else
            {
                Completed?.Invoke(result);
            }

            return result;
        }

        /// <summary>Cancels any capture in progress.</summary>
        public void Dispose() => CancelCapture();

        private CaptureResult Finish(bool autoStopped)
        {
            var frames = Math.Min(_lastPosition, _maxFrames);
            var samples = new float[frames * _channels];
            if (frames > 0) _clip.GetData(samples, 0);

            var audio = AudioBuffer.Create(samples, _rate, _channels);
            var diagnostics = _diagnostics;
            diagnostics.FramesCaptured = frames;
            diagnostics.AutoStopped = autoStopped;

            Teardown();
            var result = new CaptureResult(CaptureStatus.Ok, audio, diagnostics);
            Completed?.Invoke(result);
            return result;
        }

        private void UpdateAmplitude()
        {
            var windowFrames = Math.Max(1, (int)(_settings.AmplitudeWindowSeconds * _rate));
            var start = Math.Max(0, _lastPosition - windowFrames);
            var count = windowFrames * _channels;
            if (_amplitudeScratch == null || _amplitudeScratch.Length != count) _amplitudeScratch = new float[count];
            _clip.GetData(_amplitudeScratch, start);
            Amplitude = LevelAnalyzer.Rms(_amplitudeScratch);
        }

        private void Teardown()
        {
            SafeEnd();
            if (_clip != null)
            {
                if (Application.isPlaying) Object.Destroy(_clip);
                else Object.DestroyImmediate(_clip);
            }

            _cancellationRegistration.Dispose();
            _clip = null;
            _phase = Phase.Idle;
            _cancelRequested = false;
            Amplitude = 0f;
        }

        private void SafeEnd()
        {
            try
            {
                _backend.End(_device);
            }
            catch (Exception)
            {
                // Ending a device that already stopped must never leave the capture stuck.
            }
        }
    }
}
