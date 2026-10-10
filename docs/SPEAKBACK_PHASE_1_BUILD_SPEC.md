# SpeakBack — Phase 1 Build Specification

**Version:** 2.1  
**Date:** 10 October 2026  
**Studio:** Uprise Studios / J M Games  
**Platform:** physical Android device first  
**Status:** implementation contract; dependencies and scorer selection require validation

Product authority: `SPEAKBACK_UNITY_MASTER_SPEC.md`. Character authority: `SPEAKBACK_CHARACTER_EASTER_EGG_BIBLE.md`. This document governs Phase 1 technical detail (Master roadmap U1, U2 and a minimal U3 slice).

### Changes in 2.1

- Applied `DECISIONS.md`: Unity 6 LTS and API 26 minimum (D-001/D-002); SBPS phoneme inventory (D-011); integer score maths and shared golden vectors (D-012/D-013); Python bake-off harness (D-014); engine-free `SpeakBack.Core` assembly plus fast .NET test loop (D-015); dev-only Bake-off Recorder scene (D-016); adults-only recordings (D-017); licence policy (D-018); Git LFS and bundled model (D-019); tap-to-toggle setting (D-020); reveal timing (D-021); onboarding cards (D-022).
- Milestones now map one-to-one to the prompt pack (`prompts/P00`–`P14`).
- Starter data provided: `data/golden/`, `data/phrases/phase1_phrases.json`, `data/phonemes/sbps.json`, `data/dialogue/uprise_phase1.json`.

### Changes in 2.0

- Aligned with Master Spec 2.0: host is **Uprise** (Axiom retired); four verdicts (Perfect/Great/Close/Try Again) with 0–100; difficulty tiers Easy/Tricky/Hard/Brutal/Legendary; repository paths under `Assets/_SpeakBack`; `PhraseDefinition` naming.
- Bake-off adds an explicit accent/voice fairness check.
- Uprise reaction lines and expression set taken from the Character Bible.
- Retains from v1.1: scorer bake-off, `IAttemptScorer`, phoneme sequences, 16 kHz capture, Hear Raw and Compare, prompt autoplay rules, group fun test.

## 1. Goal and boundaries

Deliver an installable Android prototype that proves this loop:

**Play reversed reference → player imitates it → capture → reverse capture → player hears their attempt forwards → local score and Uprise reaction → Replay / Hear Raw / Compare / Retry / Next.**

One scene, ten fixed phrases, one host (Uprise, text reactions with expression sprites), no backend. No progression, persistent voice saves, purchases, ads, accounts, daily challenges, sharing, Cora, lore systems or remote analytics. Code boundaries support the future game without implementing future systems.

The target text is visible in Phase 1 to simplify learning. Reading the forward text aloud is not the challenge. Blind mode later hides it.

Before starting, add what the Flutter prototype established to Master Spec section 38 so proven behaviour is confirmed rather than re-researched. Known: two independent mic captures caused recognition timeouts, so Phase 1 uses one capture only.

## 2. Build assumptions and dependency gate

Use the Unity 6 LTS line with the Universal 2D template (D-001); pin the exact patch in P01. Prove Android compatibility of the selected scorer before locking versions. Do not upgrade to a guessed latest editor. Commit `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json` and `Packages/packages-lock.json` after the feasibility spike passes.

| Item | Phase 1 decision |
| --- | --- |
| Language | C#, conservative syntax compatible with chosen editor |
| UI | uGUI Canvas, TextMeshPro, EventSystem |
| Rendering | Simple 2D UI and sprite-based host expressions |
| Android build | IL2CPP/ARM64, contingent on scorer runtime compatibility |
| Minimum/target Android API | Minimum API 26 (D-002); target = Google Play's current requirement, checked and recorded at P01 |
| Application ID | `com.uprisestudios.speakback` (D-003, provisional) |
| Licences | Shipped code/assets: MIT, BSD, Apache-2.0, Zlib, OFL, CC0, CC-BY only. Never GPL/AGPL/non-commercial in the build (D-018) |
| Evaluation | `IAttemptScorer`; implementation chosen by bake-off; no cloud fallback |
| Model(s) | Chosen on agreement with human ratings and fairness, then size, latency and licence |
| Testing | Unity Test Framework for pure audio/scoring and controller behaviour |
| Asset delivery | Ten phrases, Uprise art and scorer model available on first offline launch |
| Audio | Single mono capture requested at 16 kHz; resample only if device or scorer requires it |

