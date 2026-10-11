# P04 Report — Capture service and Bake-off Recorder

**Date:** 2026-10-11 · **Commit:** the commit titled "P04: capture service and bake-off recorder" (see `git log`) · **Prompt:** prompts/P04-capture-and-recorder.md

## Summary
The game now has its single microphone path:
- `MicrophoneRecorder`, which hosts `MicrophoneCapture` (the §8 rules) over `UnityMicrophoneBackend`, the only code that touches `Microphone.*`;
- a permission service with Android "don't ask again" handling and an app-settings route;
- a hold / tap-to-toggle record button;
- minimal voice playback.

A dev-only **Bake-off Recorder** scene opens first in the dev APK and runs the whole consented-session flow through that same path:
1. setup with a consent gate;
2. operator reference recording;
3. reversed reference → hold → instant flip reveal → Keep / Redo / Skip;
4. WAV + JSON export;
5. adb summary and delete.

Both APKs build. The **release build now verifies its own APK**: one scene, no dev code in IL2CPP metadata, no INTERNET. Everything that needs a microphone or a phone is **UNVERIFIED**, because no device was connected. The test list is below.

## Changes
| Path | Change |
|---|---|
| `Scripts/Core/Audio/WavEncoder.cs` | 16-bit PCM WAV encode/decode (pure bytes; clamped, symmetric ±32767; skips unknown chunks) |
| `Scripts/Core/Content/PhraseCatalogueReader.cs` | Validated catalogue JSON → `PhraseInfo` list (errors refuse, warnings returned) |
| `Scripts/Runtime/Audio/IMicrophoneBackend.cs`, `UnityMicrophoneBackend.cs` | Test seam; the backend is the only `Microphone.*` caller (P00 C8, enforced by test) |
| `Scripts/Runtime/Audio/MicrophoneCapture.cs` | §8 state machine driven by `Tick()`: rate choice, non-looping clip (8 s + 1 s), startup deadline, position before End, cached position, exact frame extraction, auto-stop, idempotent stop, amplitude, diagnostics |
| `Scripts/Runtime/Audio/MicrophoneRecorder.cs` | MonoBehaviour owner: ticks the capture; cancels on pause, focus loss, disable and destroy |
| `Scripts/Runtime/Audio/CaptureTypes.cs` | `CaptureSettings` (§7 defaults), `CaptureStatus`, `CaptureResult`, `CaptureStartResult`, `CaptureDiagnostics` |
| `Scripts/Runtime/Audio/MicrophonePermissionService.cs` | Granted / Denied / DeniedPermanently / Unsupported; Android via `Permission` + rationale check; app-settings intent; results marshalled to the main thread |
| `Scripts/Runtime/Audio/AudioPlayback.cs`, `AudioClipConversion.cs` | Minimal one-voice playback that awaits the real end (P00 C10); P08 extends it |
| `Scripts/Runtime/UI/HoldRecordButton.cs` | Hold (default) and tap-to-toggle (D-020); intents only; pointer-ID tracking; focus loss → cancel; `ResetState` after auto-stop |
| `Scripts/Runtime/Gameplay/PhraseCatalogueLoader.cs` | Loads phrases and SBPS from the synced TextAssets |
| `Scripts/Dev/Bakeoff/*` (5 files) | Recorder app, participant validation, storage/export, sidecar JSON (`bakeoff-take-v1`), code-built UI kit |
| `Scripts/Dev/SpeakBack.Dev.asmdef` | Constraint now `UNITY_EDITOR \|\| SPEAKBACK_DEV`; adds TextMeshPro |
| `Scripts/Editor/DataSync.cs` | Q8: menu **SpeakBack/Sync Data** copies `data/*.json` → `Assets/_SpeakBack/Data/Source/`; every build fails if the copies differ |
| `Scripts/Editor/SceneBuilder.cs` | Generates the recorder scene; game-scene rebuild moved to an explicit "Bootstrap/… (overwrites 03_Game)" menu item |
| `Scripts/Editor/BuildScripts.cs`, `ApkContents.cs` | Sync before build; refuse release scenes that depend on Dev scripts; recorder first in dev builds; **post-build APK inspection** |
| `Scripts/Editor/StartupPermissionDialogSuppressor.cs` | Adds `unityplayer.SkipPermissionsDialog=true` (P00 B2) |
| `Data/Source/*.json` | Synced copies of phrases, SBPS and dialogue |
| `Scenes/Dev/BakeoffRecorder.unity` | Recorder, playback and app wired; UI built at runtime |
| `Tests/PlayMode/*` | Fake microphone backend; capture (17), hold button (8), playback (4), permission service (7) tests |
| `Tests/EditMode/Editor/*` | New `SpeakBack.Editor.Tests` assembly: repo rules (8) and recorder logic (14) |
| `Tests/EditMode/Core/*` | WAV encoder (9) and catalogue reader (2) tests |
| `README.md` | New menu items and dev-APK behaviour |

