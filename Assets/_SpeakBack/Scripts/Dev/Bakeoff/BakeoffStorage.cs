using System;
using System.Globalization;
using System.IO;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Dev.Bakeoff
{
    /// <summary>
    /// Bake-off files on the phone (dev builds only; D-016). Sessions go to
    /// <c>&lt;persistentDataPath&gt;/bakeoff/&lt;sessionId&gt;/</c> and operator reference recordings to
    /// <c>&lt;persistentDataPath&gt;/bakeoff_refs/</c>. Everything is meant to be pulled to a computer outside the
    /// repo and then deleted from the phone.
    /// </summary>
    public sealed class BakeoffStorage
    {
        /// <summary>Uses <paramref name="root"/> (Application.persistentDataPath) as the base folder.</summary>
        public BakeoffStorage(string root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
        }

        /// <summary>Base folder.</summary>
        public string Root { get; }

        /// <summary>Folder holding all sessions.</summary>
        public string SessionsRoot => Path.Combine(Root, "bakeoff");

        /// <summary>Folder holding operator reference recordings.</summary>
        public string ReferencesRoot => Path.Combine(Root, "bakeoff_refs");

        /// <summary>Forward reference WAV path for a phrase.</summary>
        public string ForwardReferencePath(string phraseId) => Path.Combine(ReferencesRoot, phraseId + ".forward.wav");

        /// <summary>Reversed reference WAV path for a phrase.</summary>
        public string ReversedReferencePath(string phraseId) => Path.Combine(ReferencesRoot, phraseId + ".reversed.wav");

        /// <summary>Whether a reversed reference exists, which is what participants hear.</summary>
        public bool HasReference(string phraseId) => File.Exists(ReversedReferencePath(phraseId));

        /// <summary>Saves the operator's forward recording and its frame-correct reversal.</summary>
        public void SaveReference(string phraseId, AudioBuffer forward)
        {
            Directory.CreateDirectory(ReferencesRoot);
            File.WriteAllBytes(ForwardReferencePath(phraseId), WavEncoder.EncodePcm16(forward));
            File.WriteAllBytes(ReversedReferencePath(phraseId), WavEncoder.EncodePcm16(AudioReverser.ReverseFrames(forward)));
        }

        /// <summary>Loads the reversed reference.</summary>
        public AudioBuffer LoadReversedReference(string phraseId) => WavEncoder.DecodePcm16(File.ReadAllBytes(ReversedReferencePath(phraseId)));

        /// <summary>Loads the forward reference.</summary>
        public AudioBuffer LoadForwardReference(string phraseId) => WavEncoder.DecodePcm16(File.ReadAllBytes(ForwardReferencePath(phraseId)));

        /// <summary>Creates a session folder named by UTC time and speaker code; returns its ID.</summary>
        public string CreateSession(string speakerCode, DateTime utcNow)
        {
            var id = utcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture) + "_" + speakerCode;
            Directory.CreateDirectory(SessionFolder(id));
            return id;
        }

        /// <summary>Folder for <paramref name="sessionId"/>.</summary>
        public string SessionFolder(string sessionId) => Path.Combine(SessionsRoot, sessionId);

        /// <summary>
        /// Writes one kept take: <c>&lt;take&gt;.flipped16k.wav</c> (ScorerInput), <c>&lt;take&gt;.raw.wav</c>
        /// (RawAttempt) and <c>&lt;take&gt;.json</c>. The sidecar is written last, so a complete sidecar means
        /// complete audio.
        /// </summary>
        public void WriteTake(string sessionId, string takeId, AudioBuffer scorerInput, AudioBuffer rawAttempt, string sidecarJson)
        {
            var folder = SessionFolder(sessionId);
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, takeId + ".flipped16k.wav"), WavEncoder.EncodePcm16(scorerInput));
            File.WriteAllBytes(Path.Combine(folder, takeId + ".raw.wav"), WavEncoder.EncodePcm16(rawAttempt));
            File.WriteAllText(Path.Combine(folder, takeId + ".json"), sidecarJson);
        }

        /// <summary>Deletes one session folder.</summary>
        public void DeleteSession(string sessionId)
        {
            var folder = SessionFolder(sessionId);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        /// <summary>Deletes every session and every reference recording on this phone.</summary>
        public void DeleteAll()
        {
            if (Directory.Exists(SessionsRoot)) Directory.Delete(SessionsRoot, true);
            if (Directory.Exists(ReferencesRoot)) Directory.Delete(ReferencesRoot, true);
        }

        /// <summary>
        /// adb commands to copy a session to a computer. App-specific external storage
        /// (/storage/emulated/0/Android/data/…) can be pulled directly. Internal storage needs run-as, which
        /// works because dev builds are debuggable (P00 finding C9).
        /// </summary>
        public string[] PullCommands(string sessionId, string packageName)
        {
            var folder = SessionFolder(sessionId).Replace('\\', '/');
            const string emulated = "/storage/emulated/0/";
            if (folder.StartsWith(emulated, StringComparison.Ordinal))
            {
                return new[] { $"adb pull \"/sdcard/{folder.Substring(emulated.Length)}\" ." };
            }

            var marker = "/files/";
            var index = folder.IndexOf(marker, StringComparison.Ordinal);
            var relative = index >= 0 ? folder.Substring(index + 1) : folder;
            return new[]
            {
                $"adb pull \"{folder}\" .",
                $"adb exec-out run-as {packageName} tar c \"{relative}\" > {sessionId}.tar",
            };
        }
    }
}