**Feasibility deliverable (after bake-off):** build APK, score a known flipped WAV fully offline on a physical device with the winning scorer, record latency, memory and installed size, and record engine/model/plugin versions and licences in `docs/dependency-decisions.md`. Verify the scorer's actual sample-rate and input-format requirements. Do not mark evaluation complete with an editor-only demo or a mock score.

No package APIs in this document imply a verified current integration. Consult official documentation at implementation time and pin the working combination.

## 3. Repository layout

```text
README.md
CLAUDE.md                             # rules for Claude Code sessions
docs/
  SPEAKBACK_UNITY_MASTER_SPEC.md
  SPEAKBACK_PHASE_1_BUILD_SPEC.md
  SPEAKBACK_CHARACTER_EASTER_EGG_BIBLE.md
  DECISIONS.md
  ART_BRIEF.md
  reports/                            # one report per prompt (P00-report.md …)
  dependency-decisions.md             # created by feasibility spike
  scorer-bakeoff.md                   # protocol, ratings summary, results, decision
  phase-1-validation.md               # created during tests
Assets/
  _SpeakBack/
    Art/UI/                           # logo
    Art/Characters/Uprise/            # expression sprites
    Audio/Phrases/                    # ten forward/reversed pairs
    Data/Phrases/                     # ten PhraseDefinition assets
    Data/Dialogue/                    # Uprise Phase 1 reaction pools
    Data/Config/PrototypeConfig.asset
    Scenes/03_Game.unity
    Scenes/Dev/BakeoffRecorder.unity  # SPEAKBACK_DEV builds only (D-016)
    Prefabs/UI/HoldRecordButton.prefab
    Scripts/
      Core/SpeakBack.Core.asmdef        # noEngineReferences: true (D-015)
      Core/Audio/                       # pure DSP: buffer, trim, reverse, resample
      Core/Scoring/                     # SequenceSimilarity, Verdict, ScoreMath
      Core/Content/                     # SBPS inventory, phrase validation
      Core/Round/                       # state machine rules, RoundContext
      Runtime/SpeakBack.Runtime.asmdef  # references Core + UnityEngine
      Dev/SpeakBack.Dev.asmdef          # define constraint SPEAKBACK_DEV
      Core/GameState.cs
      Core/ChallengeController.cs
      Core/PrototypeConfig.cs
      Audio/AudioBuffer.cs
      Audio/MicrophonePermissionService.cs
      Audio/MicrophoneRecorder.cs
      Audio/AudioProcessor.cs
      Audio/AudioReverser.cs
      Audio/AudioResampler.cs
      Audio/SilenceTrimmer.cs
      Audio/AudioPlayback.cs
      Gameplay/PhraseDefinition.cs
      Gameplay/PhraseDatabase.cs
      Scoring/IAttemptScorer.cs
      Scoring/ScoringOutcome.cs
      Scoring/SequenceSimilarity.cs     # shared edit-distance maths
      Scoring/Verdict.cs
      Scoring/PhonemeScorer.cs          # if selected
      Scoring/TextScorer.cs             # if selected or kept as diagnostic
      Scoring/TimingScorer.cs           # experiment only unless selected
      Speech/ISpeechRecognitionProvider.cs
      Speech/RecognitionResult.cs
      Speech/<Engine>RecognitionProvider.cs
      Characters/ReactionManager.cs
      Characters/HostPresenter.cs
      UI/GameUI.cs
      UI/HoldRecordButton.cs
    Tests/EditMode/
    Tests/PlayMode/
  ThirdParty/                         # if vendor package permits relocation
Tools/
  BakeOff/                            # Python 3.11+ harness over rated recordings (D-014)
  Content/                            # phonemisation, reversal, manifest validation
  DotnetTests/                        # links Core/*.cs for fast `dotnet test` (D-015)
data/                                 # source-of-truth content copied from the pack
  golden/ phrases/ phonemes/ dialogue/
Packages/
ProjectSettings/
ThirdPartyNotices.md
```

Keep model packaging in the runtime-supported path, documented in dependency decisions. Android StreamingAssets may be packaged inside the application archive; do not assume ordinary filesystem paths. Extract/copy models locally when the runtime requires files, verify completion before initialisation, and never require a download for the first offline round.

`Tools/` is excluded from the player build. Bake-off recordings are never committed; store them under the consent terms in section 15.

The per-file list above shows responsibilities; where a class is pure logic it lives under `Core/`, and its Unity-facing wrapper under `Runtime/`. Commit `.meta` files and keep GUIDs stable. Exclude `Library`, `Temp`, `Obj`, logs, builds and secrets. Models, WAV/OGG audio and PSD/large art go through Git LFS (D-019).

