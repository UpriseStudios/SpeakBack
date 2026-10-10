# CLAUDE.md — SpeakBack (Unity)

Rules for every Claude Code session in this repository. Read this file first, every session.

## What this is

SpeakBack is a mobile voice game by Uprise Studios / J M Games. The player hears a phrase **played backwards**, imitates it, the game reverses their recording so they hear it forwards, and scores it against the target. Bots react. Uprise is the host.

Current stage: **Phase 1 (Prove)** — one scene, ten phrases, Uprise text reactions, offline scoring, physical Android device.

## Source of truth (read in this order when a prompt says "read the specs")

1. `docs/DECISIONS.md` — decisions D-001…D-025; these override older wording anywhere.
2. `docs/SPEAKBACK_PHASE_1_BUILD_SPEC.md` — the implementation contract for Phase 1.
3. `docs/SPEAKBACK_UNITY_MASTER_SPEC.md` — product, architecture, canon guardrails.
4. `docs/SPEAKBACK_CHARACTER_EASTER_EGG_BIBLE.md` — character canon (Phase 1 needs §7.1 Uprise only).
5. `data/` — golden vectors, phrases, SBPS phone set, Uprise dialogue.

If documents conflict, stop and report the conflict. Never resolve it silently.

## Working rules

- **One prompt per session.** Do only what the current prompt asks. Do not start the next prompt.
- **Plan first.** Before editing, list the files you will create or change and the tests you will add. Then build.
- **Small, verifiable steps.** Get a narrow slice compiling and tested before widening it.
- **Tests are part of the work.** Pure logic gets tests in `SpeakBack.Core` tests and must pass in both `dotnet test Tools/DotnetTests` and Unity EditMode.
- **Never fake evidence.** No mock scores in player builds, no invented latency numbers, no "should work" claims for device behaviour. If you cannot run something, say so and mark it **UNVERIFIED**.
- **HUMAN STEPs are Jamie's.** Recording people, rating attempts, testing on phones and signing off phonemes or voices are human tasks. Prepare everything for them and then stop. Never simulate their results.
- **Finish every session with:**
  1. all tests run, with results stated;
  2. a report at `docs/reports/PNN-report.md` (template: `prompts/templates/report-template.md`);
  3. a single commit: `PNN: <summary>`;
  4. stop.
- **Stop and ask** (write the question in the report and stop) when: a spec is ambiguous in a way that changes code structure; a dependency's licence is not on the allowed list; a package API differs from the spec's assumptions; a gate fails; or a task would need network services, accounts or data collection.

## Architecture rules

- `Assets/_SpeakBack/Scripts/Core/` → assembly `SpeakBack.Core`, **`noEngineReferences: true`**. Pure C#: audio maths, trimming, reversal, resampling, similarity, verdicts, SBPS, round state rules. No `UnityEngine` usage.
- `Scripts/Runtime/` → `SpeakBack.Runtime`, references Core and Unity. MonoBehaviours, microphone, AudioClips, UI, ScriptableObjects.
- `Scripts/Dev/` → `SpeakBack.Dev`, define constraint `SPEAKBACK_DEV`. Bake-off Recorder and debug tools. Must never compile into release builds.
- `Tools/DotnetTests/` links `Core/**/*.cs` for a fast test loop. Unity EditMode is authoritative.
- `Tools/BakeOff/` is Python 3.11+ and never shipped.
- Namespace root `Uprise.SpeakBack`. Conservative C# that the pinned Unity editor supports.
- **One microphone owner** (`MicrophoneRecorder`). Nothing else touches `Microphone.*`.
- Round identity and cancellation on every async operation; late results are discarded by round ID.
- No singletons with hidden global state; inject through serialized references or constructors.

## Non-negotiables (safety, privacy, canon)

- Offline core. No network calls, analytics, ads, accounts or cloud speech in Phase 1. The Android build must not request `INTERNET` unless a later spec explicitly adds it.
- No audio, transcripts or phonemes written to disk or logs in player builds. Round audio lives in memory until Retry/Next/exit.
- Bake-off recordings come only from consenting adults (D-017), are stored outside the repo, and are never committed.
- Licences (D-018): shipped = MIT/BSD/Apache-2.0/Zlib/OFL/CC0/CC-BY only. GPL/AGPL/non-commercial never ship. Record every third-party item in `ThirdPartyNotices.md`.
- No secrets in the repo, in `Resources/` or in `StreamingAssets/`.
- Scores are never randomised, never altered by characters or purchases, and technical failures are never scored as zero.
- Uprise is witty, warm and dry, never stern or mocking. No Cora, lore or relationship content in Phase 1.
- Do not reproduce real people's likenesses, song lyrics or copyrighted characters in code, data or placeholder art.

## Commands (set `UNITY_PATH` to the pinned editor binary)

```bash
# Fast pure-logic tests
dotnet test Tools/DotnetTests

# Unity EditMode tests
"$UNITY_PATH" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log

# Unity PlayMode tests
"$UNITY_PATH" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log

# Android dev build (created in P01)
"$UNITY_PATH" -batchmode -quit -projectPath . -executeMethod Uprise.SpeakBack.EditorTools.BuildScripts.BuildAndroidDev -logFile Logs/build.log

# Bake-off harness (created in P05)
python -m venv Tools/BakeOff/.venv && Tools/BakeOff/.venv/bin/pip install -r Tools/BakeOff/requirements.txt
Tools/BakeOff/.venv/bin/python -m bakeoff --help
```

If a command fails because the environment lacks Unity, a device or the .NET SDK, report that precisely and continue with what can be verified.

## Style

- British English in player-facing text and docs.
- Clear names over comments; comments explain *why*.
- Every public Core type has a short XML doc summary.
- Player-facing strings live in data assets, not code.
