using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Audio
{
    /// <summary>How a playback ended.</summary>
    public enum PlaybackEnd
    {
        /// <summary>Played to the end.</summary>
        Finished,

        /// <summary>Stopped by <see cref="AudioPlayback.StopAll"/> or replaced by a new playback.</summary>
        Stopped,

        /// <summary>The caller's token was cancelled.</summary>
        Cancelled,
    }

    /// <summary>
    /// Minimal voice playback (P00 finding C10): play one buffer or clip and await its real end; stop.
    /// One voice at a time, so audio never overlaps. Clips made from buffers are destroyed when playback ends.
    /// P08 extends this with Compare sequences and SFX; the recorder never owns playback.
    /// </summary>
    public sealed class AudioPlayback : MonoBehaviour
    {
        [SerializeField] private AudioSource voiceSource;

        private TaskCompletionSource<PlaybackEnd> _current;
        private CancellationTokenRegistration _registration;
        private AudioClip _ownedClip;

        /// <summary>Whether something is playing.</summary>
        public bool IsPlaying => _current != null;

        /// <summary>Sets the voice source (scene builder, tests).</summary>
        public void SetVoiceSource(AudioSource source) => voiceSource = source;

        /// <summary>Plays <paramref name="buffer"/> on the voice source; completes when it finishes, is stopped or cancelled.</summary>
        public Task<PlaybackEnd> PlayAsync(AudioBuffer buffer, CancellationToken token)
        {
            var clip = AudioClipConversion.ToClip(buffer, "VoicePlayback");
            return clip == null ? Task.FromResult(PlaybackEnd.Finished) : Play(clip, true, token);
        }

        /// <summary>Plays an existing clip (not destroyed afterwards).</summary>
        public Task<PlaybackEnd> PlayAsync(AudioClip clip, CancellationToken token) =>
            clip == null ? Task.FromResult(PlaybackEnd.Finished) : Play(clip, false, token);

        /// <summary>Stops all playback; pending tasks complete with Stopped.</summary>
        public void StopAll() => End(PlaybackEnd.Stopped);

        private Task<PlaybackEnd> Play(AudioClip clip, bool owned, CancellationToken token)
        {
            End(PlaybackEnd.Stopped);
            if (token.IsCancellationRequested)
            {
                if (owned) AudioClipConversion.Destroy(clip);
                return Task.FromResult(PlaybackEnd.Cancelled);
            }

            EnsureSource();
            _ownedClip = owned ? clip : null;
            _current = new TaskCompletionSource<PlaybackEnd>(TaskCreationOptions.RunContinuationsAsynchronously);
            _registration = token.Register(() => End(PlaybackEnd.Cancelled), true);
            voiceSource.clip = clip;
            voiceSource.Play();
            return _current.Task;
        }

        private void Update()
        {
            // A paused application or listener does not count as finished.
            if (_current != null && !voiceSource.isPlaying && !AudioListener.pause) End(PlaybackEnd.Finished);
        }

        private void OnDisable() => StopAll();

        private void End(PlaybackEnd how)
        {
            if (_current == null) return;
            var completion = _current;
            _current = null;
            _registration.Dispose();
            if (voiceSource != null)
            {
                voiceSource.Stop();
                voiceSource.clip = null;
            }

            AudioClipConversion.Destroy(_ownedClip);
            _ownedClip = null;
            completion.TrySetResult(how);
        }

        private void EnsureSource()
        {
            if (voiceSource != null) return;
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
        }
    }
}
