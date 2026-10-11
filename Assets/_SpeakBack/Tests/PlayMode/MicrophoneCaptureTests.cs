using System.Collections;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Uprise.SpeakBack.Audio;

namespace Uprise.SpeakBack.Tests
{
    /// <summary>Phase 1 §8 capture rules and §17 "PlayMode with fake services" scenarios.</summary>
    public sealed class MicrophoneCaptureTests
    {
        private FakeMicrophoneBackend _mic;
        private double _now;
        private int _completedCount;
        private CaptureResult _lastCompleted;

        private MicrophoneCapture Create(int maxSeconds = 8)
        {
            _mic = new FakeMicrophoneBackend();
            _now = 100.0;
            _completedCount = 0;
            _lastCompleted = null;
            var capture = new MicrophoneCapture(_mic, new CaptureSettings(16000, maxSeconds, 1, 2f, 0.05f), () => _now);
            capture.Completed += r =>
            {
                _completedCount++;
                _lastCompleted = r;
            };
            return capture;
        }

        private CaptureStartResult StartRecording(MicrophoneCapture capture, int firstPosition = 160)
        {
            var start = capture.StartCaptureAsync(CancellationToken.None);
            _mic.Position = firstPosition;
            capture.Tick();
            Assert.That(start.IsCompleted, Is.True, "startup should complete once audio flows");
            return start.Result;
        }

        [Test]
        public void Start_WaitsForFirstAudio_WithoutBlocking()
        {
            var capture = Create();

            var start = capture.StartCaptureAsync(CancellationToken.None);
            capture.Tick();
            Assert.That(start.IsCompleted, Is.False, "position 0 means the mic hasn't produced audio yet");

            _now += 0.35;
            _mic.Position = 50;
            capture.Tick();

            Assert.That(start.Result.Status, Is.EqualTo(CaptureStatus.Ok));
            Assert.That(start.Result.Diagnostics.StartLatencyMilliseconds, Is.EqualTo(350.0).Within(1e-6));
            Assert.That(capture.IsRecording, Is.True);
            Assert.That(_mic.RequestedLoop, Is.False, "non-looping clip");
            Assert.That(_mic.RequestedLengthSeconds, Is.EqualTo(9), "max 8 s + 1 s headroom");
        }

        [TestCase(0, 0, 16000)]
        [TestCase(8000, 48000, 16000)]
        [TestCase(44100, 48000, 44100)]
        [TestCase(8000, 11025, 11025)]
        public void Start_PicksRequestedOrNearestSupportedRate(int min, int max, int expected)
        {
            var capture = Create();
            _mic.MinRate = min;
            _mic.MaxRate = max;

            var result = StartRecording(capture);

            Assert.That(_mic.RequestedRate, Is.EqualTo(expected));
            Assert.That(result.Diagnostics.RequestedRate, Is.EqualTo(expected));
            Assert.That(result.Diagnostics.GrantedRate, Is.EqualTo(expected), "clip rate recorded");
        }

        [Test]
        public void Start_NoDevice_IsDeviceUnavailable()
        {
            var capture = Create();
            _mic.Devices = new string[0];

            var start = capture.StartCaptureAsync(CancellationToken.None);

            Assert.That(start.Result.Status, Is.EqualTo(CaptureStatus.DeviceUnavailable));
            Assert.That(_mic.StartCalls, Is.EqualTo(0));
        }

        [Test]
        public void Start_FailedToOpen_EndsMicAndReportsUnavailable()
        {
            var capture = Create();
            _mic.StartReturnsNull = true;

            var start = capture.StartCaptureAsync(CancellationToken.None);

            Assert.That(start.Result.Status, Is.EqualTo(CaptureStatus.DeviceUnavailable));
            Assert.That(_mic.EndCalls, Is.EqualTo(1));
            Assert.That(capture.IsActive, Is.False);
        }

