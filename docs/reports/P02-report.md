# P02 Report — Pure audio core

**Date:** 2026-10-10 · **Commit:** the commit titled "P02: pure audio core" (see `git log`) · **Prompt:** prompts/P02-pure-audio-core.md

## Summary
`SpeakBack.Core` now contains the whole engine-free audio path in `Uprise.SpeakBack.Core.Audio`: an immutable `AudioBuffer`, frame-correct reversal, downmixing, an anti-aliased polyphase resampler, outer-silence trimming with a noise floor, level analysis, and `AudioProcessor`. The processor produces the three attempt buffers, named `RawAttempt`, `FlippedAttempt` and `ScorerInput` everywhere. 89 tests pass identically in `dotnet test` and Unity EditMode. Desktop processing of an 8 s 48 kHz stereo capture takes **34.1 ms (median, Release)**.

## Changes
| Path | Change |
|---|---|
| `Scripts/Core/Audio/AudioBuffer.cs` | Immutable interleaved buffer: validating `Create` (copies), `Silence`, `SliceFrames`, `ToArray`, internal no-copy `FromOwnedArray` |
| `Scripts/Core/Audio/AudioReverser.cs` | `ReverseFrames`, frame-correct per §9 |
| `Scripts/Core/Audio/Downmixer.cs` | `ToMono` by channel averaging (returns the same instance when already mono) |
| `Scripts/Core/Audio/Resampler.cs` | Polyphase Kaiser-windowed sinc, cut-off 0.9 × lower Nyquist, 32 zero crossings, β = 8 |
| `Scripts/Core/Audio/SilenceTrimmer.cs` | `Trim` → `TrimResult` (Ok / NoVoice / TooShort, bounds, voiced span, peak/RMS/noise floor/threshold) |
| `Scripts/Core/Audio/LevelAnalyzer.cs` | `LevelStats` (peak, RMS, clipping ratio at ≥ 0.99) and `Rms(span)` |
| `Scripts/Core/Audio/AudioProcessor.cs` | `Process` → `AudioProcessResult` {Status, RawAttempt, FlippedAttempt, ScorerInput, Diagnostics}; `ProcessDiagnostics` holds numbers only |
| `Scripts/Core/Audio/ProcessorConfig.cs` | §7 defaults as named constants; immutable, validated |
| `Tests/EditMode/Core/Audio/*.cs` | 8 test files plus `TestSignals` (deterministic xorshift fixtures) |
| `Scripts/Core/Audio/.gitkeep` | Removed (folder now has content) |

## Tests
| Runner | Result | Count |
|---|---|---|
| `dotnet test Tools/DotnetTests` | **Passed** | 89/89 (12 from P01 + 77 new) |
| Unity EditMode | **Passed** | 89 passed, 1 skipped (the `[Explicit]` benchmark) |
| Unity PlayMode | **Passed** | 3/3 (unchanged from P01) |
| Perf benchmark (`dotnet test -c Release --filter FullyQualifiedName~AudioPerformanceTests`) | **Passed** | median **34.1 ms**, min 33.3 ms (9 runs after 3 warm-ups) |
| Python | N/A | |

**Measured filter figures** (from the resampler tests):

| Tone and conversion | Level change |
|---|---|
| 1 kHz pass-band, 48 k and 44.1 k → 16 k | within ±0.1 dB, frequency ±1 Hz |
| 7.5 kHz, 48 k → 16 k (inside the transition band) | **−23.7 dB** |
| 10 kHz, 48 k → 16 k (would alias) | −96.5 dB |
| 12 kHz, 48 k → 16 k | −102.8 dB |
| 11 kHz, 44.1 k → 16 k | −105.5 dB |

**Coverage against the prompt's list:**
- **Reversal:** mono and stereo fixtures, plain `Array.Reverse` shown wrong for stereo, 3 channels, double reversal, no input mutation.
- **Validation:** rate, channels, partial frame, NaN/±Inf, null, slice bounds.
- **Trimming:**
  - outer silence removed with padding;
  - internal 300 ms pause kept;
  - quiet consonant at the boundary kept (above threshold, and also via padding when below it);
  - silence → NoVoice, lone 20 ms click → NoVoice, 0.1 s voice → TooShort, 0.3 s voice → Ok;
  - noisy floor (RMS 0.03) trimmed correctly;
  - speech filling the whole capture kept;
  - padding clipped to the capture.
- **Resampler:** durations within 1 frame for 7 rate pairs (including 22.05 k and upsampling), pass-band level and frequency, attenuation, determinism, DC including the edges, mono-only, no-op returns the same instance.
- **Processor:** stereo 48 kHz end to end, checking RawAttempt == trim(mono), FlippedAttempt == reverse(RawAttempt), ScorerInput == resample(FlippedAttempt, 16 k), and rates. Also diagnostics, NoVoice/TooShort carry no buffers, gain bounds, and gain never turning sub-threshold noise into voice.
- **Property tests:** 200 seeded random buffers each for double reversal, trimming never extending length, and directional invariants.

