using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Audio;
using Uprise.SpeakBack.Core.Content;
using Uprise.SpeakBack.Dev.Bakeoff;

namespace Uprise.SpeakBack.EditorTools.Tests
{
    /// <summary>Bake-off Recorder logic that runs without a microphone: setup validation, storage and sidecars.</summary>
    public sealed class BakeoffRecorderTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "speakback-bakeoff-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [Test]
        public void Participant_ValidInput_IsAccepted_AndNormalised()
        {
            var participant = BakeoffParticipant.TryCreate(" s07 ", 2, 1, "C-2026-001", true, out var problem);

            Assert.That(problem, Is.Null);
            Assert.That(participant.SpeakerCode, Is.EqualTo("S07"));
            Assert.That(participant.Accent, Is.EqualTo("Northern England"));
            Assert.That(participant.Voice, Is.EqualTo("middle"));
        }

        [TestCase("Jamie", 0, 0, "C-1", true)] // a name instead of a code
        [TestCase("S1", 0, 0, "C-1", true)]
        [TestCase("S01", -1, 0, "C-1", true)]
        [TestCase("S01", 0, -1, "C-1", true)]
        [TestCase("S01", 0, 0, "Jane Smith", true)] // spaces: not an ID
        [TestCase("S01", 0, 0, "", true)]
        [TestCase("S01", 0, 0, "C-1", false)] // consent not confirmed
        public void Participant_InvalidInput_IsRefused(string code, int accent, int voice, string consent, bool confirmed)
        {
            var participant = BakeoffParticipant.TryCreate(code, accent, voice, consent, confirmed, out var problem);

            Assert.That(participant, Is.Null);
            Assert.That(problem, Is.Not.Empty);
        }

        [Test]
        public void References_SaveForwardAndFrameCorrectReversal()
        {
            var storage = new BakeoffStorage(_root);
            var forward = AudioBuffer.Create(new[] { 0.1f, 0.2f, 0.3f, 0.4f }, 16000, 1);

            storage.SaveReference("sb_en_001", forward);

            Assert.That(storage.HasReference("sb_en_001"), Is.True);
            Assert.That(storage.HasReference("sb_en_002"), Is.False);
            Assert.That(storage.LoadForwardReference("sb_en_001").ToArray(), Is.EqualTo(forward.ToArray()).Within(1f / 32767f));
            Assert.That(storage.LoadReversedReference("sb_en_001").ToArray(), Is.EqualTo(new[] { 0.4f, 0.3f, 0.2f, 0.1f }).Within(1f / 32767f));
        }

        [Test]
        public void Take_WritesWavsAndSidecar_ThenSessionDeletes()
        {
            var storage = new BakeoffStorage(_root);
            var processed = ProcessedTake();
            var phrase = new PhraseInfo("sb_en_001", "Blue banana", new[] { "b" }, 1, "sbps-v1");
            var participant = BakeoffParticipant.TryCreate("S01", 0, 0, "C-1", true, out _);
            var session = storage.CreateSession("S01", new DateTime(2026, 10, 11, 9, 30, 0, DateTimeKind.Utc));
            var sidecar = BakeoffSidecar.Build("sb_en_001_S01_t1", session, phrase, participant,
                new BakeoffEnvironment("Pixel 8", "Android 15", "0.1.0", "6000.3.26f1"), null, processed, ProcessorConfig.Default,
                new DateTime(2026, 10, 11, 9, 31, 0, DateTimeKind.Utc));

            storage.WriteTake(session, "sb_en_001_S01_t1", processed.ScorerInput, processed.RawAttempt, sidecar);

            Assert.That(session, Is.EqualTo("20261011T093000Z_S01"));
            var folder = storage.SessionFolder(session);
            Assert.That(Directory.GetFiles(folder).Select(Path.GetFileName).OrderBy(n => n),
                Is.EqualTo(new[] { "sb_en_001_S01_t1.flipped16k.wav", "sb_en_001_S01_t1.json", "sb_en_001_S01_t1.raw.wav" }));
            var flipped = WavEncoder.DecodePcm16(File.ReadAllBytes(Path.Combine(folder, "sb_en_001_S01_t1.flipped16k.wav")));
            Assert.That(flipped.SampleRate, Is.EqualTo(16000));
            Assert.That(flipped.Channels, Is.EqualTo(1));

            storage.DeleteSession(session);
            Assert.That(Directory.Exists(folder), Is.False);
        }