## 4. Scene and inspector wiring

`03_Game.unity` hierarchy:

```text
GameRoot
  ChallengeController
  Services
    PermissionService
    MicrophoneRecorder
    AudioPlayback
    ScorerHost
  MainCamera
  Canvas
    SafeAreaRoot
      Header (logo, challenge counter)
      HostPanel (Uprise image, expression state, reaction text)
      PromptPanel (target text, guidance, PlayPrompt button)
      RecordingPanel (status, elapsed time, amplitude, HoldRecordButton)
      ProcessingPanel (Flipping/Analysing label)
      ResultPanel (score, verdict, "Prototype score", diagnostic, Replay/Hear Raw/Compare/Retry/Next)
      ErrorPanel (message, Retry/Settings action)
  EventSystem
```

Screen Space Overlay Canvas, safe-area handling, scalable portrait layout, large hit targets. Verdicts show colour plus text. The diagnostic line shows what the scorer heard (phonemes or words, labelled "Heard after flip") on the result only, never as a scoring promise.

Inspector references: controller receives configuration, phrase database, recorder, playback, scorer host, host presenter and UI. Playback owns voice and effects AudioSources. Recorder never owns playback. Validate missing references at startup with a clear error.

**Uprise art:** prototype art is acceptable until the artist delivers. Expression set: neutral, listening, thinking, restrained smile, amused, surprised, dry concern. HostPresenter swaps sprites with a short transition; no rigging in Phase 1. Uprise is never shown stern or militaristic.

No persistent singleton or scene loader. Do not require Boot/MainMenu.

## 5. Data contracts

Namespace `Uprise.SpeakBack` with subnamespaces by area. Contracts are signatures to implement, not drop-in compiled code.

### AudioBuffer

```csharp
public sealed class AudioBuffer
{
    public float[] Samples { get; }
    public int SampleRate { get; }
    public int Channels { get; }
    public int FrameCount => Samples.Length / Channels;
    public double DurationSeconds => (double)FrameCount / SampleRate;
}
```

Interleaved floats; one frame contains all channels at a timestamp. Validate rate/channels > 0, divisible length and finite values. Ownership defined by constructor; never mutate a buffer a worker is reading. Process into new buffers.

### PhraseDefinition / PhraseDatabase

`PhraseDefinition : ScriptableObject` fields:

- Stable `Id`, `TargetText`, locale `en-GB`.
- `Phonemes`: ordered string array in **SBPS** (`data/phonemes/sbps.json`, D-011). Each engine adapter maps its own output into SBPS through a versioned table.
- `PhonemeSource`, `PhonemeVersion`; generated by `Tools/Content` and manually reviewed, never hand-typed ad hoc.
- `Difficulty` (Easy, Tricky, Hard, Brutal, Legendary) and category.
- Forward and reversed `AudioClip` references.
- Expected forward duration derived from the asset.
- Content/audio version and provenance note.

Database holds ten unique phrases and validates IDs, text, phonemes (when the phoneme scorer is selected) and audio. Sequential selection, wrapping after ten. Retry keeps the phrase; Next advances exactly once. No adaptation.

### RecognitionResult

```csharp
public enum RecognitionStatus { Success, Unavailable, Failed, TimedOut, Cancelled }
public enum TokenKind { Word, Phoneme }
public sealed class RecognitionResult
{
    public RecognitionStatus Status { get; }
    public TokenKind Kind { get; }
    public IReadOnlyList<RecognizedToken> Tokens { get; }
    public string DisplayText { get; }
    public string EngineVersion { get; }
    public string ModelVersion { get; }
    public double ProcessingMilliseconds { get; }
    public string FailureCode { get; }
}
```

Token: text, optional start/end, optional confidence. Missing confidence remains missing. Success may contain zero tokens. Failure never masquerades as empty success. UI uses typed error mapping.

### ScoringOutcome

Fields: status, integer score 0–100, verdict, scorer version (e.g. `phoneme-ed-v1`), target and observed token sequences, edit distance, optional component diagnostics, typed failure. A score exists only for a valid voice capture and successful evaluation.

### RoundContext

Immutable phrase ID/version plus monotonically increasing round ID. Holds raw capture, processed buffer and playback clips only for the active round. A cancellation source per active operation. Retry creates a new round ID for the same phrase.

## 6. Class responsibilities and contracts

