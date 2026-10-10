# P10 — Scoring integration

**Stage:** Phase 1 / U2–U3 · **Depends on:** P06, P08, P09 · **Spec:** Phase 1 §11.2, §12, §14, §15; Master §6

## Prompt

```
Read CLAUDE.md, Master Spec §6, Phase 1 Build Spec §11.2, §12, §14 and §15, docs/scorer-bakeoff.md (approved winner) and docs/reports/P06-report.md.

Goal: the approved real scorer drives every round in both dev and release builds; failures are handled honestly; nothing personal leaks.

1. Wire ScorerHost (P06) into ChallengeController via IAttemptScorer. Remove any DevScorer path from release builds (compile-time check). Dev builds may keep a "force fake scorer" toggle in the dev menu only.
2. Startup: scorer initialises during LoadingChallenge with a visible warm-up state; if init fails, show a clear error with Retry (no gameplay without scoring — do not fall back to fake scores).
3. Deadlines: 5 s evaluation deadline per attempt; one automatic retry on recoverable failure; then unscored "scrambled" result. A timed-out native job is not cancelled by the timeout alone — the adapter must refuse new jobs until the old one finishes or resets; prove it with a test.
4. Scoring record: each ScoringOutcome carries scorerVersion, modelVersion, phrase contentVersion and verdict-policy version. Keep a session-only in-memory list of numeric outcomes (no tokens, no audio) for the dev diagnostics panel; cleared on exit.
5. Diagnostic line: show observed tokens (SBPS phonemes or words) only on the result screen as "Heard after flip: …", with a dev setting to hide it. Never logged.
6. Privacy audit (write findings into the report):
   - grep the codebase for Debug.Log calls that could print tokens, transcripts or audio; remove or guard them;
   - confirm no file writes of audio/tokens in release (search for File.*, FileStream, WAV writer usage outside Dev);
   - confirm merged release manifest has no INTERNET permission;
   - confirm crash/exception messages shown to players are typed, not raw exception text.
7. Tests: PlayMode end-to-end with a real-scorer fixture WAV injected through a fake microphone backend → expected golden score; timeout + retry path; init failure path; Next during scoring; release-build compile check that Dev types are absent.
8. Build dev and release APKs; record sizes.

Finish: docs/reports/P10-report.md, commit "P10: scoring integration", stop.
```

## Done when

- [ ] Playing on your phone, scores feel plausible and the "Heard after flip" line matches what you said.
- [ ] Airplane mode on a fresh install still scores.
- [ ] Privacy audit section in the report is clean.