        [Test]
        public void Sidecar_IsValidJson_WithRequiredFields_AndNoPersonalFields()
        {
            var participant = BakeoffParticipant.TryCreate("S02", 7, 2, "C-9", true, out _);
            var json = BakeoffSidecar.Build("sb_en_002_S02_t1", "20261011T093000Z_S02",
                new PhraseInfo("sb_en_002", "Tiny dragon", new[] { "t" }, 1, "sbps-v1"), participant,
                new BakeoffEnvironment("Model \"X\"", "Android", "0.1.0", "6000.3.26f1"), null, ProcessedTake(), ProcessorConfig.Default,
                DateTime.UtcNow);

            var root = MiniJson.ParseObject(json);

            Assert.That(JsonValues.String(root, "schema"), Is.EqualTo(BakeoffSidecar.Schema));
            Assert.That(JsonValues.String(root, "phraseId"), Is.EqualTo("sb_en_002"));
            Assert.That(JsonValues.String(root, "speakerCode"), Is.EqualTo("S02"));
            Assert.That(JsonValues.String(root, "consentRef"), Is.EqualTo("C-9"));
            Assert.That(JsonValues.String(JsonValues.Object(root, "groupLabels"), "accent"), Is.EqualTo("Non-UK"));
            Assert.That(JsonValues.String(JsonValues.Object(root, "groupLabels"), "voice"), Is.EqualTo("lower"));
            Assert.That(JsonValues.String(root, "deviceModel"), Is.EqualTo("Model \"X\""), "escaped correctly");
            Assert.That(JsonValues.Int(JsonValues.Object(root, "durations"), "scorerInputRate"), Is.EqualTo(16000));
            Assert.That(JsonValues.Has(root, "utc"), Is.True);
            Assert.That(JsonValues.Has(root, "appVersion"), Is.True);
            foreach (var forbidden in new[] { "name", "email", "phone", "deviceName", "notes", "transcript" })
            {
                StringAssert.DoesNotContain("\"" + forbidden + "\"", json);
            }
        }

        [Test]
        public void PullCommands_ExternalStorage_UsesSdcardPath()
        {
            var storage = new BakeoffStorage("/storage/emulated/0/Android/data/com.uprisestudios.speakback/files");

            var commands = storage.PullCommands("20261011T093000Z_S01", "com.uprisestudios.speakback");

            Assert.That(commands, Is.EqualTo(new[]
            {
                "adb pull \"/sdcard/Android/data/com.uprisestudios.speakback/files/bakeoff/20261011T093000Z_S01\" .",
            }));
        }

        [Test]
        public void PullCommands_InternalStorage_OffersRunAsFallback()
        {
            var storage = new BakeoffStorage("/data/user/0/com.uprisestudios.speakback/files");

            var commands = storage.PullCommands("S", "com.uprisestudios.speakback");

            Assert.That(commands.Length, Is.EqualTo(2));
            Assert.That(commands[1], Is.EqualTo("adb exec-out run-as com.uprisestudios.speakback tar c \"files/bakeoff/S\" > S.tar"));
        }

        [Test]
        public void DeleteAll_RemovesSessionsAndReferences()
        {
            var storage = new BakeoffStorage(_root);
            storage.SaveReference("sb_en_001", AudioBuffer.Create(new[] { 0.1f }, 16000, 1));
            storage.CreateSession("S01", DateTime.UtcNow);

            storage.DeleteAll();

            Assert.That(Directory.Exists(storage.SessionsRoot), Is.False);
            Assert.That(Directory.Exists(storage.ReferencesRoot), Is.False);
        }

        private static AudioProcessResult ProcessedTake()
        {
            var rate = 48000;
            var samples = new float[(int)(1.0 * rate)];
            for (var i = (int)(0.2 * rate); i < (int)(0.8 * rate); i++) samples[i] = 0.3f * (float)Math.Sin(2 * Math.PI * 220 * i / rate);
            var result = AudioProcessor.Process(AudioBuffer.Create(samples, rate, 1), ProcessorConfig.Default);
            Assert.That(result.Status, Is.EqualTo(AudioProcessStatus.Ok));
            return result;
        }
    }
}
