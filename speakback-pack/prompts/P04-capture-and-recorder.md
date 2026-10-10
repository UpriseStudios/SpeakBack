# P04 — Capture service and Bake-off Recorder

**Stage:** Phase 1 / U1–U2 · **Depends on:** P02, P03 · **Spec:** Phase 1 §6, §8, §11.1 step 0, §15; D-016, D-017, D-020

## Prompt

```
Read CLAUDE.md, Phase 1 Build Spec §6 (class table), §8 (microphone) , §11.1 and §15, DECISIONS D-016/D-017/D-020, and checklists/bakeoff-session-script.md.

Goal: the single authoritative microphone path for the game, plus a dev-only scene Jamie can use on real phones to collect consented bake-off recordings through that same path.

Part A — Runtime capture (SpeakBack.Runtime)
1. MicrophonePermissionService: in-game explanation first (caller shows UI), then Android permission request via UnityEngine.Android.Permission with callbacks; states Granted / Denied / DeniedPermanently ("don't ask again") / Unsupported; OpenAppSettings() route on Android. Editor/desktop: treat as Granted when a device exists.
2. MicrophoneRecorder (the ONLY class that calls Microphone.*), implementing §8 steps 1–8 exactly:
   - choose default device; query GetDeviceCaps; request 16 kHz if within [min,max] (0,0 means any), else nearest supported; record the actual clip frequency/channels;
   - non-looping clip of maxSeconds (8) + 1 s headroom;
   - await position > 0 with a 2 s deadline without blocking the main thread (coroutine or awaitable on the main thread; no Thread.Sleep);
   - expose live Amplitude (RMS of the last ~50 ms) and Elapsed for UI;
   - StopCapture(): read position BEFORE Microphone.End; cache last valid position for auto-stop at max length; extract exactly frames*channels samples via AudioClip.GetData; destroy the clip; return CaptureResult { Status, AudioBuffer, DeviceInfo, GrantedRate };
   - CancelCapture() and OnApplicationPause/Focus loss always end the mic and destroy the clip;
   - idempotent stop (double release, release after auto-stop) — processes once;
   - never logs audio.
3. CaptureDiagnostics: device name, requested vs granted rate, channels, start latency ms, frames captured. Session-only, in memory.
4. HoldRecordButton UI component: pointer down/up/exit/cancel, tap-to-toggle mode from a setting (D-020), raises intents only (no mic calls).

Part B — Dev Bake-off Recorder (SpeakBack.Dev, SPEAKBACK_DEV only; scene Scenes/Dev/BakeoffRecorder.unity)
Purpose: Jamie runs supervised sessions with consenting adults. Flow per participant:
1. Setup screen: operator enters pseudonymous speaker code (e.g. S01), coarse group label from a fixed list (e.g. "Accent: Southern England / Midlands / Northern England / Scotland / Wales / Northern Ireland / Other UK / Non-UK"; "Voice: higher / middle / lower") and a consent reference ID; a mandatory checkbox "Signed consent form on file and participant confirmed 18+". Cannot proceed without it.
2. Reference audio: final prompt audio does not exist until P07. Add a "Record references" operator mode: the operator (Jamie) records each phrase spoken normally once; the app saves it as bakeoff_refs/<phraseId>.forward.wav and generates the reversed reference with AudioReverser. Imported phrase assets, when present, take priority. Without a reference, the phrase is marked unavailable rather than faked.
   For each of the ten phrases (from data/phrases via a Runtime loader): play reversed reference → participant holds to record → processed with AudioProcessor → instant reveal playback of FlippedAttempt → operator chooses Keep / Redo / Skip. Aim for 1–2 kept takes per phrase per person (configurable).
3. Export: for each kept take write to Application.persistentDataPath/bakeoff/<sessionId>/: <take>.flipped16k.wav (ScorerInput), <take>.raw.wav (RawAttempt) and <take>.json (phraseId, speakerCode, groupLabels, consentRef, device model, grantedRate, durations, diagnostics, app version, UTC timestamp). No names, no contact details, no free-text fields.
4. Export summary screen shows the folder path and an adb pull command. A "Delete session" button removes the folder.
5. WAV writer (16-bit PCM, mono) lives in Core (pure byte encoding) with tests.

Privacy checks: the Dev assembly must not be present in a release build — add an EditMode test or build-time check in BuildScripts that fails BuildAndroidRelease if any SpeakBack.Dev type is referenced from release scenes. The release build must not write any audio files.

Tests
- Core: WAV encoder round-trip (header fields, sample conversion, clipping).
- PlayMode with a fake IMicrophoneBackend interface (wrap Microphone.* behind it so the recorder logic is testable): release during startup cancels; double release processes once; max-length auto-stop then late pointer-up has no effect; pause cancels and frees the clip; extracted sample count equals position*channels.
- Build: BuildAndroidDev includes the recorder scene; BuildAndroidRelease excludes it (verify via build report).

Build the dev APK. You cannot test the microphone on a phone; state precisely what is UNVERIFIED and write a short on-device test list for Jamie in the report.

Finish: docs/reports/P04-report.md, commit "P04: capture service and bake-off recorder", stop.
```

## Done when

- [ ] Dev APK opens the Bake-off Recorder; you can record, hear the flip and export.
- [ ] `adb pull` gets WAV + JSON files; the JSON has no personal details.
- [ ] Release build has no recorder scene and writes no audio.

## HUMAN STEP A and B (before P05)

0. In the recorder's "Record references" mode, record yourself saying each of the ten phrases normally, in a quiet room.
1. Follow `checklists/bakeoff-session-script.md` with 5+ consenting adults, using `checklists/consent-form.md`. Collect 40–60 kept takes covering all ten phrases, a range of UK accents, at least one quiet speaker, one noisy room and two phones.
2. Pull the files to a folder **outside the repo**, e.g. `~/speakback-bakeoff/data/`.
3. Have 3+ raters score every flipped take blind with `checklists/rating-sheet.md`. Save as `~/speakback-bakeoff/ratings.csv`.
