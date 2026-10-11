using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Uprise.SpeakBack.Audio
{
    /// <summary>Microphone permission as far as the game knows it.</summary>
    public enum MicrophonePermissionState
    {
        /// <summary>Not yet asked.</summary>
        Unknown,

        /// <summary>Recording allowed.</summary>
        Granted,

        /// <summary>Refused; asking again is still possible.</summary>
        Denied,

        /// <summary>Refused with "don't ask again": only the app settings screen can change it.</summary>
        DeniedPermanently,

        /// <summary>No microphone on this device or platform.</summary>
        Unsupported,
    }

    /// <summary>Platform permission operations behind <see cref="MicrophonePermissionService"/>.</summary>
    public interface IMicrophonePermissionBackend
    {
        /// <summary>Current state without prompting.</summary>
        MicrophonePermissionState Query();

        /// <summary>Shows the OS prompt; <paramref name="onResult"/> receives Granted, Denied or DeniedPermanently.</summary>
        void Request(Action<MicrophonePermissionState> onResult);

        /// <summary>Opens this app's OS settings page, where a permanent denial can be reversed.</summary>
        void OpenAppSettings();
    }

    /// <summary>
    /// Microphone permission (Phase 1 §8 step 1, Master §28). The caller explains why the microphone is
    /// needed first, then calls <see cref="EnsureMicrophoneAsync"/>. Results are delivered on the main thread.
    /// Owns no recording.
    /// </summary>
    public sealed class MicrophonePermissionService
    {
        private readonly IMicrophonePermissionBackend _backend;

        /// <summary>Creates the service over <paramref name="backend"/>.</summary>
        public MicrophonePermissionService(IMicrophonePermissionBackend backend)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        /// <summary>Last known state.</summary>
        public MicrophonePermissionState State { get; private set; } = MicrophonePermissionState.Unknown;

        /// <summary>Platform backend: Android runtime permissions on device; elsewhere granted when a microphone exists.</summary>
        public static IMicrophonePermissionBackend CreatePlatformBackend(Func<bool> hasMicrophone)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidMicrophonePermissionBackend();
#else
            return new DevicePresenceBackend(hasMicrophone);
#endif
        }

        /// <summary>Returns Granted immediately if already allowed; otherwise shows the OS prompt and awaits the answer.</summary>
        public Task<MicrophonePermissionState> EnsureMicrophoneAsync(CancellationToken token)
        {
            var current = _backend.Query();
            if (current == MicrophonePermissionState.Granted || current == MicrophonePermissionState.Unsupported)
            {
                State = current;
                return Task.FromResult(current);
            }

            var completion = new TaskCompletionSource<MicrophonePermissionState>(TaskCreationOptions.RunContinuationsAsynchronously);
            var context = SynchronizationContext.Current;
            var registration = token.Register(() => completion.TrySetCanceled());

            _backend.Request(result =>
            {
                void Deliver()
                {
                    State = result;
                    registration.Dispose();
                    completion.TrySetResult(result);
                }

                // OS callbacks may arrive on a Java thread; hand them back to Unity's main thread.
                if (context != null && SynchronizationContext.Current != context) context.Post(_ => Deliver(), null);
                else Deliver();
            });

            return completion.Task;
        }

        /// <summary>Opens the app settings page (permanent denial recovery).</summary>
        public void OpenAppSettings() => _backend.OpenAppSettings();

        private sealed class DevicePresenceBackend : IMicrophonePermissionBackend
        {
            private readonly Func<bool> _hasMicrophone;

            public DevicePresenceBackend(Func<bool> hasMicrophone)
            {
                _hasMicrophone = hasMicrophone ?? (() => false);
            }

            public MicrophonePermissionState Query() =>
                _hasMicrophone() ? MicrophonePermissionState.Granted : MicrophonePermissionState.Unsupported;

            public void Request(Action<MicrophonePermissionState> onResult) => onResult(Query());

            public void OpenAppSettings()
            {
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private sealed class AndroidMicrophonePermissionBackend : IMicrophonePermissionBackend
        {
            public MicrophonePermissionState Query() =>
                Permission.HasUserAuthorizedPermission(Permission.Microphone)
                    ? MicrophonePermissionState.Granted
                    : MicrophonePermissionState.Unknown;

            public void Request(Action<MicrophonePermissionState> onResult)
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => onResult(MicrophonePermissionState.Granted);

                // After a refusal, Android only offers a rationale while asking again is still possible; no
                // rationale means "don't ask again" (or a policy block), so the settings page is the only route.
                callbacks.PermissionDenied += _ => onResult(
                    Permission.ShouldShowRequestPermissionRationale(Permission.Microphone)
                        ? MicrophonePermissionState.Denied
                        : MicrophonePermissionState.DeniedPermanently);
                Permission.RequestUserPermission(Permission.Microphone, callbacks);
            }

            public void OpenAppSettings()
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (var uri = uriClass.CallStatic<AndroidJavaObject>("fromParts", "package", Application.identifier, null))
                using (var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uri))
                {
                    intent.Call<AndroidJavaObject>("addFlags", 0x10000000); // FLAG_ACTIVITY_NEW_TASK
                    activity.Call("startActivity", intent);
                }
            }
        }
#endif
    }
}