| Class | Owns | Does not own |
| --- | --- | --- |
| ChallengeController | State, round identity, orchestration, valid actions, cancellation, cleanup | DSP or engine internals |
| MicrophonePermissionService | OS request, permission state, settings route | Recording |
| MicrophoneRecorder | Single capture start/stop, position, actual frame extraction, amplitude | Trimming, scoring |
| SilenceTrimmer | Outer-silence bounds | Removing internal pauses |
| AudioProcessor | Validate, trim, optional bounded gain, downmix/resample preparation | Unity objects on worker |
| AudioReverser | Frame-correct PCM reversal | Microphone/UI |
| AudioResampler | Anti-aliased conversion when required | Deciding whether to resample |
| AudioPlayback | Play/stop clips and sequences (Compare); await end | Scoring |
| IAttemptScorer | Initialise, score processed buffer against phrase, shutdown | Gameplay policy |
| ISpeechRecognitionProvider | Engine/model lifecycle, PCM conversion, serialised native work | Scoring maths |
| SequenceSimilarity | Deterministic edit distance and similarity | Engine access |
| ReactionManager | Verdict/context → authored line + expression; no immediate repeat | Score changes |
| HostPresenter | Expression sprites and transitions | Choosing lines |
| GameUI | Render state/result/errors; emit intentions | Game rules |
| HoldRecordButton | Hold/release, cancel/lost focus, tap alternative | Direct mic calls |

Suggested contracts:

```csharp
Task<PermissionOutcome> EnsureMicrophoneAsync(CancellationToken ct);
Task<CaptureStartResult> StartCaptureAsync(CancellationToken ct);
CaptureResult StopCapture();
void CancelCapture();
AudioProcessResult Process(AudioBuffer capture, PrototypeConfig config);
AudioBuffer ReverseFrames(AudioBuffer input);
Task PlayAsync(AudioBuffer buffer, CancellationToken ct);
Task PlaySequenceAsync(IReadOnlyList<AudioClip> clips, float gapSeconds, CancellationToken ct);
Task InitializeAsync(CancellationToken ct);
Task<ScoringOutcome> ScoreAsync(AudioBuffer forwardFacingMono, PhraseDefinition phrase, CancellationToken ct);
int EditDistance(IReadOnlyList<string> target, IReadOnlyList<string> observed);
Verdict ToVerdict(int score);
```

Typed results for NoVoice, TooShort, DeviceUnavailable, InvalidData, EvaluationFailed and Cancelled. Bind UI intents once; unsubscribe on disposal. Async handlers catch all exceptions and convert to safe state; no unowned fire-and-forget work.

## 7. Prototype configuration defaults

Provisional test values, not established thresholds.

| Setting | Initial value |
| --- | ---: |
| Requested capture rate | 16,000 Hz mono (fallback: supported rate + resample) |
| Maximum recording | 8 seconds |
| Minimum retained duration | 0.25 seconds |
| Boundary padding | 100 ms each side, clipped to bounds |
| Silence analysis window | 20 ms |
| Base RMS voice threshold | 0.008 on normalised float input |
| Minimum voiced windows | 3 across capture |
| Microphone startup deadline | 2 seconds |
| Evaluation deadline | 5 seconds per attempt |
| Automatic evaluation retries | 1, recoverable engine failure only |
| Compare gap | 0.4 seconds with a short cue |
| Prompt autoplay | On first entry to a phrase; not on Retry |
| Verdict thresholds | Perfect ≥ 90, Great ≥ 75, Close ≥ 50, Try Again < 50 |
| Level normalisation | Off until quiet/loud testing justifies it |
| Recording mode | Hold-to-record default; tap-to-toggle accessibility setting (D-020) |
| Flip animation | ≤ 400 ms, overlapping reveal playback start (D-021) |

Do not reject speech on duration alone. Record clipping/noise diagnostics for the session without audio logging.

## 8. Microphone implementation

1. Explain microphone purpose in-game, then request permission before capture.
2. Stop all app audio; disable prompt/replay inputs during capture.
3. Choose the default mic, query supported rates, request 16 kHz if in range, otherwise the nearest supported rate. Inspect and record actual `AudioClip.frequency/channels`.
4. Start a non-looping bounded buffer. Await positive mic position with a deadline; never spin the main thread.
5. Position is **sample frames**, not interleaved float count.
6. On release, read position **before** ending the microphone; cache the last valid position.
7. Extract exactly `recordedFrames * channels` samples on the main thread; discard the tail.
8. Always end the mic and destroy the clip on stop, error, cancellation or pause.

This is the **only** microphone owner; evaluation consumes its PCM. At maximum length, stop once and process normally; a later pointer-up has no effect. Startup failure gives retry, no score. Release during startup cancels. No sidetone.

