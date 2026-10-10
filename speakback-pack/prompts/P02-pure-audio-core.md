# P02 — Pure audio core

**Stage:** Phase 1 / U1 · **Depends on:** P01 · **Spec:** Phase 1 §5 (AudioBuffer), §7, §9, §17

## Prompt

```
Read CLAUDE.md and Phase 1 Build Spec §5 (AudioBuffer), §7 (defaults) and §9 (processing and reversal).

Goal: engine-free, fully tested audio maths in SpeakBack.Core. No Unity types, no microphone, no UI.

Build in Assets/_SpeakBack/Scripts/Core/Audio/ (namespace Uprise.SpeakBack.Core.Audio):
1. AudioBuffer — immutable: interleaved float samples, sample rate, channels, FrameCount, DurationSeconds. Validating factory that rejects rate <= 0, channels <= 0, length not divisible by channels, NaN/Infinity. Defensive copy on construction; expose ReadOnlySpan/ReadOnlyMemory or a copy accessor (choose and justify).
2. AudioReverser.ReverseFrames(AudioBuffer) — frame-correct reversal per §9 (stereo pairs preserved).
3. Downmixer.ToMono(AudioBuffer) — channel averaging.
4. Resampler.Resample(AudioBuffer mono, int targetRate) — anti-aliased (windowed-sinc or polyphase with a low-pass at min(src,dst)/2 · 0.9). Returns the input unchanged (same instance or equal copy, documented) when rates match. Deterministic.
5. SilenceTrimmer — RMS windows (default 20 ms), threshold 0.008 with adaptive noise floor (e.g. 20th-percentile window RMS × factor, never below base threshold), minimum voiced windows 3, padding 100 ms clipped to bounds. Returns a TrimResult: status (Ok, NoVoice, TooShort), start/end frame, voiced window count, diagnostics (peak, RMS, noise floor). Never removes internal pauses.
6. LevelAnalyzer — peak, RMS, clipping ratio (|x| >= 0.99) for diagnostics.
7. AudioProcessor.Process(AudioBuffer capture, ProcessorConfig cfg) — validate → downmix → trim → (optional bounded peak-protected gain, off by default) → reverse → resample for the scorer. Returns AudioProcessResult { Status, RawAttempt, FlippedAttempt, ScorerInput, Diagnostics }:
   - RawAttempt: trimmed mono capture, unreversed, capture rate. This is what the player actually said (gibberish), used by Hear Raw.
   - FlippedAttempt: RawAttempt reversed, capture rate. This is the reveal: it should sound like the target phrase. Used by reveal playback, Replay and Compare.
   - ScorerInput: FlippedAttempt resampled to cfg.TargetRate (16 kHz) mono. The only thing a scorer ever receives.
   Use exactly these names everywhere (code, logs, UI, tests) so the direction of the audio can never be confused.
8. ProcessorConfig with the §7 defaults as named constants.

Tests (SpeakBack.Core.Tests, also run by Tools/DotnetTests):
- Mono and stereo reversal fixtures; double reversal equals original exactly; stereo channel order preserved.
- Validation failures for each invalid input.
- Trim: leading/trailing silence removed with padding; internal 300 ms pause preserved; quiet consonant at the boundary (amplitude just above floor) kept; pure silence → NoVoice; 0.1 s voice → TooShort; noisy-floor case.
- Resampler: 48k→16k and 44.1k→16k keep duration within 1 frame of expectation; a 1 kHz sine keeps its frequency (zero-crossing count) and a 7.5 kHz sine is strongly attenuated after 48k→16k (alias rejection); no-op when rates equal.
- Processor end-to-end on a synthetic stereo 48 kHz buffer: RawAttempt == trim(mono); FlippedAttempt == reverse(RawAttempt); ScorerInput is FlippedAttempt at 16 kHz; rates correct.
- Property-style tests with a seeded RNG (e.g. 200 random buffers): reverse(reverse(x)) == x; trimming never extends length.

Performance: process an 8 s 48 kHz stereo buffer in under 50 ms on desktop in a benchmark test (mark [Explicit]/Category("Perf") so it does not run by default); report the number.

Run both test runners. Finish: docs/reports/P02-report.md, commit "P02: pure audio core", stop.
```

## Done when

- [ ] All Core audio tests pass in `dotnet test` and Unity EditMode.
- [ ] The report states the perf number and which resampler design was chosen.
- [ ] Naming makes RawAttempt and FlippedAttempt impossible to confuse.