        [Test]
        public void Start_MissesDeadline_TimesOutAndEndsMic()
        {
            var capture = Create();
            var start = capture.StartCaptureAsync(CancellationToken.None);

            _now += 2.1;
            capture.Tick();

            Assert.That(start.Result.Status, Is.EqualTo(CaptureStatus.StartTimeout));
            Assert.That(_mic.EndCalls, Is.EqualTo(1));
            Assert.That(capture.IsActive, Is.False);
            Assert.That(_completedCount, Is.EqualTo(0), "a failed start is not a completed capture");
        }

        [Test]
        public void Start_WhileActive_IsRejected()
        {
            var capture = Create();
            StartRecording(capture);

            var second = capture.StartCaptureAsync(CancellationToken.None);

            Assert.That(second.Result.Status, Is.EqualTo(CaptureStatus.AlreadyActive));
            Assert.That(_mic.StartCalls, Is.EqualTo(1));
        }

        [Test]
        public void ReleaseDuringStartup_CancelsSafely()
        {
            var capture = Create();
            var start = capture.StartCaptureAsync(CancellationToken.None);

            var stop = capture.StopCapture();

            Assert.That(stop.Status, Is.EqualTo(CaptureStatus.Cancelled));
            Assert.That(stop.Audio, Is.Null);
            Assert.That(start.Result.Status, Is.EqualTo(CaptureStatus.Cancelled));
            Assert.That(_mic.EndCalls, Is.EqualTo(1));
            Assert.That(_completedCount, Is.EqualTo(0));
            Assert.That(capture.IsActive, Is.False);
        }

        [Test]
        public void CancellationToken_CancelsOnNextTick()
        {
            var capture = Create();
            using (var cts = new CancellationTokenSource())
            {
                var start = capture.StartCaptureAsync(cts.Token);

                cts.Cancel();
                capture.Tick();

                Assert.That(start.Result.Status, Is.EqualTo(CaptureStatus.Cancelled));
                Assert.That(_mic.EndCalls, Is.EqualTo(1));
            }
        }

        [Test]
        public void Stop_ReadsPositionBeforeEndingMic()
        {
            var capture = Create();
            StartRecording(capture);
            _mic.Position = 5000; // no Tick: the stop itself must read the latest position before End resets it

            var result = capture.StopCapture();

            Assert.That(result.Status, Is.EqualTo(CaptureStatus.Ok));
            Assert.That(result.Audio.FrameCount, Is.EqualTo(5000));
            Assert.That(_mic.EndCalls, Is.EqualTo(1));
        }

        [Test]
        public void ExtractedSampleCount_EqualsPositionTimesChannels()
        {
            var capture = Create();
            _mic.Channels = 2;
            StartRecording(capture);
            _mic.Position = 1234;

            var result = capture.StopCapture();

            Assert.That(result.Audio.Channels, Is.EqualTo(2));
            Assert.That(result.Audio.FrameCount, Is.EqualTo(1234));
            Assert.That(result.Audio.SampleCount, Is.EqualTo(1234 * 2));
            var expected = new float[1234 * 2];
            System.Array.Copy(_mic.ClipData, expected, expected.Length);
            Assert.That(result.Audio.ToArray(), Is.EqualTo(expected), "exactly the recorded prefix; the tail is discarded");
            Assert.That(result.Diagnostics.FramesCaptured, Is.EqualTo(1234));
        }

        [Test]
        public void DoubleRelease_ProcessesOnce()
        {
            var capture = Create();
            StartRecording(capture);
            _mic.Position = 1600;

            var first = capture.StopCapture();
            var second = capture.StopCapture();

            Assert.That(first.Status, Is.EqualTo(CaptureStatus.Ok));
            Assert.That(second.Status, Is.EqualTo(CaptureStatus.NotActive));
            Assert.That(_completedCount, Is.EqualTo(1));
            Assert.That(_mic.EndCalls, Is.EqualTo(1));
        }

