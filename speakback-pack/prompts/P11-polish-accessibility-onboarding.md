# P11 — Polish, accessibility and onboarding

**Stage:** Phase 1 / U3 · **Depends on:** P10 · **Spec:** Phase 1 §13; Master §28, §33, §34; D-021, D-022

## Prompt

```
Read CLAUDE.md, Phase 1 Build Spec §13, Master Spec §28 (permissions), §33 (audio/SFX), §34 (accessibility), DECISIONS D-021/D-022, data/dialogue/uprise_phase1.json (onboarding).

Goal: first-time players understand the game in one guided round, the reveal feels good, and the screen is accessible.

1. Onboarding (D-022): three Uprise cards from the onboarding lines, then a guided first round on sb_en_001 with coach marks ("Tap to hear it backwards" → "Hold and copy the sounds" → "Let go — here's what you said"). Skippable; shown once per install via the settings file; replayable from Settings.
2. Microphone permission UX: in-game explanation card BEFORE the OS prompt (only when first needed); denial → explanation + Retry; permanent denial → Settings button (Android app settings intent); returning from Settings re-checks.
3. Audio feel: short original UI SFX (generate procedurally or create simple synthesised sounds yourself; no third-party samples unless CC0 with source recorded): record start tick, record stop, flip swoosh, score count tick, verdict stingers (Perfect sparkle, Great chime, Close blip, Try Again playful boing — never a harsh fail buzzer). Volume settings: master / voice (prompts and reveal) / effects. Prompt and reveal audio are never ducked by SFX.
4. Accessibility pass: TalkBack-friendly labels where Unity allows (at minimum, visible text labels on every icon button and state text that announces "Recording", "Flipping", score and verdict as text); large-text mode re-flows without clipping; colour never the only signal (verdict icon + text); reduced motion; haptics toggle; minimum 48 dp targets; check focus order for external keyboard if feasible. Document what Unity cannot provide for screen readers in Phase 1.
5. Microcopy review: all player-facing strings in British English, short, kind; error lines never blame the player for technical faults.
6. Visual polish within the theme: amplitude ring responsiveness (≥ 30 fps update, smoothed), score count-up, subtle Uprise idle bob (disabled by reduced motion). Keep 60 fps on a mid-range phone; no per-frame allocations in Update (verify with the Profiler/GC alloc test where possible).
7. Tests: onboarding shown once/skip/replay; permission flow state machine with a fake permission backend (granted, denied, permanently denied, returning from settings); settings persistence + migration test (v1 file → current); large-text layout smoke test.

Screenshots of onboarding, permission explanation, large-text result → docs/reports/P11-screens/.

Finish: docs/reports/P11-report.md, commit "P11: polish, accessibility and onboarding", stop.
```

## Done when

- [ ] Someone who has never seen the game understands it after the guided round without you explaining.
- [ ] Denying the mic, then allowing it from Settings, works.
- [ ] Large text and reduced motion look right.
