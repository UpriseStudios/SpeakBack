# P08 — Round controller and state machine

**Stage:** Phase 1 / U3 · **Depends on:** P04, P07 · **Spec:** Phase 1 §5 (RoundContext), §6, §14; Master §4; D-021

## Prompt

```
Read CLAUDE.md, Master Spec §4 (core loop), Phase 1 Build Spec §5 (RoundContext), §6, §7 and §14 (state machine), DECISIONS D-021.

Goal: the full round loop working end-to-end with a FAKE scorer in tests and a temporary dev scorer in the scene, with no stale callbacks, no audio overlap and correct cancellation. UI can stay ugly (P09 makes it nice).

1. Core/Round (engine-free):
   - GameState enum exactly as §14 (LoadingChallenge, PlayingPrompt, ReadyToRecord, Recording, ProcessingAudio, PlayingResult, Scoring, ShowingResult, PlayingReview, RecoverableError, Suspended).
   - RoundStateMachine: pure transition table — (state, intent/event) → new state or rejection. Intents: PlayPrompt, BeginCapture, ReleaseCapture, CancelCapture, Replay, HearRaw, Compare, Retry, Next, Exit, Pause, Resume, OpenSettings. Events: PromptEnded, CaptureStarted, CaptureFailed, CaptureCompleted(valid/invalid), ProcessingDone, RevealEnded, ScoreReady, ScoreFailed, ReviewEnded.
   - RoundContext: immutable phrase id/version + monotonically increasing roundId; Retry → new roundId same phrase.
   - Gate rule: ShowingResult only when BOTH RevealEnded and ScoreReady/ScoreFailed have arrived for the current roundId; if reveal ends first → Scoring (Analysing); if score arrives first it waits.
   - Exhaustive tests: every legal transition; every illegal intent rejected without side effects; stale roundId events ignored; Retry/Next from every allowed state; Pause from every state → Suspended → Resume → ReadyToRecord (or RecoverableError if prerequisites lost).
2. Runtime ChallengeController (MonoBehaviour) orchestrating: PhraseDatabase, MicrophonePermissionService, MicrophoneRecorder, AudioProcessor, AudioPlayback, ScorerHost (IAttemptScorer), ReactionManager (stub for now), GameUI (stub view interface IGameView). It owns a CancellationTokenSource per round and per operation; disposes on Retry/Next/Pause/destroy. All async methods catch exceptions → RecoverableError with a typed reason.
3. AudioPlayback: voice and SFX AudioSources; PlayAsync(AudioBuffer or AudioClip, ct) awaiting real end (use dspTime/ clip length + isPlaying check; no fixed sleeps); PlaySequenceAsync for Compare (forward reference → 0.4 s gap + cue → FlippedAttempt); StopAll(). Creates and destroys AudioClips for round buffers; no leaks (track and assert counts in tests).
4. Behaviour rules:
   - Prompt autoplays on a NEW phrase, not on Retry.
   - All app audio stops before capture; capture input disabled while anything plays or processes.
   - Reveal (FlippedAttempt) starts immediately after processing; scoring starts at the same time on a worker (ScorerInput).
   - Replay → FlippedAttempt; Hear Raw → RawAttempt; Compare → sequence; none of them rescore.
   - Invalid capture (NoVoice/TooShort/DeviceUnavailable) → RecoverableError with matching error line; never a score.
   - Scoring failure → one automatic retry if recoverable, else unscored "scrambled" message with Replay/Retry available.
5. Dev-only DevScorer (SpeakBack.Dev) returning a fixed fake outcome, used ONLY when SPEAKBACK_DEV is set AND the real ScorerHost is not available; the release build must not contain it. Prefer the real scorer from P06 if it is integrated already.
6. PlayMode tests with fakes (IMicrophoneBackend, IAttemptScorer, IGameView, fake clock): every scenario in Phase 1 §17 "PlayMode with fake services", plus: score arriving before reveal end waits; Next during scoring discards the late score; no AudioClip leak after 50 rounds.

Finish: docs/reports/P08-report.md (include the transition table as Markdown), commit "P08: round controller and state machine", stop.
```

## Done when

- [ ] On your phone (dev APK), you can play a full round end-to-end, even with plain UI.
- [ ] Spamming buttons, locking the screen mid-recording and pressing Next during scoring never leaves it stuck.
