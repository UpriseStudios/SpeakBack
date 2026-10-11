using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Uprise.SpeakBack.EditorTools
{
    /// <summary>
    /// Copies source-of-truth content from <c>data/</c> (outside Assets, so not built) into
    /// <c>Assets/_SpeakBack/Data/Source/</c> as TextAssets (P00 Q8). <c>data/</c> stays authoritative:
    /// the copies are overwritten, never edited. The build scripts sync before building, every build
    /// verifies the copies are byte-identical, and an EditMode test does the same.
    /// </summary>
    public sealed class DataSync : IPreprocessBuildWithReport
    {
        public const string TargetFolder = "Assets/_SpeakBack/Data/Source";

        /// <summary>Repo-relative source → project asset path.</summary>
        public static readonly IReadOnlyDictionary<string, string> Files = new Dictionary<string, string>
        {
            { "data/phrases/phase1_phrases.json", TargetFolder + "/phase1_phrases.json" },
            { "data/phonemes/sbps.json", TargetFolder + "/sbps.json" },
            { "data/dialogue/uprise_phase1.json", TargetFolder + "/uprise_phase1.json" },
        };

        public int callbackOrder => -100;

        /// <summary>Copies any file whose bytes differ; returns the asset paths that changed.</summary>
        [MenuItem("SpeakBack/Sync Data")]
        public static IReadOnlyList<string> Sync()
        {
            Directory.CreateDirectory(TargetFolder);
            var changed = new List<string>();
            foreach (var pair in Files)
            {
                var source = File.ReadAllBytes(pair.Key);
                if (File.Exists(pair.Value) && File.ReadAllBytes(pair.Value).SequenceEqual(source)) continue;
                File.WriteAllBytes(pair.Value, source);
                changed.Add(pair.Value);
            }

            foreach (var path in changed) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (changed.Count > 0) Debug.Log("[SpeakBack] Synced data: " + string.Join(", ", changed));
            return changed;
        }

        /// <summary>Paths whose project copy is missing or differs from <c>data/</c>; empty when in sync.</summary>
        public static IReadOnlyList<string> OutOfSync() =>
            Files.Where(pair => !File.Exists(pair.Value) || !File.ReadAllBytes(pair.Value).SequenceEqual(File.ReadAllBytes(pair.Key)))
                .Select(pair => pair.Key)
                .ToList();

        public void OnPreprocessBuild(BuildReport report)
        {
            var stale = OutOfSync();
            if (stale.Count > 0)
            {
                throw new BuildFailedException("Data copies are out of date (run SpeakBack/Sync Data): " + string.Join(", ", stale));
            }
        }
    }
}
