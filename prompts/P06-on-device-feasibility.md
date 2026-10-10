# P06 — On-device feasibility of the winning scorer

**Stage:** Phase 1 / U2 · **Depends on:** P05 approved · **Spec:** Phase 1 §2 (feasibility deliverable), §3 (model packaging), §11.2; D-019

## Prompt

```
Read CLAUDE.md, docs/scorer-bakeoff.md (use the APPROVED winner; if not approved, stop and say so), Phase 1 Build Spec §2, §3 and §11.2, DECISIONS D-018/D-019.

Goal: prove the approved scorer runs fully offline on Android inside the Unity build, produces the same scores as the Python harness on the same audio, and fits the latency/size targets — before any game UI depends on it.

1. Choose the runtime path for the winner and justify it in docs/dependency-decisions.md: Unity Inference Engine (com.unity.ai.inference) with an ONNX model, sherpa-onnx native plugin, whisper.cpp binding, or another option. Prefer first-party / Apache/MIT, fewest native binaries, ARM64 support. Record versions and licences in ThirdPartyNotices.md.
2. Model packaging: Git LFS; StreamingAssets or install-time asset per D-019; on Android copy/extract to persistentDataPath on first run if the runtime needs a real file path, verify size/hash before init, never download.
3. Implement in SpeakBack.Runtime/Scoring:
   - the ISpeechRecognitionProvider adapter for the engine (one job at a time; worker thread or async inference without blocking the main thread; cancellation by round ID; late results discarded; safe dispose order);
   - an SBPS mapping loaded from the same mapping JSON the harness used (copy it into data/ and Assets as a TextAsset; it must be byte-identical);
   - the IAttemptScorer for the winner using Core TokenScorer (or the timing scorer if that won);
   - ScorerHost MonoBehaviour: InitializeAsync at scene start with a visible "Warming up…" state, then Warm() with one dummy inference.
4. Parity test: put 5 short synthetic or Jamie-provided non-personal test WAVs (Jamie can record himself; never use bake-off participant audio) in Assets/_SpeakBack/Tests/Fixtures (LFS) with their Python harness outputs as JSON. A PlayMode/EditMode test asserts the Unity scorer produces identical tokens and scores (or explains and bounds any numerical difference, e.g. float logits, and still requires identical final scores).
5. Dev-only Feasibility panel (SpeakBack.Dev) in the BakeoffRecorder scene or a new Dev scene: buttons "Score fixtures ×20" and "Record and score"; shows cold init ms, warm p50/p95 ms, peak managed memory, native memory if available, model size on disk, installed APK size; copy-to-clipboard of the results as Markdown.
6. Build the dev APK and state its size. Run what you can on desktop; device numbers are a HUMAN STEP.

If the winner cannot run on Android at acceptable size/latency, document why with evidence and stop, recommending the runner-up for a repeat of this prompt.

Finish: docs/reports/P06-report.md with a table for Jamie to fill (device, cold init, warm p50/p95, memory, APK size), commit "P06: on-device scorer feasibility", stop.
```

## Done when

- [ ] Parity test passes (Unity scores = harness scores on the fixtures).
- [ ] **HUMAN STEP C:** you run the Feasibility panel on each phone and paste the Markdown into `docs/reports/P06-report.md`.
- [ ] Warm p95 ≤ 2 s on your main phone and the APK size is acceptable to you. If not, stop and rerun P06 with the runner-up.
