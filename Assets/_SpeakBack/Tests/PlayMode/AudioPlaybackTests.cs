using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Uprise.SpeakBack.Audio;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Tests
{
    public sealed class AudioPlaybackTests
    {
        private static AudioBuffer Tone(double seconds)
        {
            var frames = (int)(seconds * 16000);
            var samples = new float[frames];
            for (var i = 0; i < frames; i++) samples[i] = 0.1f * Mathf.Sin(2f * Mathf.PI * 440f * i / 16000f);
            return AudioBuffer.Create(samples, 16000, 1);
        }

        private static IEnumerator WaitFor(Task task, float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
        }

        [UnityTest]
        public IEnumerator Play_CompletesWhenTheClipEnds()
        {
            var go = new GameObject("Playback", typeof(AudioSource));
            var playback = go.AddComponent<AudioPlayback>();
            playback.SetVoiceSource(go.GetComponent<AudioSource>());

            var task = playback.PlayAsync(Tone(0.1), CancellationToken.None);
            yield return WaitFor(task, 3f);

            Assert.That(task.IsCompleted, Is.True);
            Assert.That(task.Result, Is.EqualTo(PlaybackEnd.Finished));
            Assert.That(playback.IsPlaying, Is.False);
            Assert.That(go.GetComponent<AudioSource>().clip, Is.Null, "clip released after playback");
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator StopAll_EndsWithStopped_AndNewPlaybackReplacesOld()
        {
            var go = new GameObject("Playback", typeof(AudioSource));
            var playback = go.AddComponent<AudioPlayback>();
            playback.SetVoiceSource(go.GetComponent<AudioSource>());

            var first = playback.PlayAsync(Tone(2.0), CancellationToken.None);
            var second = playback.PlayAsync(Tone(2.0), CancellationToken.None);
            yield return null;

            Assert.That(first.IsCompleted && first.Result == PlaybackEnd.Stopped, Is.True, "one voice at a time");

            playback.StopAll();
            yield return WaitFor(second, 1f);

            Assert.That(second.Result, Is.EqualTo(PlaybackEnd.Stopped));
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Cancel_EndsWithCancelled()
        {
            var go = new GameObject("Playback", typeof(AudioSource));
            var playback = go.AddComponent<AudioPlayback>();
            playback.SetVoiceSource(go.GetComponent<AudioSource>());
            var cts = new CancellationTokenSource();

            var task = playback.PlayAsync(Tone(2.0), cts.Token);
            cts.Cancel();
            yield return WaitFor(task, 1f);

            Assert.That(task.Result, Is.EqualTo(PlaybackEnd.Cancelled));
            cts.Dispose();
            Object.Destroy(go);
        }

        [Test]
        public void EmptyBuffer_FinishesImmediately()
        {
            var go = new GameObject("Playback", typeof(AudioSource));
            var playback = go.AddComponent<AudioPlayback>();

            var task = playback.PlayAsync(AudioBuffer.Silence(0, 16000, 1), CancellationToken.None);

            Assert.That(task.IsCompleted && task.Result == PlaybackEnd.Finished, Is.True);
            Object.DestroyImmediate(go);
        }
    }
}
