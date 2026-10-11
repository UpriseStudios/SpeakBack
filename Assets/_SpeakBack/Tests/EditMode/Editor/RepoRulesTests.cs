using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using Uprise.SpeakBack.Core.Content;
using Uprise.SpeakBack.EditorTools;

namespace Uprise.SpeakBack.EditorTools.Tests
{
    /// <summary>
    /// Source and project rules from CLAUDE.md and Phase 1 §8/§15 that the compiler can't enforce:
    /// one microphone owner, no file IO or audio logging in shipped code, dev code isolated from release,
    /// and data copies identical to data/.
    /// </summary>
    public sealed class RepoRulesTests
    {
        private const string ScriptsRoot = "Assets/_SpeakBack/Scripts";

        private static string CodeWithoutComments(string path) =>
            string.Join("\n", File.ReadAllLines(path).Select(line =>
            {
                var comment = line.IndexOf("//", System.StringComparison.Ordinal);
                return comment >= 0 ? line.Substring(0, comment) : line;
            }));

        private static string[] Scripts(string folder) =>
            Directory.GetFiles(Path.Combine(ScriptsRoot, folder), "*.cs", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .ToArray();

        [Test]
        public void OnlyUnityMicrophoneBackend_UsesMicrophoneApi()
        {
            var pattern = new Regex(@"\bMicrophone\s*\.");
            var users = Scripts(string.Empty).Where(p => pattern.IsMatch(CodeWithoutComments(p))).ToList();

            Assert.That(users, Is.EqualTo(new[] { ScriptsRoot + "/Runtime/Audio/UnityMicrophoneBackend.cs" }));
        }

        [Test]
        public void CoreAndRuntime_DoNoFileIo()
        {
            // Player builds never write audio, transcripts or phonemes (Phase 1 §15). Dev tooling may.
            var pattern = new Regex(@"\b(File|Directory|FileStream|StreamWriter|BinaryWriter|PlayerPrefs)\s*\.|new\s+(FileStream|StreamWriter|BinaryWriter)\b");
            var offenders = Scripts("Core").Concat(Scripts("Runtime"))
                .Where(p => pattern.IsMatch(CodeWithoutComments(p)))
                .ToList();

            Assert.That(offenders, Is.Empty);
        }

        [Test]
        public void CaptureAndAudioCode_NeverLogs()
        {
            var pattern = new Regex(@"\bDebug\s*\.\s*Log");
            var offenders = Scripts("Runtime/Audio").Concat(Scripts("Core"))
                .Where(p => pattern.IsMatch(CodeWithoutComments(p)))
                .ToList();

            Assert.That(offenders, Is.Empty);
        }

        [Test]
        public void DevAssembly_CompilesOnlyInEditorOrDevBuilds()
        {
            var asmdef = MiniJson.ParseObject(File.ReadAllText(ScriptsRoot + "/Dev/SpeakBack.Dev.asmdef"));

            Assert.That(JsonValues.StringArray(asmdef, "defineConstraints"), Is.EqualTo(new[] { "UNITY_EDITOR || SPEAKBACK_DEV" }));
        }

        [Test]
        public void ReleaseScenes_ReferenceNoDevScripts()
        {
            var releaseScenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

            Assert.That(releaseScenes, Is.EqualTo(new[] { BuildScripts.GameScene }));
            Assert.That(BuildScripts.DevScriptsReferencedBy(releaseScenes), Is.Empty);
        }

        [Test]
        public void DevRecorderScene_IsDetectedAsDevOnly()
        {
            // Positive control for the check above.
            Assert.That(BuildScripts.DevScriptsReferencedBy(new[] { BuildScripts.DevRecorderScene }),
                Has.Some.EndsWith("/Dev/Bakeoff/BakeoffRecorderApp.cs"));
        }

        [Test]
        public void ProjectSettings_MatchPhase1Values()
        {
            Assert.That(ProjectConfigurator.Verify(), Is.Empty);
        }

        [Test]
        public void DataCopies_AreByteIdenticalToDataFolder()
        {
            Assert.That(DataSync.OutOfSync(), Is.Empty, "run SpeakBack/Sync Data");
        }
    }
}