## Tests
| Runner | Result | Count |
|---|---|---|
| `dotnet test Tools/DotnetTests` | **Passed** | 234/234 |
| Unity EditMode | **Passed** | 256 passed, 1 skipped (P02's `[Explicit]` benchmark) |
| Unity PlayMode | **Passed** | 42/42 |
| Python | N/A (unchanged; P03's 5/5) | |

**Prompt-required PlayMode scenarios (fake backend):**
- release during startup cancels: no audio, no Completed event, mic ended;
- double release processes once;
- max-length auto-stop, then a late release has no effect: clamped to max frames, `AutoStopped`;
- pause cancels and frees the clip: the clip is destroyed (checked after one frame);
- extracted samples = position × channels, exactly the recorded prefix (stereo).

**Extra capture tests:**
- rate choice for (0,0), in range, above range and below range;
- no device, open failure and startup deadline;
- cancellation token;
- position read *before* End (the fake resets position on End, as Unity does);
- cached position when the device stops and reports 0;
- amplitude and elapsed time;
- focus loss.

**Repo-rule tests:**
- only `UnityMicrophoneBackend.cs` uses `Microphone.` (comments ignored);
- no file IO or PlayerPrefs in Core or Runtime;
- no `Debug.Log` in Core or Runtime/Audio;
- Dev asmdef constraint;
- Build Settings = 03_Game only, with no Dev script dependencies, plus a positive control showing the recorder scene *is* detected;
- project settings verified;
- data copies byte-identical.

## Builds and artefact checks
| Build | Result | APK | Packaged scenes | Dev code in IL2CPP metadata | Permissions (aapt2) |
|---|---|---|---|---|---|
| Dev (`BuildAndroidDev`) | Success, 624 s | 57.2 MB | 2 (BakeoffRecorder first, 03_Game) | yes (expected) | INTERNET (dev only), RECORD_AUDIO, MODIFY_AUDIO_SETTINGS, BLUETOOTH |
| Release (`BuildAndroidRelease`) | Success, 1021 s | 30.9 MB | **1** (03_Game) | **no** | RECORD_AUDIO, MODIFY_AUDIO_SETTINGS, BLUETOOTH. **No INTERNET** |

Both manifests contain `unityplayer.SkipPermissionsDialog=true` (checked with `aapt2 dump xmltree`). The release build now *fails* if the APK has a different scene count, any INTERNET permission, or `SpeakBack.Dev` in its IL2CPP metadata. These are checks on the artefact, not just the build inputs.

## Verified vs UNVERIFIED
- **Verified:**
  - all three test runners;
  - both builds and every artefact check above;
  - the merged manifests (aapt2);
  - Core, Runtime and Dev compile for Android (the `#if UNITY_ANDROID` permission code compiled in both player builds).
- **UNVERIFIED (needs your phone tomorrow).** On-device test list:
  1. **Install:** `adb install -r Builds/Android/SpeakBack-dev.apk`, then launch. Expected: the Bake-off Recorder setup screen, with **no** OS permission prompt at launch (B2).
  2. **Permission:** tap *Record references (operator)*. Expected: the Android microphone prompt now.
     - Allow → the phrase screen.
     - Separately, deny twice → "blocked" message and the app settings page opens. Allow it there, come back, try again.
  3. **Startup latency (B1):** hold the button. Note how long "Starting mic…" shows before "Speak now". Each kept take's sidecar records `capture.startLatencyMs`.
  4. **References:** record all 10 phrases forwards, checking *Play forward* and *Play reversed* on each. The setup screen should then show "References recorded: 10/10".
  5. **Consent gate:** try *Start session* with a missing or invalid speaker code, no accent or voice, a consent ref with spaces, or the box unticked. Each should be refused with a clear message.
  6. **Session:** with a valid setup (e.g. yourself as `S00`), check that:
     - the reversed reference autoplays;
     - hold → "Speak now" → release → you hear the flip immediately;
     - *Hear flip again* and *Hear what was said* work;
     - Keep / Redo / Skip work;
     - releasing too early says so.
  7. **Edge cases:**
     - hold past 8 s: it auto-stops and lifting your finger afterwards does nothing;
     - lock the screen or switch apps mid-recording: you come back to an idle screen, not a stuck one;
     - try a tap-free quick press.
  8. **Export:** finish the session. Then:
     - run the adb command shown (or *Copy adb command*) into a folder **outside the repo**;
     - check that `<take>.flipped16k.wav`, `<take>.raw.wav` and `<take>.json` exist;
     - open a JSON and confirm there are no names or free text;
     - check the WAV plays forwards-sounding.
  9. **Delete:** *Delete this session* (tap twice), then confirm the folder is gone (`adb shell ls` on the path shown).
  10. **Game scene:** *Open game scene* shows the SpeakBack title clear of the notch (also closes the P01 check).
  11. **Optional:** `adb install -r Builds/Android/SpeakBack.apk` (release) opens straight to the title scene.

  If anything misbehaves, paste the on-screen message and what you did; the report-back can go straight into a bug-fix session.

## Decisions taken in this session
- **Dev assembly constraint `UNITY_EDITOR || SPEAKBACK_DEV`.** This resolves the P01 open point: the recorder scene can reference Dev components in the editor, dev players include them, and release players can't. The release build proves the last part from the APK's IL2CPP metadata, not by assumption.
- **Recorder scene first in the dev APK.** It's what you need to reach on a phone. A button opens 03_Game, and the release APK contains only 03_Game.
- **Capture logic is a plain class (`MicrophoneCapture`) ticked by the MonoBehaviour.** That makes every §8 rule testable with a fake backend and a fake clock.
  - The Completed event fires exactly once per capture (Ok or Cancelled). `StopCapture()` returns the same result, or `NotActive` when there is nothing to stop.
  - A failed or cancelled *start* reports through the start task only.
- **"One microphone owner" read as one owner class plus one backend file**, enforced by a source-scan test (P00 C8).
- **Startup latency (B1).** The recorder only says "Speak now" once audio is actually flowing, and every take records `startLatencyMs`. Whether a ready cue or a pre-opened mic is needed is decided from your device numbers, not now.
- **Permanent denial** is detected Android's way: denied and no rationale means "don't ask again", which leads to the app-settings route. OS callbacks are marshalled to the main thread.
- **Bake-off setup takes no free text.**
  - Speaker code must match `S` plus 2–3 digits.
  - The consent reference must be an ID (letters, digits, dashes); a name with a space is refused.
  - Accent and voice are chosen from the fixed lists in the session script.
  - The sidecar records `SystemInfo.deviceModel`, never the user-set device name.
- **Export writes on Keep**, sidecar last, so a crash can't leave a sidecar without its audio. Raw WAVs are at the capture rate; `flipped16k.wav` is the exact `ScorerInput`.
- **Delete actions need two taps.** There are three: *Delete ALL bake-off data* (sessions and references), *Delete this session*, and *Next participant*, which clears the form.
- **References are the operator's forward recording, trimmed.** The reversal is generated with the same `AudioReverser`. Imported phrase assets will take priority once P07 creates them; there's no hook needed until then.
- **03_Game wasn't touched.** Rebuilding it is now an explicit bootstrap menu item, so later prompts can edit the scene by hand safely.

## Deviations from the spec
- None in behaviour.
- The prompt's "pointer exit" handling is deliberately *not* a release. Unity delivers pointer-up to the pressed object even off-button, so a finger drifting off doesn't cut the recording. Focus loss and disable cancel.

## Findings
- **Unity adds `BLUETOOTH` and `MODIFY_AUDIO_SETTINGS`** alongside `RECORD_AUDIO` when `Microphone` is used. Both are install-time (normal) permissions with no prompt. They'll appear in the store listing, so note them for D-024 and the privacy review. No action needed now.
- **The dev APK is 57 MB, the release 31 MB.** The difference is dev-only code, the development player and debug symbols, not shipped content.

## Licences and third-party items added
None.

## Questions for Jamie
None blocking. The device results tomorrow decide whether B1 (startup latency) needs a ready cue.

## For the next prompt
- **HUMAN STEP A/B follows P04:** run the on-device list above first, then the consented sessions (`checklists/bakeoff-session-script.md`, `checklists/consent-form.md`; get the consent form checked against ICO guidance first), then blind rating.
- **P05 inputs:**
  - each take's `<take>.flipped16k.wav` (16 kHz mono, 16-bit) plus `<take>.json` (schema `bakeoff-take-v1`: `phraseId`, `speakerCode`, `groupLabels.accent`/`voice`, `consentRef`, `deviceModel`, `capture.*`, `processing.*`, `durations.*`);
  - references at `<persistentDataPath>/bakeoff_refs/<phraseId>.forward.wav` and `.reversed.wav`, which can be pulled the same way for the DTW candidate.
- **Runtime API for P08:**
  - `MicrophoneRecorder.StartCaptureAsync/StopCapture/CancelCapture` and its `Completed` event;
  - `AudioPlayback.PlayAsync(...) → PlaybackEnd`;
  - `HoldRecordButton` intents and `ResetState()`;
  - `MicrophonePermissionService.EnsureMicrophoneAsync`.