**Android audio processing:** devices may apply automatic gain control and noise suppression, and Unity does not expose the capture source. Per device, record whether processing is audible and compare scorer results on the same speech. If harmful, raise a native capture plugin as a later decision; do not build it in Phase 1.

## 9. Processing and reversal

Clip extraction/creation/destruction on the main thread. DSP on owned arrays; offload resampling and native evaluation.

Outer trim: RMS per window; first/last voiced windows with noise-floor handling; add padding; preserve everything between. Insufficient voice → NoVoice. Quiet consonants are not cut for being below peak.

Optional gain: bounded, peak-protected, never amplifies silence into validity.

Frame reversal invariant:

```csharp
for (int frame = 0; frame < input.FrameCount; frame++)
    for (int channel = 0; channel < input.Channels; channel++)
        output[frame * input.Channels + channel] =
            input.Samples[(input.FrameCount - 1 - frame) * input.Channels + channel];
```

Stereo `[L0,R0,L1,R1]` becomes `[L1,R1,L0,R0]`. `Array.Reverse` is valid only for mono. Double reversal restores every frame.

Downmix by channel averaging if not mono. Resample only if rates differ, with anti-aliasing; never relabel the rate or drop samples. Convert to 16-bit PCM only if the runtime requires it; confirm bytes vs samples.

The trimmed raw (unreversed) capture is retained for Hear Raw Attempt.

## 10. Prompt asset generation

Record authorised/original clean English forward speech, or use a voice with game redistribution rights under a written agreement. Keep all ten prompts consistent in voice, loudness and environment. Generate reversed assets with the same frame-correct algorithm. Listen in both directions, including a "reverse comedy" listen.

Generate phonemes with `Tools/Content` into SBPS through one documented mapping table (ARPAbet→SBPS is provided in `sbps.json`). Draft phonemes for the ten phrases are in `data/phrases/phase1_phrases.json`, marked `draft_unreviewed` until Jamie signs them off. Common dictionaries are US English; review each phrase for UK pronunciation and record corrections. Content tools run offline only; check their licences.

Reference audio never plays while recording. The scorer receives the transformed **player result**. The forward reference is used by Compare and debugging only.

| ID | Target | Category | Provisional difficulty |
| --- | --- | --- | --- |
| sb_en_001 | Blue banana | Food | Easy |
| sb_en_002 | Tiny dragon | Animals | Easy |
| sb_en_003 | Hello robot | Everyday | Easy |
| sb_en_004 | Purple moon | Weird | Easy |
| sb_en_005 | My cat rules | Animals | Easy |
| sb_en_006 | Please pass the biscuits | British | Tricky |
| sb_en_007 | The robot stole my sandwich | Weird | Tricky |
| sb_en_008 | Never trust a talking penguin | Animals | Tricky |
| sb_en_009 | I speak fluent nonsense | Weird | Tricky |
| sb_en_010 | The tiny dragon ate my homework | Weird | Hard |

Swap any phrase that reverses dully. Never use generated noise as phrase audio.

## 11. Scorer bake-off and evaluation adapter

### 11.1 Bake-off protocol

0. **Tooling:** recordings are captured with the dev-only Bake-off Recorder scene (D-016) on real phones, through the same `MicrophoneRecorder` and `AudioProcessor` the game uses, and exported as 16 kHz mono WAV plus a JSON sidecar (phrase ID, pseudonymous speaker code, coarse group label, device model, consent reference). Participants are adults only (D-017).
1. **Collect** 40–60 flipped attempts across the ten phrases from at least five consenting speakers, deliberately varying UK regional accent, pitch/voice type, age range where possible, plus one quiet speaker, one noisy room and at least two phones. Include poor, partial and good attempts.
2. **Rate** each flipped attempt blind (no scores shown): at least three raters choose good, partial or unintelligible, plus an optional 0–10 closeness. Use the median.
3. **Run** every candidate offline via `Tools/BakeOff`:
   - Phoneme recogniser with no word language model + phoneme edit distance.
   - Word ASR (e.g. a sherpa-onnx model, Whisper tiny/base via whisper.cpp or Unity Inference Engine) + word and character similarity.
   - Word ASR with a constrained grammar plus unknown-word token, as a control only.
   - Timing similarity (MFCC + DTW against the forward reference).
   - Vosk small English model, as a baseline.
4. **Measure** per candidate: Spearman rank correlation with human ratings; obvious inversions (human "good" scored below human "unintelligible" on the same phrase); share of valid attempts scoring 0; score spread on two-word phrases; **fairness** — mean score gap between speaker groups for attempts humans rated equally.
5. **Filter** survivors by model size, warm latency, memory and licence.
6. **Decide** in `docs/scorer-bakeoff.md`: winner, version string, results, rejected candidates and why.