## Verified vs UNVERIFIED
- **Verified:** all of the above in both runners; Core and its tests have no `UnityEngine` usage (grep; the only engine reference is the test-runner entry in the test asmdef); release-mode desktop timing.
- **UNVERIFIED:**
  - Timing on Android under IL2CPP. P12 measures capture-stop → reveal on devices.
  - Behaviour with real microphone audio and Android AGC/noise suppression. The trimmer is only tested on synthetic signals until P04 captures and P12's input check.
  - P01's on-phone checks are still open (no cable today): installing the dev APK and seeing the title, and pressing Play on `03_Game` in the editor.

## Decisions taken in this session
- **`AudioBuffer.Samples` is a `ReadOnlySpan<float>`**, plus `ToArray()` for an owned copy. Spans give zero-copy reads in the DSP loops and can't be written through, so a buffer a worker is reading can't change. The class itself is immutable, so the buffer object, not a span, is what crosses threads. Unity call sites such as `AudioClip.SetData` use `ToArray()`. Empty buffers are allowed, because a capture can be zero frames; the trimmer reports them as NoVoice.
- **Resampler design:** polyphase windowed sinc with exact phases for every practical rate pair (gcd-reduced; 44.1 k → 16 k uses 160 phases), each phase normalised to unit DC gain. The edges renormalise over the taps inside the signal, so there's no fade. Same rate → same instance (documented).
- **7.5 kHz test, read literally.** The prompt calls this "alias rejection", but 7.5 kHz is below the 8 kHz Nyquist of 16 kHz output, so it can't alias. It sits in the transition band above the 0.9 × Nyquist cut-off. It measures −23.7 dB, and the test asserts < −20 dB. True alias rejection is tested separately on 10, 11 and 12 kHz tones at < −60 dB (measured −96 to −106 dB). A sharper filter (64 zero crossings) would push 7.5 kHz to about −80 dB but roughly double the cost, past the 50 ms budget. Speech content at 7.2–8 kHz doesn't matter to a 16 kHz recogniser.
- **Noise floor = 10th percentile × 3** (the prompt's example was the 20th). At the 20th percentile, a capture that is more than 80% speech puts the "noise floor" on speech, so the threshold climbs and voice is lost. The 10th percentile still lands on silence when only about 10% of the capture is quiet. The threshold never drops below the base 0.008.
- **No peak-relative cap on the threshold.** One would let a perfectly steady signal count as voice, but it would also make steady room noise count as voice, and that would be scored 0 instead of reported as NoVoice. That breaks "technical failures are never scored as zero". The trade-off is documented and tested in `PerfectlySteadySignal_FillingWholeCapture_IsTreatedAsNoise`. Real speech varies by syllable, which `SpeechFillingWholeCapture_KeepsEverything` covers. Worth watching on devices with strong AGC.
- **TooShort** follows P00 Q5: voiced span (padding excluded) under 0.25 s. NoVoice is checked first: fewer than 3 voiced windows.
- **Optional gain** (off by default, §7): applied only after the voice decision, only ever boosts (×1 to ×3.98, i.e. +12 dB max), never past a −1 dBFS peak. It applies to RawAttempt, so Hear Raw and the reveal play at the same level.
- **Test fixtures use their own xorshift RNG**, so Mono (Unity) and .NET generate identical signals.
- **Benchmark reports the median.** One run in the Release benchmark hit 900 ms (a JIT tier-up pause), so the median is the statistic, and the test documents that it's meaningful only in Release. Debug builds measured 126 ms.

## Deviations from the spec
- Noise-floor percentile 10% instead of the prompt's "e.g. 20%". It was given as an example; reasoning above.
- 7.5 kHz attenuation threshold as explained above (−20 dB asserted, −23.7 dB measured), with separate true-alias tests added.

## Licences and third-party items added
None.

## Questions for Jamie
None blocking.

## For the next prompt (P03)
- Audio API: `Uprise.SpeakBack.Core.Audio`. The scorer contract takes `AudioBuffer` (16 kHz mono, i.e. `AudioProcessResult.ScorerInput`).
- `ProcessDiagnostics` is the numeric, audio-free record for session diagnostics. Its pattern of no tokens, no audio and no text is the one to follow for scoring diagnostics.
- Shared test conventions: tests in `Tests/EditMode/Core/<Area>/`; NUnit 3.5-compatible APIs; `TestSignals.Rng` for any randomness; `[Explicit]` + `[Category("Perf")]` for benchmarks.
- P03's golden-vector tests read `data/` from disk. Both runners execute from different working directories (Unity: project root; dotnet: `Tools/DotnetTests/bin/...`), so locate the repo root by walking up to the directory containing `data/golden/`.
