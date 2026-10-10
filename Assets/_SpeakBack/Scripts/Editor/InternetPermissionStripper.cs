using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Uprise.SpeakBack.EditorTools
{
    /// <summary>
    /// Removes android.permission.INTERNET from non-development Android builds (Phase 1 §15, CLAUDE.md
    /// non-negotiables). Unity's "Internet Access: Auto" adds it whenever a stripped assembly references
    /// UnityEngine.Networking, which Unity.InputSystem and Unity.RenderPipelines.Core.Runtime both do, so
    /// the setting alone cannot keep it out. Development builds keep it for the profiler and debugger.
    /// <see cref="BuildScripts"/> verifies the finished release APK independently.
    /// </summary>
    public sealed class InternetPermissionStripper : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        public const string InternetPermission = "android.permission.INTERNET";

        private static readonly XNamespace Android = "http://schemas.android.com/apk/res/android";
        private static readonly XNamespace Tools = "http://schemas.android.com/tools";

        // Build callbacks are separate invocations; this carries one build's options from the
        // pre-process step to the Gradle step. It is reset at the start of every build.
        private static bool s_stripForCurrentBuild;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_stripForCurrentBuild = report.summary.platform == BuildTarget.Android &&
                                     (report.summary.options & BuildOptions.Development) == 0;
        }

        public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
        {
            if (!s_stripForCurrentBuild) return;

            var manifestPath = Path.Combine(unityLibraryPath, "src", "main", "AndroidManifest.xml");
            var document = XDocument.Load(manifestPath);
            var manifest = document.Root;

            manifest.Elements("uses-permission")
                .Where(e => (string)e.Attribute(Android + "name") == InternetPermission)
                .Remove();

            // A remove marker stops any AAR merged beneath unityLibrary from adding it back.
            if (manifest.Attribute(XNamespace.Xmlns + "tools") == null)
            {
                manifest.Add(new XAttribute(XNamespace.Xmlns + "tools", Tools.NamespaceName));
            }

            manifest.AddFirst(new XElement("uses-permission",
                new XAttribute(Android + "name", InternetPermission),
                new XAttribute(Tools + "node", "remove")));

            document.Save(manifestPath);
            Debug.Log("[SpeakBack] Removed INTERNET permission from the release manifest.");
        }
    }
}
