using System;
using System.Threading;
using NUnit.Framework;
using Uprise.SpeakBack.Audio;

namespace Uprise.SpeakBack.Tests
{
    public sealed class MicrophonePermissionServiceTests
    {
        private sealed class FakePermissions : IMicrophonePermissionBackend
        {
            public MicrophonePermissionState Current = MicrophonePermissionState.Unknown;
            public MicrophonePermissionState Answer = MicrophonePermissionState.Granted;
            public Action<MicrophonePermissionState> Pending;
            public int Requests;
            public int SettingsOpened;
            public bool AnswerImmediately = true;

            public MicrophonePermissionState Query() => Current;

            public void Request(Action<MicrophonePermissionState> onResult)
            {
                Requests++;
                if (AnswerImmediately) onResult(Answer);
                else Pending = onResult;
            }

            public void OpenAppSettings() => SettingsOpened++;
        }

        [Test]
        public void AlreadyGranted_DoesNotPrompt()
        {
            var backend = new FakePermissions { Current = MicrophonePermissionState.Granted };
            var service = new MicrophonePermissionService(backend);

            var result = service.EnsureMicrophoneAsync(CancellationToken.None).Result;

            Assert.That(result, Is.EqualTo(MicrophonePermissionState.Granted));
            Assert.That(backend.Requests, Is.EqualTo(0));
        }

        [TestCase(MicrophonePermissionState.Granted)]
        [TestCase(MicrophonePermissionState.Denied)]
        [TestCase(MicrophonePermissionState.DeniedPermanently)]
        public void NotYetGranted_PromptsAndReportsAnswer(MicrophonePermissionState answer)
        {
            var backend = new FakePermissions { Answer = answer };
            var service = new MicrophonePermissionService(backend);

            var result = service.EnsureMicrophoneAsync(CancellationToken.None).Result;

            Assert.That(result, Is.EqualTo(answer));
            Assert.That(service.State, Is.EqualTo(answer));
            Assert.That(backend.Requests, Is.EqualTo(1));
        }

        [Test]
        public void NoMicrophone_IsUnsupported_WithoutPrompt()
        {
            var backend = new FakePermissions { Current = MicrophonePermissionState.Unsupported };

            var result = new MicrophonePermissionService(backend).EnsureMicrophoneAsync(CancellationToken.None).Result;

            Assert.That(result, Is.EqualTo(MicrophonePermissionState.Unsupported));
            Assert.That(backend.Requests, Is.EqualTo(0));
        }

        [Test]
        public void Cancelled_WhileWaitingForAnswer()
        {
            var backend = new FakePermissions { AnswerImmediately = false };
            var service = new MicrophonePermissionService(backend);
            var cts = new CancellationTokenSource();

            var task = service.EnsureMicrophoneAsync(cts.Token);
            cts.Cancel();

            Assert.That(task.IsCanceled, Is.True);
            cts.Dispose();
        }

        [Test]
        public void OpenAppSettings_IsForwarded()
        {
            var backend = new FakePermissions();

            new MicrophonePermissionService(backend).OpenAppSettings();

            Assert.That(backend.SettingsOpened, Is.EqualTo(1));
        }
    }
}
