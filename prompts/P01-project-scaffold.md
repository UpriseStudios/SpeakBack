# P01 — Unity project scaffold

**Stage:** Phase 1 / U1 · **Depends on:** P00 answers · **Spec:** Phase 1 §2, §3, §4; D-001–D-005, D-015, D-018, D-019

## Prompt

```
Read CLAUDE.md, docs/DECISIONS.md, docs/reports/P00-report.md (including Jamie's answers if added) and Phase 1 Build Spec §2–§4.

Goal: an empty but correctly structured Unity 6 LTS project that builds a development APK, with the assembly layout, test harnesses and repo hygiene the rest of Phase 1 depends on. No gameplay yet.

Tasks
1. Create the Unity project at the repo root (Universal 2D template, Unity 6 LTS from $UNITY_PATH). If a project already exists, inspect it instead and report.
2. Repo hygiene: Unity .gitignore; .gitattributes with Git LFS for *.wav *.ogg *.mp3 *.onnx *.bin *.gguf *.psd *.png under Art/ (not UI icons <100 KB if you judge it better; explain), and force text serialisation + visible meta files.
3. Folder layout exactly as Phase 1 §3. Assemblies:
   - SpeakBack.Core (Scripts/Core, noEngineReferences: true, no references)
   - SpeakBack.Runtime (Scripts/Runtime → Core, Unity.TextMeshPro)
   - SpeakBack.Dev (Scripts/Dev → Core, Runtime; defineConstraints: SPEAKBACK_DEV)
   - SpeakBack.EditorTools (Editor only)
   - SpeakBack.Core.Tests (EditMode, → Core)
   - SpeakBack.Runtime.Tests (PlayMode, → Runtime, Core)
4. Tools/DotnetTests: a net8.0 xUnit (or NUnit, match Unity's NUnit if simpler) project that links ../../Assets/_SpeakBack/Scripts/Core/**/*.cs and a shared test folder so the same Core tests run under `dotnet test`. Add one trivial Core type + test to prove both runners work.
5. Project settings (record each value in the report): company "Uprise Studios", product "SpeakBack", application ID per D-003; portrait only; IL2CPP; ARM64 only; min API 26; target API = current Google Play requirement (state your source; if you cannot check, leave Unity's "Automatic (highest installed)" and flag it); Microphone usage description; no INTERNET permission (verify the merged manifest after a build and report what permissions it contains).
6. Editor build script Uprise.SpeakBack.EditorTools.BuildScripts with BuildAndroidDev (development build, SPEAKBACK_DEV define, output Builds/Android/SpeakBack-dev.apk) and BuildAndroidRelease (no SPEAKBACK_DEV). Both callable via -executeMethod and failing with non-zero exit on errors.
7. Scenes: Assets/_SpeakBack/Scenes/03_Game.unity (camera, Canvas with SafeAreaRoot, EventSystem, a "SpeakBack" title label) and Scenes/Dev/BakeoffRecorder.unity (empty placeholder). Only 03_Game in release build settings; the dev scene is added only by BuildAndroidDev.
8. Fonts: add Baloo 2 and Nunito (OFL) as TextMeshPro font assets ONLY if you can obtain the font files offline in this environment; otherwise leave TMP defaults and add a TODO in the report for Jamie to drop the .ttf files in Assets/_SpeakBack/Art/UI/Fonts/.
9. Create ThirdPartyNotices.md (TextMeshPro/Unity packages note, fonts if added) and docs/dependency-decisions.md (editor version, packages + versions from packages-lock.json, Android SDK/NDK/JDK versions, target/min API).
10. Copy data/ into the repo if not already present (it should be) and leave it untouched.

Verify
- dotnet test Tools/DotnetTests passes.
- Unity EditMode tests pass in batchmode.
- BuildAndroidDev produces an APK. If the environment cannot build Android, say exactly why and mark UNVERIFIED.
- Report the merged AndroidManifest permissions.

Finish: docs/reports/P01-report.md, commit "P01: Unity project scaffold", stop.
```

## Done when

- [ ] You can open the project in Unity and press Play on `03_Game` without errors.
- [ ] `SpeakBack-dev.apk` installs on your phone and shows the title.
- [ ] Manifest permissions include RECORD_AUDIO (or will once the mic is used) and **not** INTERNET.
- [ ] Both test runners pass.
