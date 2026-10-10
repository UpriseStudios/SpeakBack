using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Uprise.SpeakBack.EditorTools
{
    /// <summary>
    /// Single source for the Phase 1 player settings (Phase 1 §2, D-002, D-003). <see cref="Apply"/> is
    /// idempotent; <see cref="Verify"/> lets the build scripts refuse to build from drifted settings.
    /// </summary>
    public static class ProjectConfigurator
    {
        public const string CompanyName = "Uprise Studios";
        public const string ProductName = "SpeakBack";
        public const string ApplicationId = "com.uprisestudios.speakback";
        public const string BundleVersion = "0.1.0";
        public const int MinApi = 26;

        // Google Play: new apps and updates must target API 36 from 31 Aug 2026
        // (developer.android.com/google/play/requirements/target-sdk, checked 2026-10-10).
        public const int TargetApi = 36;

        public const string MicrophoneUsageDescription =
            "SpeakBack listens while you record so it can flip your voice. Your audio stays on this device.";

        [MenuItem("SpeakBack/Apply Project Settings")]
        public static void Apply()
        {
            var android = NamedBuildTarget.Android;

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = BundleVersion;
            PlayerSettings.SetApplicationIdentifier(android, ApplicationId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, ApplicationId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, ApplicationId);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinApi;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)TargetApi;
            PlayerSettings.Android.forceInternetPermission = false;

            // iOS-only key today; set now so the explanation exists when iOS starts (Master §3).
            PlayerSettings.iOS.microphoneUsageDescription = MicrophoneUsageDescription;

            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            EditorUserBuildSettings.buildAppBundle = false;

            AssetDatabase.SaveAssets();
            Debug.Log("[SpeakBack] Project settings applied.");
        }

        /// <summary>Returns a description of every setting that differs from the Phase 1 values; empty when all match.</summary>
        public static IReadOnlyList<string> Verify()
        {
            var android = NamedBuildTarget.Android;
            var problems = new List<string>();

            void Check(bool ok, string what)
            {
                if (!ok) problems.Add(what);
            }

            Check(PlayerSettings.companyName == CompanyName, $"companyName is '{PlayerSettings.companyName}'");
            Check(PlayerSettings.productName == ProductName, $"productName is '{PlayerSettings.productName}'");
            Check(PlayerSettings.GetApplicationIdentifier(android) == ApplicationId,
                $"application ID is '{PlayerSettings.GetApplicationIdentifier(android)}'");
            Check(PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait, "orientation is not Portrait");
            Check(PlayerSettings.GetScriptingBackend(android) == ScriptingImplementation.IL2CPP, "scripting backend is not IL2CPP");
            Check(PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64,
                $"architectures are {PlayerSettings.Android.targetArchitectures}, expected ARM64 only");
            Check((int)PlayerSettings.Android.minSdkVersion == MinApi, $"min API is {(int)PlayerSettings.Android.minSdkVersion}");
            Check((int)PlayerSettings.Android.targetSdkVersion == TargetApi, $"target API is {(int)PlayerSettings.Android.targetSdkVersion}");
            Check(!PlayerSettings.Android.forceInternetPermission, "Internet access is forced on");
            Check(!PlayerSettings.GetScriptingDefineSymbols(android).Contains("SPEAKBACK_DEV"),
                "SPEAKBACK_DEV is in the project defines; it must come only from BuildAndroidDev");
            return problems;
        }
    }
}
