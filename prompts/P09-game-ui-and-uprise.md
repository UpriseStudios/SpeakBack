# P09 — Game UI and Uprise presenter

**Stage:** Phase 1 / U3 · **Depends on:** P08 · **Spec:** Phase 1 §4, §13; Master §31–§34; Bible §7.1; D-004, D-005, D-020, D-021

## Prompt

```
Read CLAUDE.md, Phase 1 Build Spec §4 and §13, Master Spec §31 (palette table), §32–§34, Bible §7.1 (Uprise), data/dialogue/uprise_phase1.json, DECISIONS D-004/D-005/D-020/D-021.

Goal: the real Phase 1 game screen — clear, readable, reactive — with Uprise reacting through expression swaps and authored lines. Portrait phones from 360×640 dp to large 20:9.

1. Theme: a UITheme ScriptableObject holding the Master §31 colour tokens and font assets; every UI element reads from it (no hard-coded colours). A contrast check (EditMode test) computing WCAG contrast for text/verdict tokens against bg and surface; AA (4.5:1 body, 3:1 large text) must pass or the test fails with the offending pair.
2. Layout per Phase 1 §4 hierarchy: Header (logo text, challenge 1/10) → HostPanel (Uprise image ~35% height, reaction text bubble) → PromptPanel (target text, "Hear it backwards" button) → RecordingPanel (big round cyan HoldRecordButton ≥ 96 dp, "Hold to speak / Release to flip" label, live amplitude ring, elapsed time) → ProcessingPanel (Flipping…/Analysing…) → ResultPanel (big integer score, verdict text + colour + icon, "Prototype score" label, "Heard after flip:" diagnostic line in small steel text, buttons Replay / Hear what you said / Compare / Retry / Next) → ErrorPanel. Safe areas respected. Large hit targets (≥ 48 dp).
3. Reveal moment (D-021): on ProcessingDone play a ≤ 400 ms "flip" animation (waveform mirrors horizontally) overlapping the start of reveal playback; then score counts up over ~600 ms when shown; reduced-motion setting skips animations.
4. GameView implements IGameView from P08; it only renders state and raises intents. No game rules in UI.
5. HostPresenter: maps expressions (neutral, listening, thinking, restrained_smile, amused, surprised, dry_concern) to sprites from an UpriseExpressionSet ScriptableObject; 120 ms cross-fade; state-driven expressions from the dialogue file (listening while prompt/recording, thinking while processing/scoring). If sprites are missing, use a clean placeholder (simple vector-style shape with a text label of the expression) — never a recognisable copyrighted character.
6. ReactionManager (Core logic + Runtime loader): loads uprise_phase1.json; selection order from its notes (error → validZero → improvement → verdict pool); weighted random with a seeded RNG injected for tests; no line repeated within the last 5 picks of a pool (or pool size − 1). No lore/Cora pools exist or can be selected.
7. Settings sheet (minimal): tap-to-toggle recording (D-020), reduced motion, haptics on/off, text size (normal/large). Persist to a small versioned JSON settings file in persistentDataPath (not PlayerPrefs-as-truth). Haptic tick on record start and on flip if enabled.
8. Strings: all player-facing text comes from data files or a strings table, not code.
9. Tests: ReactionManager selection order, no-repeat, verdict→pool mapping (golden verdict thresholds), expression mapping; UI smoke PlayMode test that every IGameView state renders without exceptions and every button raises the right intent; contrast test.

Prototype art: if Jamie has placed Uprise prototype sprites in Assets/_SpeakBack/Art/Characters/Uprise/, wire them into the expression set (one sprite may be reused for several expressions); otherwise placeholders.

Take screenshots in the editor (Game view at 1080×2340 and 720×1280) of: ready, recording, processing, result for each verdict, error — save to docs/reports/P09-screens/ and link them in the report.

Finish: docs/reports/P09-report.md, commit "P09: game UI and Uprise presenter", stop.
```

## Done when

- [ ] The screenshots look clear and uncluttered on both sizes.
- [ ] Uprise's lines read as dry and warm, never mean. Edit `data/dialogue/uprise_phase1.json` if any line isn't right.
- [ ] Tap-to-toggle and reduced motion work.
