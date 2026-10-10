# P00 Report — Orientation and plan check

**Date:** 2026-10-10 · **Commit:** the commit titled "P00: orientation report" (see `git log`) · **Prompt:** speakback-pack/prompts/P00-orientation.md

## Summary
This was a read-and-plan session. I read CLAUDE.md, DECISIONS.md, the Phase 1 Build Spec, the requested Master Spec sections (0–7, 23–37, 41–42), Bible §7.1, all of `data/`, and the pack prompts P01–P15 and checklists. All the data checks pass: every phrase phoneme is in SBPS, every golden case recomputes exactly, and every dialogue pool meets its minimum. The environment has most of what's needed, but there's **no Android device connected** and **`UNITY_PATH` is not set**. The only installed Unity 6 editor (6000.6.4f1) looks like a non-LTS release, which conflicts with D-001. I found 4 spec contradictions that change code structure (questions 3–6 below), plus several Android microphone risks and gaps in the prompts.

## Changes
| Path | Change |
|---|---|
| `docs/reports/P00-report.md` | New (this report). No other files changed. |

## Environment

| Item | Found |
|---|---|
| git | `git version 2.53.0.windows.1` |
| git-lfs | `git-lfs/3.7.1 (GitHub; windows amd64; go 1.25.1)`. Hooks installed in this repo (`pre-push`, `post-checkout`). No LFS-tracked files yet. |
| `$UNITY_PATH` | **not found**: unset in the process, user and machine environments |
| Unity editors installed (Hub) | `2021.3.36f1`, `2022.3.21f1`, `6000.6.4f1` (`Unity.exe` ProductVersion `6000.6.4f1_12bfff696524`) |
| Project editor version | `ProjectSettings/ProjectVersion.txt`: `6000.6.4f1 (12bfff696524)` |
| Is 6000.6.4f1 LTS? | **Probably not.** The Unity Hub cache lists 6000.0.x and 6000.3.x under the `LTS` stream query and 6000.6.4f1 under `SUPPORTED`/`TECH`. This is an inference from the Hub cache, so confirm it in Unity Hub (see Q1). |
| Android Build Support (6000.6.4f1) | **Installed.** SDK platforms `android-34`, `android-36`, `android-37.0`; build-tools `36.0.0`; cmdline-tools `16.0`; NDK `27.2.12479018`; OpenJDK `17.0.18` |
| dotnet SDK | `10.0.103` (runtimes: Microsoft.NETCore.App `8.0.24` and `10.0.3`, so a `net8.0` test project can run) |
| python3 | **not found**: `python3` is the Microsoft Store stub ("Python was not found"). `python` / `py` = **Python 3.14.2** (`C:\Python314`); `py -3.12` = **Python 3.12** also installed |
| adb | `Android Debug Bridge version 1.0.41`, `Version 36.0.0-13206524` (`%LOCALAPPDATA%\Android\Sdk\platform-tools`) |
| adb devices | **No devices attached** (empty list) |
| Java on PATH | `openjdk 17.0.16` |
| Repo | `main`, clean, 1 commit (`c5132a0`), remote `origin` = github.com/UpriseStudios/SpeakBack |

## Tests
| Runner | Result | Count |
|---|---|---|
| dotnet test Tools/DotnetTests | N/A: doesn't exist until P01 | – |
| Unity EditMode | N/A: no code; `UNITY_PATH` unset | – |
| Unity PlayMode | N/A | – |
| Python (throwaway data validation, not committed) | **PASS**: 0 failures | 13 similarity + 1 error case, 10 verdict + 2 invalid, 10 phrases, 6 pools |

## Data validation (`data/`)

The script ran from the session scratchpad with `python -I -X utf8`. It was not committed.

**SBPS (`data/phonemes/sbps.json`).** Has 44 symbols (24 consonants + 20 vowels), all unique. `ɡ` is U+0261 everywhere and `ː` is U+02D0, with no ASCII `g` or `:` anywhere. Every ARPAbet target is in SBPS. Four SBPS symbols have **no ARPAbet source**: `ɒ ɪə eə ʊə`. This is expected (CMUdict is US English), but it means `Tools/Content phonemise --engine arpabet` can never propose them. "robot" and "nonsense" would come out with `ɑː` and need manual correction.