A combined scorer may be tried only with weights fitted on part of the data and checked on held-out data; otherwise ship the best single scorer.

### 11.2 Adapter rules

Load one engine/model once per session; initialise without freezing UI; one job at a time; supply complete processed audio; finalise and use final output.

Phoneme recognition must not use a word language model or lexicon constraint. Word ASR baselines are unconstrained. Constrained-grammar experiments include unknown-word and noise controls and only ship if they win fairly.

Discard late results by round ID; do not start another native job until the first finishes or the adapter resets. `Task.WhenAny` timeout does not stop native work. Dispose handles in runtime-supported order after work stops.

No cloud fallback. If no candidate is feasible on device, document it and stop before UI polish. Never substitute randomness or timers for scoring.

## 12. Phase 1 score formula

All candidates share one deterministic similarity over token sequences:

```text
D = Levenshtein distance over tokens (insert, delete, substitute cost 1)
N = target token count (N > 0, else content error)
score = D >= N ? 0 : (200 * (N - D) + N) / (2 * N)   // integer division (D-012)
// equivalent to floor(100 * (1 - D/N) + 0.5), without floating-point drift
verdict = Perfect ≥ 90 | Great ≥ 75 | Close ≥ 50 | Try Again
```

| Scorer version | Target tokens | Observed tokens |
| --- | --- | --- |
| `phoneme-ed-v1` | `PhraseDefinition.Phonemes` | Recognised phonemes after inventory mapping, stress removed |
| `word-ed-v1` | Normalised words of target | Normalised words of transcript |
| `char-ed-v1` | Characters of normalised target, no spaces | Characters of normalised transcript, no spaces |

Text normalisation: Unicode normalisation, invariant lowercase, punctuation to spaces, collapsed whitespace, identical on both sides. Phoneme normalisation: shared inventory, strip stress/length marks, drop silence tokens.

Empty successful observation of a valid capture scores 0 (Try Again). Extra tokens count as errors. Order preserved. No confidence multiplier, duration reward or invented components. Weighted phoneme substitution costs are a later version after calibration.

Examples (word scorer): "never trust a talking penguin" → identical 100 (Perfect); "never trust the talking penguin" 80 (Great); "blue banana" → "blue" 50 (Close); → "banana blue" 0 (Try Again).

Illustrative phoneme case: "blue banana" as `b l uː b ə n ɑː n ə` (N = 9 in this example inventory); attempt heard as `b l uː b ə n æ n ə` gives D = 1, score 89 (Great), where the word scorer would give 50 for "blue bandana". Exact phonemes depend on the chosen inventory.

C# and the Python harness must both pass `data/golden/similarity_cases.json` and `verdict_cases.json` (D-013). Display "Prototype score" during testing; log scorer version with every observation.

## 13. Uprise reaction data

Authored pools are provided in `data/dialogue/uprise_phase1.json` (import as ScriptableObject data; Jamie may edit lines). First launch shows three onboarding cards from the same file, then a guided first round on sb_en_001; skippable, shown once per install (D-022). Text only in Phase 1, from the Character Bible's Uprise voice: dry, warm, witty, notices improvement, never stern. Each line tagged with an expression. Minimum pools: 6 per verdict, 4 for valid zero, 4 for improvement on retry. Seed lines:

- Perfect (restrained smile): "Well. That's inconvenient. I had a very good encouraging speech prepared."
- Great (amused): "Close enough that I'm now suspicious you've been practising."
- Close (neutral): "You've got the shape of it. One more."
- Try Again (dry concern): "That was definitely backwards. Whether it belonged to this language is still under review."
- Valid zero (surprised): write in Uprise's voice — playful, never mocking the player.
- Improvement (restrained smile): write lines that notice progress more than the number.

Errors use separate, neutral copy: permission needed, no voice heard, too short, microphone unavailable, "That one got scrambled. Give it another go?". No immediate repeats. No lore, Cora references or relationship content in Phase 1.

## 14. State machine and valid inputs

| State | Permitted actions | Completion |
| --- | --- | --- |
| LoadingChallenge | Exit/cancel | Ready → PlayingPrompt (new phrase) or ReadyToRecord (Retry) |
| PlayingPrompt | Stop/exit | End → ReadyToRecord |
| ReadyToRecord | Play prompt, begin capture, Next | Capture start → Recording |
| Recording | Release/stop/cancel | Valid capture → ProcessingAudio |
| ProcessingAudio | Exit/cancel | Processed → PlayingResult |
| PlayingResult | Exit/cancel | End → Scoring or ShowingResult |
| Scoring | Exit/cancel | Score ready → ShowingResult |
| ShowingResult | Replay, Hear Raw, Compare, Retry, Next | Intent selects next state |
| PlayingReview | Stop | End → ShowingResult |
| RecoverableError | Retry/settings/Next | Recovery → ReadyToRecord |
| Suspended | None | Resume → clean ReadyToRecord or error |

