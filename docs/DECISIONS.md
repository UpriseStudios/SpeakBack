# SpeakBack — Decision Log

**Date:** 10 October 2026  
**Owner:** Jamie (Uprise Studios / J M Games)  
**Scope:** Decisions taken while assembling the Claude Code build pack. Each is applied to the specs and can be reversed by Jamie. Reversing one means updating this log and the affected spec section in the same change.

Status values: **Decided** (build against it), **Provisional** (build against it and confirm by the named point), **Deferred** (not needed before the named point).

| ID | Decision | Status | Affects |
|---|---|---|---|
| D-001 | Unity 6 LTS line, Universal 2D template; the exact patch is pinned in P01 and recorded in `dependency-decisions.md` | Provisional (confirm at P06 feasibility) | Phase 1 §2 |
| D-002 | Android minimum API 26 (Android 8.0); target API = whatever Google Play currently requires, checked at P01 | Decided | Phase 1 §2 |
| D-003 | Application ID `com.uprisestudios.speakback` | Provisional (confirm before first store upload) | P01 |
| D-004 | Palette: dark steel base, **cyan as primary interactive colour**, orange reserved for brand marks (logo, title). Verdict colours stay gold/green/orange/red and appear only on result elements, always with text | Decided | Master §31 |
| D-005 | Typography: Baloo 2 (display) and Nunito (body/UI), both SIL Open Font Licence | Decided | Master §31 |
| D-006 | Genius bot renamed **Professor Quibble**: an original word-obsessed inventor bot. No real-person likeness, hair, moustache or famous equations. Still unlocks Genius Lab | Decided | Bible §10.1, Art brief |
| D-007 | Villain uses a neutral, non-regional theatrical voice | Decided | Bible §11.2 |
| D-008 | DJ unlock: **Rhythm Set** — score Great or better on 15 rhythm-tagged phrases. Sharing is never rewarded | Decided | Bible §10.5 |
| D-009 | Samurai stays as written in the Bible. The ninja prototype art is retired; the artist designs a respectful samurai-inspired bot from scratch | Decided | Art brief |
| D-010 | Prototype art mapping: visor bot → Uprise, pink bot → Cora mood reference, rusty bot → ClumsyBot reference, sparking orange bot → ChaosCore reference. The gamer and clown bots have no role yet and are parked | Decided | Art brief |
| D-011 | Canonical phoneme inventory is the **SpeakBack Phone Set (SBPS)**: 44 British English IPA symbols in `data/phonemes/sbps.json`. Content is stored in SBPS; each engine gets a mapping table. This decouples phrase content from whichever recogniser wins | Decided | Phase 1 §5, §10, §12 |
| D-012 | Score maths uses **integer arithmetic** so C# and Python agree exactly: `score = D >= N ? 0 : (200*(N-D) + N) / (2*N)` (integer division). Same result as `floor(100*(1-D/N) + 0.5)` without float drift | Decided | Phase 1 §12 |
| D-013 | Shared golden vectors in `data/golden/similarity_cases.json` and `verdict_cases.json`. The C# runtime and the Python bake-off harness must both pass them | Decided | Phase 1 §17 |
| D-014 | Bake-off harness in Python 3.11+ under `Tools/BakeOff` (where the ML tooling lives); it is never shipped | Decided | Phase 1 §11 |
| D-015 | Pure logic (audio maths, similarity, verdicts, state machine rules) lives in a `SpeakBack.Core` assembly with `noEngineReferences: true`. A `Tools/DotnetTests` project compiles the same files for a fast test loop outside Unity. Unity Test Framework stays authoritative | Decided | Phase 1 §3, §17 |
| D-016 | A **dev-only Bake-off Recorder scene** collects consented attempts on real phones through the real capture path. It is compiled only with the `SPEAKBACK_DEV` define and excluded from release builds | Decided | Phase 1 §11, P04 |
| D-017 | Bake-off and fun-test participants are **adults (18+) only**, with written consent. No recordings from minors at any stage | Decided | Phase 1 §15, checklists |
| D-018 | Licence policy for anything shipped: MIT, BSD, Apache-2.0, Zlib, OFL, CC0, CC-BY allowed. GPL, AGPL, LGPL-static and any non-commercial licence are never shipped. GPL tools (e.g. espeak-ng) may be used offline in `Tools/` when only their data output is kept, with that recorded in `ThirdPartyNotices.md` | Decided | Phase 1 §2, §10 |
| D-019 | Models and phrase audio stored with Git LFS. The model is bundled in the build (install-time asset pack if needed); there are no first-launch downloads | Decided | Phase 1 §3 |
| D-020 | Phase 1 recording: **hold-to-record by default plus a tap-to-toggle accessibility setting**, both built in Phase 1 | Decided | Phase 1 §6 |
| D-021 | Reveal timing: flip animation ≤ 400 ms, overlapping the start of reveal playback, so it adds no latency | Decided | Phase 1 §14 |
| D-022 | First-launch onboarding in Phase 1 is three Uprise text cards plus a guided first round on phrase sb_en_001. It can be skipped and is shown once per install (stored in a tiny settings file, not PlayerPrefs-as-truth) | Decided | Phase 1 §13 |
| D-023 | Post-gate order: **Phase 2 = rest of U3 + U4 + U9 Party (moved up)**, because Party is the strongest likely social context. Cora (U6) still waits for the bot family (U5) | Provisional (confirm with Phase 1 group fun results) | Master §37 |
| D-024 | Audience: general audience, not designed for children; plan for a 13+ store rating. No ads, analytics or accounts until a dedicated child-safety review | Provisional (confirm before store submission) | Master §29 |
| D-025 | Claude Code works one prompt per session. Each prompt ends with a written report in `docs/reports/`, a commit and a stop. Human-only steps are marked **HUMAN STEP** and Claude Code must not fake them | Decided | prompts/ |

## Still open (Jamie's call)

- Final prices and bundle SKUs (needed before U11).
- Voice casting for Uprise and Cora, including contracts (needed before voiced dialogue in U4/U6).
- Decode mode (parked until after launch).
- Ranking/social backend (not before score reliability is proven).
- Which recogniser wins (decided by the bake-off, not by opinion).