**Phrases (`data/phrases/phase1_phrases.json`).** 0 errors.
- All 10 IDs match Phase 1 §10's text, category and difficulty exactly.
- All phonemes, including sb_en_007's 2 alternates, are in SBPS.
- `phonemeCount` matches the sequence length for every phrase.
- N per phrase: 9, 10, 9, 8, 9, 16, 19, 22, 18, 22.
- All 10 are `draft_unreviewed`, which is expected.

**Golden similarity (`data/golden/similarity_cases.json`).** All 13 cases match `expectedDistance` and `expectedScore`, and `empty_target` raises EmptyTarget. Each case was recomputed two ways: with the D-012 integer formula, and with an exact-rational `floor(100·(1−D/N)+0.5)` clamped at 0. An exhaustive sweep (N = 1…200, D = 0…2N) found **0 mismatches** between the two forms, so D-012's equivalence claim holds.

**Golden verdicts (`data/golden/verdict_cases.json`).** All 10 cases pass. −1 and 101 are rejected. The thresholds 90/75/50 match Master §6.4, Phase 1 §7 and Phase 1 §12.

**Dialogue (`data/dialogue/uprise_phase1.json`).**

| Pool | Count | Minimum | Result |
|---|---|---|---|
| perfect | 6 | 6 | OK |
| great | 6 | 6 | OK |
| close | 6 | 6 | OK |
| tryAgain | 6 | 6 | OK |
| validZero | 4 | 4 | OK |
| improvement | 4 | 4 | OK |