Scoring starts concurrently with the reveal playback. Show the score only when both finish; if playback ends first, show Analysing with the thinking expression.

Every transition validates round/operation identity. Retry and Next invalidate older callbacks. Hold disabled during playback/processing. Replay, Hear Raw and Compare never rescore. Retry clears score/error/capture, keeps the phrase. Next releases round-owned buffers before selecting a new phrase.

Focus loss/pause: cancel capture, stop playback, invalidate processing, reset UI; never resume a held mic. Destruction: cancel and dispose safely. Errors never trap the player behind a disabled control.

## 15. Privacy and diagnostics

**Player build:** no PCM files by default; round audio lives in memory until Retry, Next, exit or destruction. Extracted model files are not recordings. No transcripts, phonemes or audio in logs or crash breadcrumbs. Session-only numeric diagnostics cleared on exit. No network permission; verify airplane-mode first launch.

**Bake-off recordings:** adults (18+) only (D-017); explicit, informed consent using `checklists/consent-form.md`, covering what is recorded, who hears it, storage and deletion. Stored outside the repo and build, labelled by pseudonymous speaker code (with only the coarse accent/voice-group label needed for the fairness check), deleted at study end unless consent covers retention.

## 16. Implementation milestones

1. **Project:** pin editor, commit settings, `03_Game` scene, UI skeleton, config.
2. **Bake-off data:** quick capture tool, consent forms, 40–60 flipped attempts, blind ratings.
3. **Scorer bake-off:** run all candidates in `Tools/BakeOff`; write `scorer-bakeoff.md`; select winner.
4. **On-device feasibility:** package winner, score a known flipped WAV offline on Android; record versions, size, latency. Fall back to next-best if infeasible.
5. **Capture:** permission, 16 kHz request, start/release/cancel/max length, actual frames, Android processing notes.
6. **Pure audio:** validation, trim, reversal, resampling; fixtures.
7. **Content:** ten forward/reversed pairs, reviewed phonemes, PhraseDefinitions, rights recorded.
8. **Round controller:** autoplay prompt → recording → reveal; review playback; no overlap or stale callbacks.
9. **Scoring integration:** scorer in round, verdicts, typed errors, diagnostic line.
10. **Uprise and results:** expressions, reaction pools, transitions.
11. **Android test pass:** lifecycle, offline, devices, latency/memory, input variation.
12. **Fun test:** solo and group sessions; write go/no-go findings.

A working narrow slice at each milestone. Mock scorers live in tests only.

Prompt mapping (pack `prompts/`): P00 orientation · P01 project scaffold · P02 pure audio core · P03 scoring core and golden vectors · P04 capture service and Bake-off Recorder · P05 bake-off harness · P06 on-device feasibility · P07 content pipeline · P08 round controller · P09 game UI and Uprise · P10 scoring integration · P11 polish, accessibility and onboarding · P12 Android hardening · P13 fun-test build · P14 Phase 1 gate report. HUMAN STEPs sit between P04→P05 (record and rate), P06 (device), P07 (phoneme sign-off, prompt voice), P12 (device matrix) and P13 (fun tests).

## 17. Verification plan

### EditMode tests
- Mono/stereo reversal fixtures; double reversal; no channel swap.
- Length/rate/channels/NaN validation.
- Outer silence trimmed with padding; inner pause kept; quiet boundary.
- Silence → NoVoice; voice validity independent of scorer.
- Resampling: duration, rate, frame count; skipped when rates match.
- Sequence similarity: identity, insert, delete, substitute, reorder, empty observed, empty target error.
- Text normalisation; phoneme mapping and stress stripping.
- PhraseDefinition validation (phonemes required when phoneme scorer active).
- Verdict boundaries: 0/49/50/74/75/89/90/100.
- Reaction selection: correct expression per verdict; no immediate repeat; no lore content eligible.

### PlayMode with fake services
- Release during mic startup cancels safely.
- Double release / max-length race processes once.
- Next/Retry invalidates late scorer results.
- Permission/evaluation failure gives no score.
- Empty successful observation of valid capture scores 0.
- Replay/Hear Raw/Compare never rescore; no audio overlap.
- Prompt autoplays on new phrase, not on Retry.
- Pause cancels capture; UI resumes usable.

