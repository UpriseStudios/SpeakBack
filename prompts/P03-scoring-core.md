# P03 — Scoring core, SBPS and golden vectors

**Stage:** Phase 1 / U2 · **Depends on:** P02 · **Spec:** Phase 1 §5, §12; D-011–D-013

## Prompt

```
Read CLAUDE.md, DECISIONS.md D-011 to D-013, Phase 1 Build Spec §5 (RecognitionResult, ScoringOutcome) and §12, and data/golden/*, data/phonemes/sbps.json, data/phrases/phase1_phrases.json.

Goal: deterministic scoring maths and content validation in SpeakBack.Core, proven against the shared golden vectors. No engines yet.

Build in Scripts/Core/Scoring and Scripts/Core/Content:
1. SequenceSimilarity.EditDistance(IReadOnlyList<string> target, IReadOnlyList<string> observed) — Levenshtein over tokens, O(n·m) time, O(min) memory.
2. ScoreMath.Score(int n, int d) — exactly: d >= n ? 0 : (200*(n-d) + n) / (2*n), integer division. Throws/returns typed error for n == 0 (EmptyTarget).
3. Verdict enum {Perfect, Great, Close, TryAgain} + VerdictPolicy with thresholds 90/75/50 from a versioned config object (VerdictThresholds "verdict-v1"); rejects scores outside 0–100.
4. TextNormaliser (word and char modes) per §12: Unicode NFKC, invariant lowercase, punctuation → space, collapse whitespace; char mode removes spaces.
5. Sbps: loads the inventory (from a JSON string passed in — Core must not do file IO; Runtime/Tools supply the text), validates tokens, ArpabetToSbps mapping (strip stress digits except AH0/ER0 handling per sbps.json), PhonemeNormaliser (strip marks, drop silence/pad tokens per sbps.json).
6. Contracts from §5: RecognitionStatus, TokenKind, RecognizedToken, RecognitionResult, ScoringStatus, ScoringOutcome (score, verdict, scorerVersion, target tokens, observed tokens, distance, optional components dictionary, failure code). Immutable.
7. IAttemptScorer and ISpeechRecognitionProvider interfaces as in Master §27, using Core's AudioBuffer and a PhraseInfo record (id, targetText, phonemes, version) instead of a ScriptableObject so Core stays engine-free.
8. TokenScorer — a reusable helper: given target tokens, a RecognitionResult and a scorer version, produce a ScoringOutcome (empty successful observation → score 0; failed recognition → no score, typed failure). Three named presets: phoneme-ed-v1, word-ed-v1, char-ed-v1.
9. PhraseCatalogueValidator: given phrase JSON text + SBPS → list of errors (missing fields, duplicate IDs, unknown phonemes, empty phonemes, phonemeStatus not 'reviewed' reported as a WARNING not an error).

Golden tests:
- A test fixture reads data/golden/similarity_cases.json and verdict_cases.json from disk (test projects may do file IO; locate the repo root robustly in both runners). Every case must pass: distance and score, error case, verdicts and invalid scores.
- Add a test that runs the validator on data/phrases/phase1_phrases.json: zero errors, warnings for draft phonemes.
- Unit tests for normalisers and ARPAbet mapping (e.g. "B L UW1 B AH0 N AE1 N AH0" → b l uː b ə n æ n ə).

Also create Tools/BakeOff/bakeoff/scoring.py now (pure Python, no dependencies) implementing the identical edit distance, ScoreMath and verdict policy, plus Tools/BakeOff/tests/test_golden.py that runs the same golden files. Run it with python3 -m pytest if pytest is available, else a plain python runner you include.

Run all three test runners. Finish: docs/reports/P03-report.md (include a table: case name → C# score → Python score), commit "P03: scoring core and golden vectors", stop.
```

## Done when

- [ ] C# and Python produce identical results for every golden case (table in the report).
- [ ] Phrase validator passes with draft-phoneme warnings only.
- [ ] Core still has no UnityEngine references.
