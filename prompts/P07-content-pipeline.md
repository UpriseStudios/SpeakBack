# P07 — Content pipeline and phrase assets

**Stage:** Phase 1 / U3 · **Depends on:** P03, P06 · **Spec:** Phase 1 §5 (PhraseDefinition), §10; D-011, D-018

## Prompt

```
Read CLAUDE.md, Phase 1 Build Spec §5 and §10, data/phrases/phase1_phrases.json, data/phonemes/sbps.json and checklists/phoneme-signoff.md.

Goal: a repeatable offline pipeline that turns authorised forward recordings and reviewed phonemes into validated Unity phrase assets, and the Runtime types that load them.

1. Tools/Content (Python 3.11+, never shipped), CLI `python -m content`:
   - `ingest-audio --src <folder>`: for each <phraseId>.forward.wav (any rate/channels): validate, downmix, trim outer silence with the same parameters as Core (port the algorithm; test parity on fixtures), loudness-normalise to a consistent target (e.g. -18 LUFS integrated or peak -1 dBFS — choose, document), write Assets/_SpeakBack/Audio/Phrases/<id>.forward.wav and <id>.reversed.wav (frame-correct reversal), 48 kHz or 44.1 kHz mono 16-bit, plus measured duration back into the phrase JSON.
   - `phonemise --engine arpabet|espeak`: OPTIONAL helper to propose phonemes (CMUdict via a permissively licensed copy if available; espeak-ng only as an external tool whose output is data, per D-018) → SBPS via the mapping tables. Writes proposals to a separate file; never overwrites reviewed phonemes.
   - `validate`: runs the same rules as the C# PhraseCatalogueValidator (port or shell out) plus: audio present for every phrase, durations within 0.5–4.0 s, peak not clipped, reversed is exact reverse of forward. Exits non-zero on errors.
   - `provenance`: requires an audio provenance entry per phrase (who recorded, licence/agreement reference, date). Missing provenance is an error for release, warning for dev.
2. Runtime:
   - PhraseDefinition ScriptableObject (fields per §5 + phonemeStatus, alternates, provenance) and PhraseDatabase ScriptableObject (ordered list, sequential selection with wrap, Retry keeps phrase, Next advances once).
   - Editor importer (SpeakBack.EditorTools): menu "SpeakBack/Import Phrases" reads data/phrases/phase1_phrases.json + audio files and creates/updates assets idempotently by ID (never duplicates; preserves GUIDs).
   - PhraseInfo conversion for Core scorers.
3. Comedy listen support: `python -m content preview --id sb_en_006` plays forward then reversed (or writes a combined WAV) so Jamie can judge how fun each reversal sounds.
4. Tests: Python tests for trim parity, reversal exactness, validator rules; EditMode tests for the importer (idempotency, GUID stability) and PhraseDatabase selection rules.

Use Jamie's forward recordings if present in an input folder he provides (PHRASE_AUDIO_SRC). If not present, generate clearly labelled placeholder tones ONLY for pipeline tests (never imported as phrase audio) and stop after reporting what is missing.

Finish: docs/reports/P07-report.md, commit "P07: content pipeline and phrase assets", stop.
```

## Done when

- [ ] **HUMAN STEP D:** you've recorded (or commissioned with written rights) clean forward audio for all ten phrases, run `ingest-audio`, done the comedy listen and swapped any dull phrase.
- [ ] Phonemes signed off with `checklists/phoneme-signoff.md` and `phonemeStatus` set to `reviewed`.
- [ ] `python -m content validate` passes and "SpeakBack/Import Phrases" creates ten assets.
