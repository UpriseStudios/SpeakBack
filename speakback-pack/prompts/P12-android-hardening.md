# P12 — Android hardening

**Stage:** Phase 1 / U3 · **Depends on:** P11 · **Spec:** Phase 1 §8 (Android processing), §17 (device matrix), §18; Master §35

## Prompt

```
Read CLAUDE.md, Phase 1 Build Spec §8, §17 and §18, Master §35, checklists/device-matrix.md.

Goal: make the build robust on real phones and give Jamie the tools to measure it.

1. Lifecycle hardening: audit every path for OnApplicationPause/Focus, incoming call/audio focus loss, Bluetooth headset connect/disconnect mid-round, screen lock during recording, low-memory warning (Application.lowMemory). Each must leave the app in ReadyToRecord or a clear RecoverableError, with mic ended and clips freed. Add tests with fakes where possible; list what only a device can prove.
2. Audio routing: verify prompt/reveal playback works on speaker, wired and Bluetooth; detect when output is Bluetooth (if detectable) and show a one-time tip that Bluetooth adds delay. Do not attempt native audio routing changes in Phase 1.
3. Android input processing check (§8): Unity cannot switch the capture source, so measure its effects instead. In the Dev menu add "Input check": the operator records 3 s of room silence, then the phrase sb_en_001 spoken normally, then the same phrase very quietly. Report per take: noise floor, RMS envelope summary, peak and clipping ratio, signs of automatic gain (silence-level drift or pumping after speech), signs of a noise gate (runs of exact zeros or abrupt tail cut-off), and the scorer result. Copy a Markdown snippet for the device matrix. Do not build a native capture plugin.
4. Performance: Dev overlay (SPEAKBACK_DEV) showing fps, GC allocations per frame, managed memory, active AudioClip count, scorer queue length. Fix any per-frame allocations in gameplay scripts. 20-minute soak mode in Dev: plays rounds automatically with the fake mic backend replaying a fixture, then reports memory drift, clip count and any errors.
5. Latency instrumentation: measure capture-stop → reveal-start (target p95 ≤ 300 ms) and warm scoring time (target p95 ≤ 2 s) with Stopwatch timestamps; Dev panel shows p50/p95 over the session and copies Markdown.
6. Release configuration: IL2CPP, ARM64, managed stripping level tested (start Medium; ensure link.xml preserves anything the scorer runtime needs), development console off in release, logs minimal in release (Debug.unityLogger.filterLogType = Warning or a define), app icon placeholder, version 0.1.0 with versionCode scheme documented.
7. Build both APKs, record sizes, and run all automated tests.

Write checklists/device-matrix results template entries for Jamie (one block per device) in the report.

Finish: docs/reports/P12-report.md, commit "P12: Android hardening", stop.
```

## Done when — HUMAN STEP E

- [ ] You run `checklists/device-matrix.md` on 2–3 phones and paste results (from the Dev panels) into `docs/reports/P12-report.md`.
- [ ] 50 consecutive rounds per device with no stuck state.
- [ ] Capture-stop → reveal p95 ≤ 300 ms and warm scoring p95 ≤ 2 s on your main phone.
