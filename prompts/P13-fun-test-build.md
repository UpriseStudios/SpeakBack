# P13 — Fun-test build and kit

**Stage:** Phase 1 · **Depends on:** P12 · **Spec:** Phase 1 §18 (fun and credibility gates), §15; D-017

## Prompt

```
Read CLAUDE.md, Phase 1 Build Spec §15 and §18, DECISIONS D-017, checklists/fun-test-script.md.

Goal: a clean test build and the minimal, privacy-safe tooling Jamie needs to run solo and pass-the-phone fun tests and judge score credibility.

1. "Playtest" build flavour: release-like (no dev overlay, no recorder) but with SPEAKBACK_PLAYTEST define that enables ONLY:
   - a session counter screen reachable by a hidden gesture (e.g. 5 taps on the logo) showing numeric, anonymous session stats held in memory: rounds played, retries, nexts, Hear Raw / Compare / Replay taps, verdict counts, average score, session length, how many extra rounds a player chose after the guided round;
   - a "Reset session" button;
   - no audio, tokens or phrase text in the stats; nothing written to disk; nothing sent anywhere.
   Add a build script method BuildAndroidPlaytest.
2. Pass-the-phone support without new modes: a "Next player" button on the result screen in Playtest builds that resets per-player counters and shows "Pass to the next player" — this is all Party needs for testing.
3. Verify the guided first round, all ten phrases and all error states work in the Playtest build.
4. Update checklists/fun-test-script.md only if the build changes something it describes (keep Jamie's observation sheet format).
5. Write docs/phase-1-validation.md skeleton: sections for each §18 gate with fields for evidence, numbers, devices, sample counts and pass/fail; pre-fill the gates that automated tests and earlier reports already evidence, citing report files.

Finish: docs/reports/P13-report.md, commit "P13: fun-test build and kit", stop.
```

## Done when — HUMAN STEP F

- [ ] Solo tests: 5+ new adult testers, observed with `checklists/fun-test-script.md`.
- [ ] Group test: at least one pass-the-phone session with 3+ adults.
- [ ] Fairness questions asked, and every "that felt unfair" moment noted (phrase, verdict, what they expected).
- [ ] Observations typed into `docs/phase-1-validation.md`.
