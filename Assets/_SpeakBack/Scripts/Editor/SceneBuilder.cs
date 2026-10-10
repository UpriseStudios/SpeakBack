using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Uprise.SpeakBack.UI;

namespace Uprise.SpeakBack.EditorTools
{
    /// <summary>
    /// Builds the Phase 1 scenes and font assets from code, so the scene skeleton is reproducible
    /// and reviewable rather than hand-edited YAML. Re-running overwrites the generated scenes.
    /// </summary>
    public static class SceneBuilder
    {
        private const string FontFolder = "Assets/_SpeakBack/Art/UI/Fonts";
        private const string DisplayFontFile = FontFolder + "/Baloo2-Variable.ttf";
        private const string BodyFontFile = FontFolder + "/Nunito-Variable.ttf";
        private const string DisplayFontAsset = FontFolder + "/Baloo2 SDF.asset";
        private const string BodyFontAsset = FontFolder + "/Nunito SDF.asset";
        private const string TmpEssentials = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        private const string TmpSettings = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        // Master §31 palette (D-004).
        private static readonly Color Background = Hex(0x0F151D);
        private static readonly Color Brand = Hex(0xFF7A1A);

        /// <summary>Batch-mode entry: settings, TMP essentials, fonts, then both scenes.</summary>
        public static void SetUpProject()
        {
            ProjectConfigurator.Apply();
            EnsureTmpEssentials();
            EnsureFontAssets();
            BuildScenes();
        }

        [MenuItem("SpeakBack/Rebuild Scenes")]
        public static void BuildScenes()
        {
            BuildGameScene();
            BuildBakeoffRecorderPlaceholder();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BuildScripts.GameScene, true) };
            AssetDatabase.SaveAssets();
        }

        // ImportPackage is asynchronous in batch mode, so the essentials are committed to the repo
        // (extracted from the ugui package) rather than imported here.
        private static void EnsureTmpEssentials()
        {
            if (!File.Exists(TmpSettings))
            {
                throw new FileNotFoundException(
                    $"TMP essentials missing ({TmpSettings}). In the editor use Window > TextMeshPro > Import TMP Essential Resources " +
                    $"(source: {TmpEssentials}).");
            }
        }

        [MenuItem("SpeakBack/Rebuild Font Assets")]
        public static void EnsureFontAssets()
        {
            CreateDynamicFontAsset(DisplayFontFile, DisplayFontAsset);
            CreateDynamicFontAsset(BodyFontFile, BodyFontAsset);
        }

        private static void CreateDynamicFontAsset(string fontPath, string assetPath)
        {
            if (File.Exists(assetPath)) return;

            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.LogWarning($"[SpeakBack] Font file missing, using TMP default: {fontPath}");
                return;
            }

            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);

            // A dynamic font asset keeps its atlas and material as sub-assets.
            asset.atlasTextures[0].name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.SaveAssets();
        }

        private static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var safeArea = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(SafeAreaFitter));
            safeArea.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)safeArea.transform);

            var title = new GameObject("TitleLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            title.transform.SetParent(safeArea.transform, false);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -64f);
            titleRect.sizeDelta = new Vector2(0f, 160f);
            var text = title.GetComponent<TextMeshProUGUI>();
            text.text = "SpeakBack";
            text.fontSize = 120f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Brand;
            var display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontAsset);
            if (display != null) text.font = display;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            EditorSceneManager.SaveScene(scene, BuildScripts.GameScene);
        }

        private static void BuildBakeoffRecorderPlaceholder()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            EditorSceneManager.SaveScene(scene, BuildScripts.DevRecorderScene);
        }

        private static void CreateCamera()
        {
            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var camera = cameraGo.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
