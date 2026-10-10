# SpeakBack

A mobile voice game by Uprise Studios / J M Games. You hear a phrase played backwards and copy it. The game flips your recording so you hear it forwards, then scores it. Uprise hosts.

Current stage: **Phase 1 (Prove)**. Read `CLAUDE.md` first, then `docs/DECISIONS.md` and `docs/SPEAKBACK_PHASE_1_BUILD_SPEC.md`.

## Requirements

- Unity **6000.3.26f1** (Unity 6.3 LTS) with Android Build Support (SDK, NDK, OpenJDK). See `docs/dependency-decisions.md`.
- `UNITY_PATH` set to the editor binary, e.g. `C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe`.
- .NET SDK with the .NET 8 runtime, for the fast test loop.
- Git LFS (`git lfs install`): audio, models, fonts and character art are stored in LFS (D-019).

## Common commands

```bash
# Fast pure-logic tests (SpeakBack.Core)
dotnet test Tools/DotnetTests

# Unity EditMode / PlayMode tests
"$UNITY_PATH" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
"$UNITY_PATH" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log

# Android builds → Builds/Android/
"$UNITY_PATH" -batchmode -quit -projectPath . -executeMethod Uprise.SpeakBack.EditorTools.BuildScripts.BuildAndroidDev -logFile Logs/build.log
"$UNITY_PATH" -batchmode -quit -projectPath . -executeMethod Uprise.SpeakBack.EditorTools.BuildScripts.BuildAndroidRelease -logFile Logs/build-release.log

# Install the dev build on a connected phone
adb install -r Builds/Android/SpeakBack-dev.apk
```

The editor menu **SpeakBack/** contains **Apply Project Settings**, **Rebuild Scenes**, **Rebuild Font Assets** and the two build commands. Builds refuse to run if the project settings have drifted from the Phase 1 values.

## Layout

| Path | Contents |
|---|---|
| `Assets/_SpeakBack/Scripts/Core` | `SpeakBack.Core`: pure C#, no UnityEngine (`noEngineReferences`) |
| `Assets/_SpeakBack/Scripts/Runtime` | `SpeakBack.Runtime`: MonoBehaviours, audio, UI |
| `Assets/_SpeakBack/Scripts/Dev` | `SpeakBack.Dev`: compiled only with `SPEAKBACK_DEV` (dev builds) |
| `Assets/_SpeakBack/Scripts/Editor` | `SpeakBack.EditorTools`: settings, scenes, builds |
| `Assets/_SpeakBack/Tests` | EditMode (Core) and PlayMode (Runtime) tests |
| `Tools/DotnetTests` | `dotnet test` project that compiles the Core sources and tests |
| `data/` | Source-of-truth content: phrases, SBPS phone set, golden vectors, dialogue |
| `docs/` | Specs, decisions, reports (`docs/reports/PNN-report.md`) |
| `prompts/`, `checklists/` | Claude Code prompt pack and human-step checklists |

## Privacy

The game is offline. It makes no network calls, has no analytics or accounts, and requests no INTERNET permission in release builds. Round audio stays in memory only. Bake-off recordings are never committed.
