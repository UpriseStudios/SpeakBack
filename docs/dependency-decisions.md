# Dependency Decisions

Pinned engine, packages, toolchain and platform targets. **Status:** P01 scaffold. The scorer runtime and model are added at P06 after the bake-off. D-001 remains provisional until the P06 feasibility check.

## Editor

| Item | Value | Notes |
|---|---|---|
| Unity editor | **6000.3.26f1** (revision `de8167e7440c`) | Unity 6.3 LTS line (D-001; Jamie switched from the non-LTS 6000.6.4f1 after P00). |
| Template | Universal 2D (`com.unity.template.2d-cross-platform-2d` 6.1.6, bundled with the editor) | Project rebuilt in place at P01. The previous project used the URP 3D blank template. |
| C# language version | 9.0 | From Unity's compiler response files (`-langversion:9.0`). `Tools/DotnetTests` pins the same value. |
| Enter Play Mode options | Domain reload **disabled** (template default) | Fine while there are no static singletons (CLAUDE.md). Revisit if a test depends on static reset. |

## Packages (`Packages/manifest.json` → `packages-lock.json`)

| Package | Version | Source | Depth | Why |
|---|---|---|---|---|
| com.unity.ugui | 2.0.0 | builtin | 0 | uGUI Canvas + TextMeshPro (Phase 1 §2) |
| com.unity.inputsystem | 1.20.0 | registry | 0 | Active input handling = Input System only; `InputSystemUIInputModule` on the EventSystem |
| com.unity.render-pipelines.universal | 17.3.0 | builtin | 0 | URP 2D renderer from the template |
| com.unity.2d.sprite | 1.0.0 | builtin | 0 | Sprite import for Uprise expressions |
| com.unity.test-framework | 1.6.0 | builtin | 0 | EditMode/PlayMode tests (authoritative runner) |
| com.unity.ide.rider | 3.0.38 | registry | 0 | IDE integration (editor only) |
| com.unity.ide.visualstudio | 2.0.26 | registry | 0 | IDE integration (editor only) |
| com.unity.render-pipelines.core | 17.3.0 | builtin | 1 | URP dependency |
| com.unity.render-pipelines.universal-config | 17.0.3 | builtin | 1 | URP dependency |
| com.unity.shadergraph | 17.3.0 | builtin | 1 | URP dependency |
| com.unity.ext.nunit | 2.0.5 | builtin | 1 | NUnit 3.5 for Unity tests |
| com.unity.burst | 1.8.30 | registry | 2 | Transitive (collections) |
| com.unity.collections | 2.6.8 | registry | 2 | Transitive |
| com.unity.mathematics | 1.3.3 | registry | 2 | Transitive |
| com.unity.searcher | 4.9.5 | registry | 2 | Transitive (shadergraph, editor) |
| com.unity.nuget.mono-cecil | 1.11.6 | registry | 3 | Transitive |
| com.unity.test-framework.performance | 3.5.0 | registry | 3 | Transitive |

Built-in modules enabled: accessibility, androidjni, animation, audio, imageconversion, imgui, jsonserialize, ui, uielements (direct), plus hierarchycore, physics and terrain (pulled in by dependencies).

**Removed from the template at P01 (P00 finding C11):** com.unity.collab-proxy (Unity Version Control, networked editor tooling), com.unity.visualscripting, com.unity.timeline, the 2D animation, aseprite, PSD importer, sprite shape, tilemap and tooling packages, and the `unityanalytics` and `unitywebrequest*` modules. Phase 1 needs none of them, and removing the analytics and web-request modules keeps the offline guarantee easy to audit. Re-add a package only with a reason recorded here.

## Android toolchain (bundled with the editor's Android Build Support)

| Item | Version |
|---|---|
| Android SDK platforms | android-34, android-35, android-36, android-37.0 |
| Build-tools | 36.0.0 |
| Platform-tools | 37.0.1 |
| Command-line tools | 16.0 |
| NDK | 27.2.12479018 |
| OpenJDK | 17.0.18 |
| Gradle (bundled launcher) | 9.3.1 |

## Platform targets

| Setting | Value | Source |
|---|---|---|
| Minimum API | 26 (Android 8.0) | D-002 |
| Target API | **36** (Android 16), set explicitly | Google Play: "Starting August 31 2026: New apps and app updates must target Android 16 (API level 36) or higher" (developer.android.com/google/play/requirements/target-sdk, page dated 2026-10-01, checked 2026-10-10) |
| Scripting backend | IL2CPP | Phase 1 §2 |
| Architectures | ARM64 only | Phase 1 §2 |
| Application ID | `com.uprisestudios.speakback` | D-003 (provisional until the first store upload) |
| Output | APK (`buildAppBundle = false`) | Dev and test installs. An AAB is needed for the store later. |

## Fonts

Baloo 2 and Nunito variable TTFs from google/fonts (OFL). TMP dynamic font assets are generated from them. Unity's font importer uses each variable font's **default instance** (weight 400), so heavier display weights need static instances (e.g. Baloo 2 ExtraBold from github.com/EkType/Baloo2) when P09 styles the UI. See `ThirdPartyNotices.md`.

## To be added later

- Scorer runtime, model, versions, licence, size and 16 KB page-size compatibility (P05/P06; P00 finding B9).
- Release signing keystore (kept outside the repo).
