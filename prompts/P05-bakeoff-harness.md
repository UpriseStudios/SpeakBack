# P05 — Bake-off harness and candidate scorers

**Stage:** Phase 1 / U2 · **Depends on:** P03, HUMAN STEPS A+B · **Spec:** Phase 1 §11, §12, §18 (agreement, fairness, spread); D-011, D-014, D-018

## Prompt

```
Read CLAUDE.md, DECISIONS D-011/D-014/D-018, Phase 1 Build Spec §11–§12 and §18, data/phonemes/sbps.json and Tools/BakeOff/bakeoff/scoring.py from P03.

Inputs (outside the repo — never copy recordings into the repo or commit them):
  BAKEOFF_DATA=<path to folder of *.flipped16k.wav + *.json>   (Jamie supplies)
  BAKEOFF_RATINGS=<path to ratings.csv>                         (format: checklists/rating-sheet.md)
  BAKEOFF_REFS=<optional folder of <phraseId>.forward.wav>
If these are not provided or are empty, build and test everything against a tiny SYNTHETIC fixture you generate (sine/noise WAVs + fake ratings, clearly labelled synthetic), produce the report with "NO REAL DATA — results meaningless", and stop before choosing a winner.

Goal: an offline Python harness that runs every candidate scorer over the same rated recordings, measures agreement with humans, fairness and practicality, and writes docs/scorer-bakeoff.md with a recommendation for Jamie to approve.

1. Harness (Tools/BakeOff, package `bakeoff`, CLI `python -m bakeoff`):
   - `ingest`: load WAV+JSON sidecars and ratings.csv; validate (16 kHz mono, phrase IDs exist, ≥3 ratings per take, consent ref present); compute median human label (good=2, partial=1, unintelligible=0) and median 0–10 closeness when given.
   - `rating-list --rater R1`: a seeded, shuffled per-rater list of take IDs and target phrases (for checklists/rating-sheet.md). It may be run before ratings exist.
   - `run --candidate <name>`: score every take; cache results as JSONL in an output dir outside the repo (default ~/speakback-bakeoff/out).
   - `report`: compute metrics and write docs/scorer-bakeoff.md (aggregate numbers only; no transcripts of individuals, no speaker codes).
   - Deterministic split: 70% fit / 30% held-out, stratified by phrase, seeded; ALL headline metrics on held-out only.
2. Candidates (each a module implementing `score(wav_path, phrase) -> {status, tokens, kind, score, latency_ms, engine, model, version}` using scoring.py for the maths):
   a. phoneme-ed-v1: a CTC phoneme recogniser with NO word language model. Research current options (e.g. wav2vec2/XLSR espeak-phoneme CTC checkpoints, smaller phoneme CTC models, sherpa-onnx models with phone outputs). For each option record: licence of code AND weights, size, whether an ONNX export exists or is feasible, and whether it could plausibly run on a mid-range phone. Pick the best 1–2 that pass the licence policy (D-018). Build a mapping table <model phone set> → SBPS in Tools/BakeOff/mappings/<model>.json with every unmapped symbol listed.
   b. word-ed-v1 and char-ed-v1: an unconstrained word ASR (e.g. Whisper tiny/base via faster-whisper or whisper.cpp bindings, and/or a sherpa-onnx English model). Same licence checks.
   c. grammar-control: the same word ASR constrained to the target phrase plus an unknown/garbage token, if the engine supports it. Control only — never eligible to win unless it wins fairly on held-out data.
   d. timing-dtw-v1: MFCC (13 coeffs + deltas) DTW distance between ScorerInput and the forward reference (BAKEOFF_REFS), mapped to 0–100 by a monotonic function fitted on the fit split only.
   e. vosk-baseline: Vosk small English model, unconstrained.
   Download models into a cache outside the repo. Record exact versions/hashes. If a candidate cannot be installed in this environment, record why and continue with the others.
3. Metrics per candidate (held-out): Spearman ρ vs median human label (and vs closeness if present) with a bootstrap 95% CI; obvious inversions (same phrase, human good scored below human unintelligible); % of human "partial" takes scoring 0; score spread (IQR) on two-word phrases; fairness gap = max difference in mean score between speaker groups for takes with the same human label (report per label, and flag any >10 points); hallucination check for word ASR (% of human-unintelligible takes that produce a non-empty transcript scoring ≥50); latency p50/p95 on this machine; model size.
4. Optional combined scorer: only if the best single candidate misses ρ ≥ 0.6, fit a non-negative linear combination on the fit split and evaluate on held-out. Report honestly whether it helps.
5. docs/scorer-bakeoff.md: protocol, dataset summary (counts by phrase/group/device — no identities), results table, fairness table, per-candidate licence/size/latency, failures, recommendation with reasoning, and the runner-up. Mark the decision "PENDING JAMIE APPROVAL".
6. Tests: harness unit tests (ingest validation, split determinism, Spearman/inversion/fairness calculations on hand-made fixtures, mapping table coverage), plus the P03 golden test still passing.

Never commit recordings, model weights or caches. Add the output/cache paths to .gitignore if they could ever fall inside the repo.

Finish: docs/reports/P05-report.md, commit "P05: bake-off harness and results", stop.
```

## Done when

- [ ] `docs/scorer-bakeoff.md` exists with held-out metrics, a fairness table and a clear recommendation.
- [ ] No audio, weights or individual transcripts are in git (`git status` is clean of them).
- [ ] You approve the winner by editing the decision line to `APPROVED by Jamie <date>` (and add a D-026 entry to `DECISIONS.md`).

If no candidate reaches ρ ≥ 0.6 with acceptable fairness, do **not** continue to P06. Follow Phase 1 §19: run `prompts/templates/review.md` on the bake-off and consider more data, weighted phoneme costs or a hybrid.