        [Test]
        public void MaxLength_AutoStopsOnce_AndLateReleaseHasNoEffect()
        {
            var capture = Create(maxSeconds: 1);
            StartRecording(capture);

            _mic.Position = 17000; // past 1 s at 16 kHz
            capture.Tick();

            Assert.That(_completedCount, Is.EqualTo(1));
            Assert.That(_lastCompleted.Status, Is.EqualTo(CaptureStatus.Ok));
            Assert.That(_lastCompleted.Audio.FrameCount, Is.EqualTo(16000), "clamped to the maximum length");
            Assert.That(_lastCompleted.Diagnostics.AutoStopped, Is.True);

            var late = capture.StopCapture();

            Assert.That(late.Status, Is.EqualTo(CaptureStatus.NotActive));
            Assert.That(_completedCount, Is.EqualTo(1));
        }

        [Test]
        public void DeviceStopsWithPositionReset_UsesCachedPosition()
        {
            var capture = Create();
            StartRecording(capture);
            _mic.Position = 8000;
            capture.Tick();

            _mic.Position = 0;
            _mic.Recording = false;
            capture.Tick();

            Assert.That(_completedCount, Is.EqualTo(1));
            Assert.That(_lastCompleted.Audio.FrameCount, Is.EqualTo(8000));
        }

        [Test]
        public void Cancel_WhileRecording_DiscardsAudio()
        {
            var capture = Create();
            StartRecording(capture);
            _mic.Position = 3000;

            capture.CancelCapture();

            Assert.That(_completedCount, Is.EqualTo(1));
            Assert.That(_lastCompleted.Status, Is.EqualTo(CaptureStatus.Cancelled));
            Assert.That(_lastCompleted.Audio, Is.Null);
            Assert.That(_mic.EndCalls, Is.EqualTo(1));
            Assert.That(capture.StopCapture().Status, Is.EqualTo(CaptureStatus.NotActive));
        }

        [Test]
        public void Amplitude_ReflectsRecentAudio()
        {
            var capture = Create();
            StartRecording(capture);
            _mic.Position = 4000;

            capture.Tick();

            Assert.That(capture.Amplitude, Is.GreaterThan(0.1f));
            Assert.That(capture.ElapsedSeconds, Is.EqualTo(0.25).Within(1e-9));
        }

        [UnityTest]
        public IEnumerator Recorder_PauseCancelsCapture_AndFreesClip()
        {
            var go = new GameObject("Recorder");
            var recorder = go.AddComponent<MicrophoneRecorder>();
            var mic = new FakeMicrophoneBackend();
            var now = 0.0;
            recorder.Initialise(mic, () => now);
            CaptureResult completed = null;
            recorder.Completed += r => completed = r;

            var start = recorder.StartCaptureAsync(CancellationToken.None);
            mic.Position = 320;
            yield return null; // Update → Tick
            Assert.That(start.IsCompleted && start.Result.Status == CaptureStatus.Ok, Is.True);
            var clip = mic.Clip;

            go.SendMessage("OnApplicationPause", true);
            yield return null;

            Assert.That(completed, Is.Not.Null);
            Assert.That(completed.Status, Is.EqualTo(CaptureStatus.Cancelled));
            Assert.That(mic.EndCalls, Is.EqualTo(1));
            Assert.That(clip == null, Is.True, "clip destroyed");
            Assert.That(recorder.IsActive, Is.False);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Recorder_FocusLossCancels()
        {
            var go = new GameObject("Recorder");
            var recorder = go.AddComponent<MicrophoneRecorder>();
            var mic = new FakeMicrophoneBackend();
            recorder.Initialise(mic, () => 0.0);

            recorder.StartCaptureAsync(CancellationToken.None);
            go.SendMessage("OnApplicationFocus", false);
            yield return null;

            Assert.That(recorder.IsActive, Is.False);
            Assert.That(mic.EndCalls, Is.EqualTo(1));
            Object.Destroy(go);
        }
    }
}
