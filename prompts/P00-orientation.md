# P00 — Orientation and plan check

**Stage:** Phase 1 · **Depends on:** pack copied into repo · **Changes code:** no

## Prompt

```
You are starting the SpeakBack Unity project. This session is read-and-plan only: do not create Unity files or code yet.

1. Read CLAUDE.md, then docs/DECISIONS.md, docs/SPEAKBACK_PHASE_1_BUILD_SPEC.md, docs/SPEAKBACK_UNITY_MASTER_SPEC.md (sections 0–7, 23–37, 41–42) and docs/SPEAKBACK_CHARACTER_EASTER_EGG_BIBLE.md §7.1. Skim everything under data/.

2. Check the environment and report exactly what you find (version strings or "not found"):
   - git and git-lfs
   - $UNITY_PATH and the editor version it points to; whether Android Build Support is installed
   - dotnet SDK version
   - python3 version
   - adb, and whether any device is connected (adb devices)

3. Consistency review. Look for:
   - contradictions between the specs, DECISIONS.md and data/ files
   - anything in the Phase 1 spec that is impossible or risky with Unity 6's Microphone API on Android
   - anything a later prompt will need that no prompt provides
   List each finding with file + section, why it matters, and your recommended resolution. Do not edit the specs.

4. Validate data/:
   - every phrase phoneme is in data/phonemes/sbps.json
   - golden similarity cases are internally consistent with the stated formula (recompute them in a quick throwaway script you do not commit)
   - dialogue pools meet the Phase 1 minimums (6 per verdict, 4 valid-zero, 4 improvement)

5. Write docs/reports/P00-report.md using prompts/templates/report-template.md. Include: environment table, findings, data validation results, and a numbered list of QUESTIONS FOR JAMIE (only things that block P01–P04; give your default for each so Jamie can just say "defaults").

6. Commit only the report: "P00: orientation report". Stop.
```

## Done when

- [ ] Environment table is complete and honest (missing tools say "not found").
- [ ] Findings cite file and section.
- [ ] Data validation passed or lists precise failures.
- [ ] Questions have defaults.