### Fast loop
- `dotnet test Tools/DotnetTests` runs all `SpeakBack.Core` tests and golden vectors in seconds; Unity EditMode runs the same tests authoritatively in CI or before each commit.

### Bake-off harness
- Deterministic scorers repeat identical results.
- Correlation, inversion and fairness-gap calculations verified on a hand-made fixture.
- Golden similarity/verdict vectors pass in Python and C# with identical output.

### Physical Android matrix
At least two devices, ideally three across manufacturers/tiers, including Jamie's primary phone: permission allow/deny/recovery, backgrounding, interruptions, quiet/normal/loud speech, moderate noise, speaker/headphones, rapid input, max recording, repeated rounds, airplane-mode first launch, granted capture rate, audible Android input processing.

Pure tests cannot validate native capture, model packaging or whether the game is funny.

## 18. Provisional acceptance gates (Phase 1 gate)

| Area | Gate |
| --- | --- |
| Functional | Ten phrases playable; Replay, Hear Raw, Compare, Retry, Next correct |
| Offline | Fresh install scores in airplane mode |
| Capture | 50 consecutive controlled rounds per device without capture/state failure |
| Lifecycle | Pause, denial, late callback and max-duration paths recover |
| Reversal | Fixtures pass; listening finds no truncation/corruption |
| Scorer agreement | Spearman ≥ 0.6 with human ratings on held-out set; no unexplained obvious inversions |
| Fairness | No speaker group's equally-rated attempts score more than 10 points lower on average; every gap investigated |
| Score spread | < 25% of human-rated "partial" attempts score 0 |
| Result latency | Capture stop → reveal p95 ≤ 300 ms |
| Scoring latency | Warm p95 ≤ 2 s for short phrases; UI responsive; 5 s deadline handled |
| Footprint | Installed and model size recorded and agreed acceptable |
| Resources | 20-minute session, no crash or accumulating clips/native jobs |
| Fun (solo) | ≥ 5 new testers; ≥ 4 understand after one guided round; ≥ 3 choose three more attempts unprompted |
| Fun (group) | ≥ 1 pass-the-phone session of 3+ players; record laughter, Hear Raw/Compare use and requests to continue |
| Credibility | Testers asked whether scores felt fair; every "unfair" example investigated |

Measure percentiles with enough repetitions; record device and sample counts. Human judgement, not scorer output, is the reference.

## 19. Scoring validation and decision

Reveal funny but no scorer meets agreement/fairness: keep the loop, pause U4+ work, extend scoring research (weighted phoneme costs, other models, timing hybrid). Winner fails on device: next-best feasible candidate before UI polish. Players don't want another go: revise phrase lengths, onboarding, reveal timing and Hear Raw/Compare presentation before content expansion. Phase 1 does not pass because an APK opens.

## 20. Delivery checklist

- [ ] Unity source with pinned editor/package/model versions and metadata.
- [ ] `03_Game` scene; Uprise expressions; ten licensed phrase pairs with reviewed phonemes.
- [ ] `Tools/BakeOff` and `Tools/Content` with README.
- [ ] `docs/scorer-bakeoff.md` with protocol, results, fairness check and decision.
- [ ] Physical-device APK.
- [ ] Permission, cancellation, offline, capture/processing/scoring and result handling.
- [ ] Audio/scoring/controller tests at real failure boundaries.
- [ ] README with build/run and model setup; no secrets.
- [ ] ThirdPartyNotices and dependency decisions.
- [ ] Validation report: devices, latency/resources, Android processing, scoring limitations, solo and group fun findings.
- [ ] Master Spec section 38 (Flutter learnings) completed.
- [ ] Explicit Phase 1 pass/revise/stop decision before U4.

## 21. Instructions for Claude Code or a future Loom build

Work through `prompts/P00`–`P14` in order, one per session, following `CLAUDE.md`. Read all three documents and `DECISIONS.md` before implementation. Build Phase 1 only. Run the bake-off before integrating any scorer. Keep audio and scoring testable outside scene objects. One microphone capture feeds everything. Use real offline evaluation in the player build. Do not add services, monetisation, accounts, voice upload, Cora or lore systems. Record compatibility and scorer decisions rather than guessing versions. Protect async work with cancellation and round IDs. Return working source, device build steps and measured evidence, and label unverified items clearly.

Loom (formerly Forge) involvement remains contingent on its separate proof-of-concept outcome; this document is usable for a direct Claude Code build.