- There are 3 onboarding cards (D-022) and 5 error lines (all of §13's list).
- No duplicate IDs. Every expression is in the §4 set of seven.
- All four §13 seed lines are present.
- No Cora, Axiom or lore keywords appear.

## Findings

Severity: **High** = changes code structure or blocks P01–P04 · **Med** = will bite a later prompt · **Low** = tidy-up.

### A. Contradictions between specs, decisions, data and prompts

| # | Sev | Where | Finding and why it matters | Recommended resolution |
|---|---|---|---|---|
| A1 | High | D-001; Phase 1 §2; `ProjectVersion.txt` | D-001 requires the **Unity 6 LTS line, Universal 2D template**. The repo project is **6000.6.4f1**, which looks non-LTS, and it was made from the **URP 3D blank template**: `PC_RPAsset`/`Mobile_RPAsset`, `TutorialInfo/`, `SampleScene`, Android ID `com.UnityTechnologies.com.unity.template.urpblank`. Phase 1 §2 says "Do not upgrade to a guessed latest editor". | Q1 and Q2. |
| A2 | High | Phase 1 §3 (file list) vs CLAUDE.md "Architecture rules", D-015 and P01 task 3 | §3 lists `Core/GameState.cs`, `Core/ChallengeController.cs` and `Core/PrototypeConfig.cs` under `Scripts/Core/` (`noEngineReferences: true`), but ChallengeController is a MonoBehaviour and PrototypeConfig a ScriptableObject. It also lists `Audio/…`, `Gameplay/…`, `UI/…` directly under `Scripts/` with no `Runtime/` folder. P01 says "folder layout exactly as Phase 1 §3", which can't be done literally. Master §25 also recommends ~11 assemblies, versus the 3+3 in CLAUDE.md and P01. | Q3. |
| A3 | High | Phase 1 §5 PhraseDefinition, §6 `ScoreAsync(…, PhraseDefinition …)`, Master §27 | `IAttemptScorer` takes a `PhraseDefinition` (a ScriptableObject), so it can't live in engine-free Core. P03 task 7 already fixes this with a Core `PhraseInfo` record, but the specs still show the old signature. | Follow P03: Core `IAttemptScorer` takes `PhraseInfo`, and Runtime converts `PhraseDefinition → PhraseInfo` (P07). No question needed; recorded here so later prompts don't "correct" it back. |
| A4 | High | Phase 1 §12 ("strip stress/length marks") vs `sbps.json` `normalisation.note` ("Length (ː) … is never stripped") and D-011 | Stripping `ː` would merge `iː/ɪ`, `uː/ʊ` and `ɑː/ʌ`-like pairs and turn SBPS tokens into non-SBPS ones. This affects `PhonemeNormaliser` in P03. | Q4: follow `sbps.json` and D-011 (keep `ː`; strip stress and diacritics only). |
| A5 | High | Phase 1 §7 ("Do not reject speech on duration alone") vs §6 typed `TooShort`, §7 "Minimum retained duration 0.25 s", P02 task 5 (0.1 s voice → TooShort), dialogue `errors.tooShort` | It's unclear whether TooShort is an absolute floor or relative to the phrase's expected duration. This determines the shape of `TrimResult` in P02. | Q5. |
| A6 | Med | `uprise_phase1.json` `errors.noVoice` / `errors.tooShort` ("Hold the button…") vs D-020 (tap-to-toggle setting) | These lines are wrong when tap-to-toggle is on. | P09/P11: add mode-specific variants (e.g. `noVoice.tap`), with Jamie approving the copy. Not blocking. |
| A7 | Med | `uprise_phase1.json` notes ("cooldown 5 … per pool") vs P09 task 6 ("last 5 picks … or pool size − 1") | With 6-line pools and a 5-pick cooldown, only one line is ever eligible, so selection becomes a fixed rotation and the weights do nothing. The 4-line pools fall back to size − 1 = 3, which is also a fixed rotation. | P09: cooldown = min(3, size − 2) so 2+ lines are always eligible. Jamie to confirm at P09. |
| A8 | Med | `phase1_phrases.json` sb_en_006 / sb_en_008 / sb_en_005 vs Master §6.5 and Phase 1 §18 Fairness | The primary targets are Southern RP: `pɑːs` ("pass"), `trʌst` ("trust"), `ʌ` generally. Northern English speakers say `pæs`, `trʊst`. With primary-only scoring (sb_en_007 `reviewNote`), Northern speakers lose about 1 phoneme per word on correct attempts, which builds a regional gap into the fairness gate. | Q6. Also add Northern variants at HUMAN STEP D phoneme sign-off. |
| A9 | Low | Master §0 hierarchy (Master → Phase 1 → Bible; no DECISIONS) vs CLAUDE.md read order (DECISIONS → Phase 1 → Master) | No practical conflict today, because D-001…D-025 are applied in both specs. A future decision applied to only one of them would be ambiguous. | Add DECISIONS.md to Master §0's precedence list at the next spec edit. |
| A10 | Low | Master §24 service names (`AudioCaptureService`, `AttemptScoringService`) vs Phase 1 §6 (`MicrophoneRecorder`, `ScorerHost`) | Naming drift. | Use Phase 1 names in Phase 1 (Master §0 lets Phase 1 own technical detail). |
| A11 | Low | Phase 1 §13 ("import as ScriptableObject data") vs P09 task 6 (runtime JSON loader) | Two different import paths are described. | Follow P09 (TextAsset JSON loaded by a Runtime loader); either satisfies "strings in data assets". |
| A12 | Low | CLAUDE.md, D-025, Phase 1 §16 and §21 refer to `prompts/…` and `checklists/…` at the repo root | They actually live at `speakback-pack/prompts/` and `speakback-pack/checklists/`. Literal paths fail, including this prompt's report-template path. | Q7. |
| A13 | Low | `uprise_phase1.json` `errors.permissionNeeded` ("You can allow it in Settings") vs Master §28 | The line suits *permanent* denial. First denial should offer Retry rather than Settings. | P11: separate lines for denied and permanently denied. |

### B. Unity 6 Microphone API on Android: risks in the Phase 1 spec

| # | Sev | Where | Risk | Recommended handling |
|---|---|---|---|---|
| B1 | High | Phase 1 §8 step 4, D-020 hold-to-record | **Start latency clips the first phoneme.** On Android, `Microphone.GetPosition` often stays 0 for roughly 100–500 ms after `Start` (**UNVERIFIED** on Jamie's phones). With hold-to-record, players start speaking on press, so the first sound (often a plosive: b, p, t) is lost. That depresses scores systematically and can't be fixed in DSP. | P04: show the "Listening" state and give a haptic tick only once position > 0, and record start latency in `CaptureDiagnostics` (already planned). If device numbers are bad, the options are a "3-2-1"/ready cue or keeping the mic open while ReadyToRecord. The second has a privacy-indicator cost, so it would need Jamie's decision. |
| B2 | Med | Phase 1 §8 step 1, Master §28 | **Startup permission prompt.** Unity's Android player has historically requested manifest-declared dangerous permissions at launch unless told not to (`unityplayer.SkipPermissionsDialog`). That would show the OS prompt before the in-game explanation. Whether 6000.x still does this is **UNVERIFIED**. | P04: verify on a device that no prompt appears at launch. If it does, add the skip flag through a custom manifest or Gradle template. |
| B3 | Med | Phase 1 §8 step 3, §17 device matrix ("granted capture rate") | `GetDeviceCaps` commonly returns (0,0) on Android (P04 already handles this). `AudioClip.frequency` reports the *requested* rate, because Unity resamples internally, so the device matrix's "granted capture rate" will read 16000 on every phone and reveal nothing about the hardware rate. | Re-word the device-matrix field to "Unity clip rate". The real hardware rate needs a Java call; that's optional and P12-level. |
| B4 | Med | Phase 1 §8 steps 6–7 | After `Microphone.End`, `GetPosition` returns 0. On a non-looping clip that fills up, position may wrap to 0 or stop, depending on the device. | Already covered by spec ("read before End", "cache last valid position") and P04's +1 s headroom. Keep the PlayMode tests in P04. |
| B5 | Med | Phase 1 §18 (capture stop → reveal p95 ≤ 300 ms), `AudioManager.asset` `m_DSPBufferSize: 1024` | Mic teardown, possible audio-mode switching after capture (some devices route or duck playback after mic use) and a 1024-frame DSP buffer all add to reveal latency. | P12: measure (already planned) and try "Best latency" DSP buffer. Speaker routing after mic use goes into the device matrix. |
| B6 | Med | Phase 1 §8 "Android audio processing" | AGC and noise suppression can't be controlled through Unity's Microphone API. A noise gate can zero out quiet consonants, which interacts with the 0.008 RMS threshold. | Already scoped (P12 "Input check"). Bake-off recordings go through the same path, which is correct. |
| B7 | Low | Phase 1 §8, P04 | `Microphone.*` and `AudioClip.GetData`/`Create` are main-thread only. | Already in spec §9. P04 must not put extraction on a worker. |
| B8 | Med | P01 task 5, P01 "Done when" (no INTERNET) | Unity **development** builds add `INTERNET` for the profiler and debugger connection (**UNVERIFIED** for 6000.x). P01's manifest check on `SpeakBack-dev.apk` may show INTERNET even if release is clean. | P01: check INTERNET on the **release** APK's merged manifest, and record the dev APK's permissions separately. |
| B9 | Med | D-018/D-019, P05 step 5, P06 | Google Play requires **16 KB memory-page alignment** for native `.so` libraries in apps targeting Android 15+. Scorer runtimes (onnxruntime/sherpa-onnx, whisper.cpp, Vosk) ship native libraries that may not be aligned. P05's practicality filter doesn't mention this. | Add "16 KB page-size compatible ARM64 `.so`" to P05's filter and P06's checks. |

### C. Gaps: things a later prompt needs that no prompt provides

| # | Sev | Needed by | Gap | Recommended resolution |
|---|---|---|---|---|
| C1 | High | P04 ("data/phrases via a Runtime loader"), P06 (mapping JSON "byte-identical"), P09 (dialogue JSON) | `data/` is outside `Assets/`, so nothing in it reaches a build. No prompt says how the JSON gets into the player or how drift between the two copies is prevented. | Q8. |
| C2 | Med | Phase 1 §1 ("Before starting, add what the Flutter prototype established to Master Spec section 38"), §20 checklist | No prompt does this, and it needs Jamie's knowledge (reversal quality, latency, scoring credibility, device issues, player reactions). | Q9. |
| C3 | Med | Phase 1 §3 and §20 (`README.md` with build/run and model setup) | No prompt creates the root `README.md`. P05/P07 make tool-level READMEs only. | P01 creates a minimal root README; P06 and P12 extend it. |
| C4 | Med | P01 task 8 (fonts), P01 task 5 (current Google Play target API), P05 (models, pip packages) | These need network access during development. CLAUDE.md says to stop and ask when a task "would need network services". | Q10. |
| C5 | Med | P01 target API | I couldn't check Google Play's current target-API requirement offline. The editor ships `android-36` and `android-37.0`. **UNVERIFIED**: API 36 is likely required for new apps from late August 2026. | P01 checks Google's official "target API level requirements" page (if Q10 allows), records the source, and sets it. |
| C6 | Med | P01 task 4 / D-015 (one set of Core files compiled by both Unity and `dotnet`) | Unity's compiler supports C# 9 (with gaps such as no `init` without an `IsExternalInit` shim). The installed .NET 10 SDK defaults `net8.0` projects to C# 12, so code that passes `dotnet test` could fail in Unity. | P01: set `<LangVersion>9.0</LangVersion>` (or whatever Unity reports) in `Tools/DotnetTests` and add an `IsExternalInit` polyfill in Core if `record`/`init` are used. Use NUnit for both runners so tests are shared verbatim. |
| C7 | Med | CLAUDE.md commands (`Tools/BakeOff/.venv/bin/pip`), P03/P05 Python | On Windows, a venv puts executables in `.venv\Scripts\`, not `bin/`, and `python3` is the Store stub. The default Python 3.14.2 is likely too new for some ML wheels (onnxruntime, vosk, torch); this is **UNVERIFIED** until P05 tries. | Q11. |
| C8 | Low | P04 Part A | P04's fake `IMicrophoneBackend` means some backend class other than `MicrophoneRecorder` calls `Microphone.*`, which literally breaks CLAUDE.md's "Nothing else touches `Microphone.*`". | P04: put `UnityMicrophoneBackend` next to `MicrophoneRecorder` as an internal class, as the only file containing `Microphone.` calls, and enforce that with an EditMode source-scan test. I'm reading the rule as "one mic owner"; flagging it rather than asking. |
| C9 | Low | P04 Part B export via `adb pull` | On Android 11+, access to `Android/data/<pkg>` through adb varies by device. | P04: give both `adb pull` and a `run-as`-based fallback (dev builds are debuggable), and test on Jamie's phone (HUMAN STEP). |
| C10 | Low | P04 Part B (reveal playback), P08 (AudioPlayback) | The Recorder needs playback before P08 builds `AudioPlayback`. | P04 builds a minimal `AudioPlayback` (play and await end, stop) that P08 then extends rather than replaces. |
| C11 | Low | `Packages/manifest.json` | The template brings in `com.unity.modules.unityanalytics`, `unitywebrequest*`, `com.unity.collab-proxy`, `visualscripting`, `ai.navigation`, `timeline`, `terrain`, `vehicles` and others. Services are disabled in `UnityConnectSettings.asset`, and the project is linked to cloud project `SpeakBack`. | P01: remove unused packages and modules (especially analytics and web request) and record what was removed in `dependency-decisions.md`. Smaller APK, smaller surface. |
| C12 | Low | HUMAN STEP A | `checklists/consent-form.md` says to check it against UK GDPR (ICO) guidance before first use. | Jamie action before the first recording session; doesn't block P01–P04 code. |

## Verified vs UNVERIFIED
- **Verified:** the tool versions in the environment table (commands run this session); the editor install and Android module contents (file inspection); project settings values (`ProjectSettings.asset`, `UnityConnectSettings.asset`, `AudioManager.asset`); all data validation results (script run).
- **UNVERIFIED:**
  - whether 6000.6.4f1 is LTS (inferred from the Hub cache; check the Unity Hub "Installs" badge);
  - Google Play's current target API;
  - every Android microphone behaviour in section B (B1 start latency, B2 launch-time permission prompt, B5 routing, B8 dev-build INTERNET). These need a physical device, and **no device is connected**.

## Decisions taken in this session
- I recomputed the golden vectors two ways (integer and exact rational) rather than one, so that D-012's equivalence claim was itself checked.
- I treated `speakback-pack/prompts/templates/report-template.md` as the template that `prompts/templates/report-template.md` refers to (see A12/Q7).

## Deviations from the spec
None. No specs or data were edited. Only this report is committed.

## Licences and third-party items added
None.

## Questions for Jamie
Answer each with a number, or say "defaults".

1. **Editor version (A1).** The repo is on 6000.6.4f1, which looks non-LTS, but D-001 says Unity 6 LTS. Which editor should P01 pin? **Default:** install the latest **6000.3 LTS** patch with Android Build Support in Unity Hub, and set `UNITY_PATH` to its `Editor\Unity.exe`. P01 then recreates the near-empty project on it. If you'd rather stay on 6000.6.4f1, say so and P01 will amend D-001 with your approval.
2. **Template (A1).** The current project is the URP 3D blank template with sample content. **Default:** P01 rebuilds from the **Universal 2D** template per D-001 and drops `TutorialInfo/`, `Readme.asset`, `SampleScene` and the PC renderer assets. The app ID, company and product names are set fresh (D-003).
3. **Folder and assembly layout (A2).** **Default:** `Scripts/Core/{Audio,Scoring,Content,Round}` holds pure C# only (`GameState`, the transition rules and `ProcessorConfig` values live here as plain types). `Scripts/Runtime/{Audio,Gameplay,Scoring,Speech,Characters,UI}` holds every Unity-facing class from §3's list (ChallengeController, PrototypeConfig ScriptableObject, MicrophoneRecorder, AudioPlayback, PhraseDefinition/Database, ScorerHost, HostPresenter, GameUI, HoldRecordButton). Assemblies are exactly as P01 task 3, not Master §25's eleven.
4. **Phoneme length marks (A4).** **Default:** follow `sbps.json` and D-011. Keep `ː` as part of SBPS symbols; strip only stress and diacritic marks listed in `normalisation.stripMarks`. Phase 1 §12's "length" wording gets corrected at the next spec edit.
5. **TooShort (A5).** **Default:** TooShort means the retained voiced span is under the absolute 0.25 s floor (§7). It's never compared with the phrase's expected duration, which is how I read "do not reject on duration alone". 0.1 s of voice → TooShort; a fast but complete 0.6 s attempt is always scored.
6. **Accent alternates (A8).** **Default:** P03 builds `TokenScorer` to accept one *or more* acceptable target sequences and score against the best (max) one. `phoneme-ed-v1` stays primary-only, as the data says. P05's harness also reports `phoneme-ed-best-v1` (primary plus alternates) so the bake-off can show whether alternates close any regional fairness gap. You add Northern variants (`pæs`, `trʊst`, …) at phoneme sign-off.
7. **Pack location (A12).** **Default:** P01 runs `git mv speakback-pack/prompts prompts`, `git mv speakback-pack/checklists checklists` and `git mv speakback-pack/README.md docs/PACK_README.md`, so every path in CLAUDE.md and the specs resolves literally.
8. **Getting `data/` into the build (C1).** **Default:** an editor menu and pre-build step, "SpeakBack/Sync Data", copies the needed `data/**/*.json` files into `Assets/_SpeakBack/Data/Source/` as TextAssets. An EditMode test fails if any copy isn't byte-identical to `data/`. `data/` stays the source of truth. P04 creates it, since P04 is the first to read phrases at runtime.
9. **Flutter learnings (C2).** Can you paste a few bullets on what the Flutter build showed (reversal quality, latency, scoring credibility, device issues, player reactions)? **Default:** if you give bullets, P01 adds them to Master §38 as a spec edit you've authorised. If not, §38 stays as it is and P14 flags it under §20.
10. **Network during development (C4/C5).** **Default:** yes, for dev tooling only. That covers Google Fonts' OFL sources for Baloo 2 and Nunito (P01), NuGet and pip packages, Google's target-API page (P01), and model downloads into a cache outside the repo (P05). Every item is pinned and licence-checked into `ThirdPartyNotices.md`. The game itself never makes network calls.
11. **Python for tooling (C7).** **Default:** `Tools/BakeOff` and `Tools/Content` use a **Python 3.12** venv (`py -3.12 -m venv`, Windows `Scripts\` layout, documented in each README). P03's pure-Python golden tests also run on 3.14.

## For the next prompt
Things you need to do before P01:
- Answer the questions above (or say "defaults").
- Set `UNITY_PATH` at user level to the chosen editor's `Unity.exe`. It's unset today.
- For the device "Done when" items, connect a phone with USB debugging on; `adb devices` is empty today.

Facts for the P01 session:
- `.gitignore` and `.gitattributes`, including the LFS patterns from D-019, already exist from commit `c5132a0`. P01 should inspect them, not recreate them.
- Current `ProjectSettings` are template values:
  - `companyName: DefaultCompany`
  - Android ID `com.UnityTechnologies.com.unity.template.urpblank`
  - `AndroidMinSdkVersion: 26` (already correct)
  - `AndroidTargetSdkVersion: 0` (automatic)
  - `AndroidTargetArchitectures: 2` (ARM64)
  - `scriptingBackend` unset (platform default)
  - `ForceInternetPermission: 0`
  - `microphoneUsageDescription` empty
  - `activeInputHandler: 1` (Input System only, so uGUI needs `InputSystemUIInputModule`)
  - `m_SerializationMode: 2` (force text)
- The build scenes list contains only `Assets/Scenes/SampleScene.unity`.
- Use Python as `python` or `py -3.12`, never `python3`.
- The .NET 8 runtime is present, so a `net8.0` test project runs under SDK 10.0.103.
