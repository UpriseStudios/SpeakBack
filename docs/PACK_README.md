# SpeakBack — Spec & Claude Code Prompt Pack

**Version:** 1.0 · 10 October 2026 · Uprise Studios / J M Games

This pack turns the SpeakBack specs into a build you can run step by step with Claude Code. It covers **Phase 1 (Prove)** in detail and outlines what comes after.

## What's inside

| Path | What it is |
|---|---|
| `CLAUDE.md` | Rules Claude Code reads every session. Goes in the repo root. |
| `docs/SPEAKBACK_UNITY_MASTER_SPEC.md` | Product authority (v2.1) |
| `docs/SPEAKBACK_PHASE_1_BUILD_SPEC.md` | Phase 1 implementation contract (v2.1) |
| `docs/SPEAKBACK_CHARACTER_EASTER_EGG_BIBLE.md` | Character canon (v1.2) |
| `docs/DECISIONS.md` | Decisions D-001 to D-025, made while assembling this pack |
| `docs/ART_BRIEF.md` | Brief for the artist doing final character art |
| `data/golden/` | Golden test vectors for score maths and verdicts (C# and Python must both pass) |
| `data/phrases/phase1_phrases.json` | The ten Phase 1 phrases with **draft** phonemes |
| `data/phonemes/sbps.json` | SpeakBack Phone Set (44 British English symbols) and ARPAbet mapping |
| `data/dialogue/uprise_phase1.json` | Uprise onboarding, reactions and error lines |
| `prompts/P00`–`P14` | The Phase 1 prompts, in order |
| `prompts/P15-post-gate-roadmap.md` | Outline prompts for Phase 2 onwards |
| `prompts/templates/` | Report, review, bug-fix and resume templates |
| `checklists/` | Your human steps: consent, rating, device matrix, fun tests, phoneme sign-off |

## Set-up (once)

1. Create an empty git repo (`speakback`) and turn on Git LFS.
2. Copy `CLAUDE.md`, `docs/` and `data/` from this pack into the repo root. Keep `prompts/` and `checklists/` alongside, or in a `pack/` folder.
3. Install Unity Hub and the Unity 6 LTS editor with Android Build Support (SDK, NDK, OpenJDK).
4. Install the .NET 8 SDK (for the fast test loop) and Python 3.11+ (for the bake-off harness).
5. Set `UNITY_PATH` to the editor binary, for example in your shell profile.
6. Have at least one Android phone with developer mode and USB debugging on. Two or three is better.

## How to run each prompt

1. Start a fresh Claude Code session in the repo (`/clear` between prompts).
2. Paste the block under **Prompt** from the next `prompts/PNN-*.md` file.
3. Let it plan, build and test. It ends by writing `docs/reports/PNN-report.md`, making one commit and stopping.
4. Read the report. Check the **Done when** list in the prompt file yourself.
5. If something is wrong, use `prompts/templates/bugfix.md` or `review.md` before moving on.
6. Do any **HUMAN STEP** listed before the next prompt.

## Sequence and human steps

| # | Prompt | Your part afterwards |
|---|---|---|
| P00 | Orientation and plan check | Read its questions; answer or accept defaults |
| P01 | Unity project scaffold | Open the project once in the editor; confirm it builds to your phone |
| P02 | Pure audio core | — |
| P03 | Scoring core and golden vectors | — |
| P04 | Capture service and Bake-off Recorder | **HUMAN STEP A:** recruit 5+ adults, get consent, record 40–60 attempts (`checklists/consent-form.md`, `bakeoff-session-script.md`) |
| — | | **HUMAN STEP B:** 3+ raters score attempts blind (`checklists/rating-sheet.md`) |
| P05 | Bake-off harness and candidate scorers | Review `docs/scorer-bakeoff.md`; approve the winner |
| P06 | On-device feasibility of the winner | **HUMAN STEP C:** run the APK on your phones; paste the timings into the report |
| P07 | Content pipeline and phrase assets | **HUMAN STEP D:** record or commission the ten forward prompts; sign off phonemes (`checklists/phoneme-signoff.md`) |
| P08 | Round controller and state machine | — |
| P09 | Game UI and Uprise presenter | Drop in prototype Uprise art (or keep placeholders) |
| P10 | Scoring integration | — |
| P11 | Polish, accessibility and onboarding | — |
| P12 | Android hardening | **HUMAN STEP E:** run `checklists/device-matrix.md` |
| P13 | Fun-test build and kit | **HUMAN STEP F:** run solo and group fun tests (`checklists/fun-test-script.md`) |
| P14 | Phase 1 gate report | Make the pass/revise/stop call |

## Decisions I made for you

These are recorded in `docs/DECISIONS.md` and can be reversed. The most important:

- **Palette:** dark steel with cyan as the action colour. Orange is kept for the logo, so it never competes with the orange "Close" verdict.
- **Professor Quibble** replaces the Einstein working name, as an original word-obsessed inventor bot.
- **Score maths uses integers**, so the C# game and the Python bake-off produce identical numbers. Shared golden vectors prove it.
- **SBPS phone set:** phrases are stored in one British phoneme inventory, independent of whichever recogniser wins.
- **Engine-free core assembly** plus a `dotnet test` loop, so Claude Code can iterate in seconds without opening Unity.
- **Dev-only recorder scene** collects bake-off data through the real capture path.
- **Adults only** for recordings and tests.
- **Party mode moves up** to Phase 2 (provisional, depending on your group test).

## Things Claude Code cannot do for you

- Record or rate people, or test on your phones.
- Choose voices or sign contracts.
- Decide whether the game is fun.

The prompts are written so it prepares everything for these steps and then stops, rather than guessing.
