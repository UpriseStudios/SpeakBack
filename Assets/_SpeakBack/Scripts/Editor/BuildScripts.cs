using System;
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
    /// Command-line Android builds (-executeMethod). In batch mode a failure exits with code 1 so
    /// scripts and CI see it; in the editor the exception surfaces normally.
    /// </summary>
    public static class BuildScripts
    {
        public const string GameScene = "Assets/_SpeakBack/Scenes/03_Game.unity";
        public const string DevRecorderScene = "Assets/_SpeakBack/Scenes/Dev/BakeoffRecorder.unity";
        public const string DevDefine = "SPEAKBACK_DEV";

        private const string OutputDirectory = "Builds/Android";

        [MenuItem("SpeakBack/Build/Android Dev APK")]
        public static void BuildAndroidDev() => RunGuarded(() => Build(development: true));

        [MenuItem("SpeakBack/Build/Android Release APK")]
        public static void BuildAndroidRelease() => RunGuarded(() => Build(development: false));

        private static void Build(bool development)
        {
            var problems = ProjectConfigurator.Verify();
            if (problems.Count > 0)
            {
                throw new BuildFailedException(
                    "Project settings drifted (run SpeakBack/Apply Project Settings):\n - " + string.Join("\n - ", problems));
            }

            DataSync.Sync();

            var scenes = ReleaseScenes();
            var leaks = DevScriptsReferencedBy(scenes);
            if (leaks.Count > 0)
            {
                throw new BuildFailedException("Release scenes reference dev-only scripts: " + string.Join(", ", leaks));
            }

            if (development)
            {
                // The recorder opens first in dev builds; it has a button to the game scene.
                scenes.Insert(0, DevRecorderScene);
            }

            foreach (var scene in scenes.Where(s => !File.Exists(s)))
            {
                throw new BuildFailedException($"Scene missing: {scene}");
            }

            Directory.CreateDirectory(OutputDirectory);
            var output = Path.Combine(OutputDirectory, development ? "SpeakBack-dev.apk" : "SpeakBack.apk");

            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = output,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = development ? BuildOptions.Development : BuildOptions.None,
                extraScriptingDefines = development ? new[] { DevDefine } : Array.Empty<string>(),
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Android {(development ? "dev" : "release")} build {summary.result} with {summary.totalErrors} error(s).");
            }

            var permissions = ReadApkPermissions(output);
            var contents = ApkContents.Read(output);
            Debug.Log($"[SpeakBack] Built {output}: {new FileInfo(output).Length / (1024.0 * 1024.0):F1} MB APK, " +
                      $"{summary.totalTime.TotalSeconds:F0} s, scenes: {string.Join(", ", scenes)}, " +
                      $"packaged scenes: {contents.SceneCount}, dev code shipped: {contents.ContainsDevCode}, " +
                      $"permissions: [{string.Join(", ", permissions)}]");

            // Check the artefact itself, not just the inputs.
            if (contents.SceneCount != scenes.Count)
            {
                throw new BuildFailedException($"APK contains {contents.SceneCount} scene(s), expected {scenes.Count}.");
            }

            if (development) return;

            if (permissions.Contains(InternetPermissionStripper.InternetPermission))
            {
                throw new BuildFailedException("Release APK requests INTERNET; Phase 1 must be offline (Phase 1 §15).");
            }

            if (contents.ContainsDevCode)
            {
                throw new BuildFailedException("Release APK contains SpeakBack.Dev code (found in IL2CPP metadata).");
            }
        }

        /// <summary>Dev-only scripts (under Scripts/Dev) that any of <paramref name="scenePaths"/> depend on, directly or through prefabs.</summary>
        public static IReadOnlyList<string> DevScriptsReferencedBy(IEnumerable<string> scenePaths) =>
            AssetDatabase.GetDependencies(scenePaths.ToArray(), true)
                .Where(path => path.StartsWith("Assets/_SpeakBack/Scripts/Dev/", StringComparison.Ordinal) && path.EndsWith(".cs", StringComparison.Ordinal))
                .Distinct()
                .ToList();

        /// <summary>Permissions declared by the built APK, read with aapt2 from the editor's bundled Android SDK.</summary>
        private static List<string> ReadApkPermissions(string apkPath)
        {
            var aapt2 = FindAapt2();
            if (aapt2 == null)
            {
                throw new BuildFailedException("aapt2 not found; cannot verify APK permissions.");
            }

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo(aapt2, $"dump permissions \"{apkPath}\"")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };
            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new BuildFailedException($"aapt2 failed ({process.ExitCode}): {stderr}");
            }

            const string prefix = "uses-permission: name='";
            return stdout.Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.StartsWith(prefix, StringComparison.Ordinal))
                .Select(line => line.Substring(prefix.Length).TrimEnd('\''))
                .ToList();
        }

        private static string FindAapt2()
        {
            var sdkRoots = new[]
            {
                Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "SDK"),
                Environment.GetEnvironmentVariable("ANDROID_HOME"),
                Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
            };
            var exe = Application.platform == RuntimePlatform.WindowsEditor ? "aapt2.exe" : "aapt2";

            return sdkRoots
                .Where(root => !string.IsNullOrEmpty(root) && Directory.Exists(Path.Combine(root, "build-tools")))
                .SelectMany(root => Directory.GetDirectories(Path.Combine(root, "build-tools")))
                .OrderByDescending(dir => dir, StringComparer.Ordinal)
                .Select(dir => Path.Combine(dir, exe))
                .FirstOrDefault(File.Exists);
        }

        /// <summary>Enabled scenes from Build Settings; refuses to proceed if a dev-only scene has leaked in.</summary>
        private static List<string> ReleaseScenes()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
            if (scenes.Contains(DevRecorderScene))
            {
                throw new BuildFailedException($"{DevRecorderScene} is in Build Settings; it is added only by BuildAndroidDev.");
            }

            if (!scenes.Contains(GameScene))
            {
                throw new BuildFailedException($"{GameScene} is not in Build Settings.");
            }

            return scenes;
        }

        private static void RunGuarded(Action build)
        {
            try
            {
                build();
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SpeakBack] Build failed: {e.Message}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                throw;
            }
        }
    }
}
