# P03 Report — Scoring core, SBPS and golden vectors

**Date:** 2026-10-10 · **Commit:** the commit titled "P03: scoring core and golden vectors" (see `git log`) · **Prompt:** prompts/P03-scoring-core.md

## Summary
`SpeakBack.Core` now holds deterministic scoring maths and content validation, with no engines:
- token edit distance, the D-012 integer score and versioned verdicts;
- word and character text normalisation;
- the SBPS inventory with ARPAbet mapping, and phoneme normalisation;
- the recognition and scoring contracts, and the `IAttemptScorer` / `ISpeechRecognitionProvider` interfaces;
- a reusable `TokenScorer` with versioned presets;
- a phrase catalogue validator.

The shared golden vectors give **identical results in C# (.NET), C# (Unity Mono) and Python** on all 13 similarity cases, the error case and all verdict cases. The real Phase 1 catalogue validates with 0 errors and 10 draft-phoneme warnings.

## Changes
| Path | Change |
|---|---|
| `Scripts/Core/Content/MiniJson.cs`, `JsonValues.cs` | Strict, dependency-free JSON reader plus typed accessors (Core can't use JsonUtility or packages, and does no file IO) |
| `Scripts/Core/Content/SbpsInventory.cs` | `SbpsInventory.Parse(json)` (44 symbols, duplicate and unknown-mapping checks); `ArpabetMapping` (exact AH0/ER0, otherwise stress digits stripped) |
| `Scripts/Core/Content/PhonemeNormaliser.cs` | Strip stress and diacritic marks (NFD → strip → NFC), drop silence and pad tokens, **keep ː** (Q4) |
| `Scripts/Core/Content/PhraseInfo.cs` | Engine-free phrase view (id, text, phonemes, alternates, content and phoneme versions) |
| `Scripts/Core/Content/PhraseCatalogueValidator.cs` | Catalogue checks → `CatalogueIssue` list (errors and warnings with stable codes) |
| `Scripts/Core/Scoring/SequenceSimilarity.cs` | Levenshtein over tokens, O(n·m) time, O(min) memory |
| `Scripts/Core/Scoring/ScoreMath.cs` | D-012 formula in `long` arithmetic; `EmptyTargetException` (code `EmptyTarget`) |
| `Scripts/Core/Scoring/Verdict.cs` | `Verdict`, `VerdictThresholds` (`verdict-v1` = 90/75/50, validated), `VerdictPolicy` (rejects scores outside 0–100) |
| `Scripts/Core/Scoring/TextNormaliser.cs` | NFKC, invariant lowercase, punctuation and symbols → space, collapse; `Words`, `Characters` (text elements) |
| `Scripts/Core/Scoring/RecognitionResult.cs` | `RecognitionStatus`, `TokenKind`, `RecognizedToken`, `RecognitionResult` (Success/Failure factories), `RecognitionContext` |
| `Scripts/Core/Scoring/ScoringOutcome.cs` | `ScoringStatus`, typed `ScoringFailure`, immutable `ScoringOutcome` (Scored/Failed factories) |
| `Scripts/Core/Scoring/IAttemptScorer.cs` | `IAttemptScorer` (+ `ScorerVersion`) and `ISpeechRecognitionProvider` as in Master §27, using `PhraseInfo` |
| `Scripts/Core/Scoring/TokenScorer.cs` | `ScorerPreset` (`phoneme-ed-v1`, `phoneme-ed-best-v1`, `word-ed-v1`, `char-ed-v1`); `TokenScorer` |
| `Tests/EditMode/Core/TestData.cs` | Finds the repo root from either runner and reads `data/` |
| `Tests/EditMode/Core/Scoring/*`, `Content/*` | 8 new test files |
| `Tools/BakeOff/bakeoff/{__init__,scoring}.py` | Python mirror of the maths (standard library only) |
| `Tools/BakeOff/tests/test_golden.py`, `Tools/BakeOff/README.md` | Golden tests runnable with or without pytest |
| `Scripts/Core/{Scoring,Content}/.gitkeep` | Removed |

## Tests
| Runner | Result | Count |
|---|---|---|
| `dotnet test Tools/DotnetTests` | **Passed** | 223/223 |
| Unity EditMode | **Passed** | 223 passed, 1 skipped (P02's `[Explicit]` perf benchmark) |
| Unity PlayMode | **Passed** | 3/3 |
| Python `Tools/BakeOff/tests/test_golden.py` (3.14.2 and 3.12, no pytest installed) | **Passed** | 5/5 test functions (13 similarity cases, error case, 10 verdict cases + 2 invalid, an exhaustive N ≤ 200 formula check, edit-distance basics) |

### Golden similarity cases: expected vs C# vs Python
Each runner wrote its values in machine-readable form (`GOLDEN|name|distance|score`). The lists were diffed: .NET = Unity = Python on 13/13.

| Case | Expected D | Expected score | C# D | C# score | Python D | Python score |
|---|---|---|---|---|---|---|
| identical_words | 0 | 100 | 0 | 100 | 0 | 100 |
| empty_observed | 2 | 0 | 2 | 0 | 2 | 0 |
| one_word_missing | 1 | 50 | 1 | 50 | 1 | 50 |
| reordered_words | 2 | 0 | 2 | 0 | 2 | 0 |
| five_words_one_substitution | 1 | 80 | 1 | 80 | 1 | 80 |
| insertion_extra_token | 1 | 67 | 1 | 67 | 1 | 67 |
| deletion_one_of_four | 1 | 75 | 1 | 75 | 1 | 75 |
| phoneme_one_substitution_n9 | 1 | 89 | 1 | 89 | 1 | 89 |
| rounding_half_up_n8 | 1 | 88 | 1 | 88 | 1 | 88 |
| many_extras_clamp_zero | 3 | 0 | 3 | 0 | 3 | 0 |
| all_substituted | 3 | 0 | 3 | 0 | 3 | 0 |
| two_of_seven_wrong | 2 | 71 | 2 | 71 | 2 | 71 |
| long_n12_three_errors | 3 | 75 | 3 | 75 | 3 | 75 |
| empty_target (error case) | — | EmptyTarget | — | `EmptyTargetException` | — | `EmptyTargetError` |

Verdict cases: all 10 match in both languages (0/1/49 TryAgain, 50/74 Close, 75/89 Great, 90/99/100 Perfect). −1 and 101 are rejected by both.

**Other coverage:**
- **Phrase validator** on `data/phrases/phase1_phrases.json`: **0 errors, 10 warnings**, all `PhonemesNotReviewed`.
- **Validator error codes**, each tested: duplicate ID, ASCII `:` or ASCII `g` instead of the IPA symbols, count mismatch, unknown difficulty, empty phonemes, phone-set mismatch, missing field, bad alternate, non-integer version, malformed JSON. Unknown-phoneme messages include code points (e.g. `U+0067`).
- **ARPAbet:** `B L UW1 B AH0 N AE1 N AH0` → `b l uː b ə n æ n ə`; AH0/AH1/AH2/ER0/ER1; unknown tokens are reported.
- **Phoneme normaliser:** ˈ ˌ, syllabic, aspiration, precomposed `ã`, silence/pad/`|` dropping; ː kept.
- **Token scorer:** the Phase 1 §12 worked examples (100/80/50/0 for words; 89 Great for the N = 9 phoneme case), an empty observation scoring 0 TryAgain, every failure status giving no score, kind mismatch, empty target, best-of alternates with ties going to the primary, and versions carried through.

## Verified vs UNVERIFIED
- **Verified:**
  - all runners above;
  - three-way golden parity (diffed);
  - Core has no `UnityEngine` usage and no file IO (grep);
  - the real catalogue validates as described.
- **UNVERIFIED:**
  - Unicode normalisation on an Android device. IL2CPP uses the same Mono class library as the editor, so device behaviour should match the editor (see the NFKC finding below), but it hasn't been run on a phone.
  - P01's on-phone checks are still open (no cable).

## Decisions taken in this session
- **Own JSON reader (`MiniJson`).** Core is `noEngineReferences` and dependency-free, so neither JsonUtility nor Newtonsoft is available. The reader is strict: it rejects duplicate keys, trailing commas and single quotes, and tolerates a BOM. It's tested against every file in `data/`.
- **Q6 (alternates):** `TokenScorer` takes the primary plus alternates and keeps the best score, preferring the primary on ties. `phoneme-ed-v1` stays primary-only. A fourth preset, `phoneme-ed-best-v1`, exists so P05 can report the regional-fairness comparison under its own version string. `ScoringOutcome.MatchedTargetIndex` records which pronunciation matched.
- **Q4 (length):** ː is kept. The normaliser strips only `stripMarks` from sbps.json, decomposing first so that marks folded into precomposed letters (e.g. ã) are removed too.
- **`RecognitionContext`** carries phrase ID, locale and requested token kind, and deliberately **not** the target text, so unconstrained recognisers can't be given it by accident (§11.2). A test locks its shape. P05's grammar-control experiment gets its target outside this contract.
- **`IAttemptScorer`** gains a `ScorerVersion` property beyond Master §27, so the host can label outcomes before any are produced.
- **`RecognitionResult` has no "alternatives" field yet.** Master §27 mentions alternatives; I left them out until an engine needs them (P05/P06).
- **Text normalisation** turns apostrophes into spaces, as the spec says (punctuation → space). Both sides are normalised identically, so "that's" ≡ "that's". An ASR that drops apostrophes ("thats") would cost a token. None of the Phase 1 phrases contain apostrophes.
- **No pytest installed.** Python 3.14.2 and 3.12 both lack it, so the golden tests also run as a plain script, as the prompt allows. Nothing was installed.

## Deviations from the spec
None in behaviour. The NFKC finding below is a runtime limitation that I've documented rather than worked around.

## Findings
- **NFKC differs between runtimes.** Unity's Mono class library, which IL2CPP also uses on Android, leaves ligatures like `ﬁ` unexpanded under `Normalize(FormKC)`; .NET and Python expand them. Full-width forms fold correctly in both. The test that caught this was removed from the shared suite and the limitation is documented in `TextNormaliser`. **Impact:** none for English targets and transcripts. P05's Python word/char normaliser must not rely on compatibility folding beyond full-width forms, or parity could break on exotic input.

## Licences and third-party items added
None. Python code uses the standard library only.

## Questions for Jamie
None blocking.

## For the next prompt (P04)
- Scoring entry points:
  - `TokenScorer(ScorerPreset, PhonemeNormaliser).Score(PhraseInfo, RecognitionResult)`;
  - `VerdictPolicy.Default`;
  - `SbpsInventory.Parse(text)` (Runtime supplies the JSON from a TextAsset).
- `PhraseCatalogueValidator.Validate(json, sbps)` is ready for the Q8 data-sync step and P04's runtime phrase loader.
- `ScoringOutcome` and `ProcessDiagnostics` are the audio-free records. Observed tokens are shown only on the result screen and must never be logged.
