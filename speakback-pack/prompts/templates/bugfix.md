# Bug-fix template

## Prompt

```
Fix a bug in SpeakBack. Read CLAUDE.md first.

Bug: <what happens>
Expected: <what should happen>
Steps to reproduce: <device/editor, steps>
Evidence: <log lines, screenshot path, report reference>
Related prompt/spec: <PNN, spec section>

Process:
1. Reproduce or explain why you cannot, and find the root cause. State it before changing code.
2. Write a failing test that captures the bug (Core, EditMode or PlayMode with fakes). If it can only be shown on a device, say so and write the closest automated test you can.
3. Fix with the smallest change that addresses the root cause. No unrelated refactors.
4. Run all test suites.
5. Write docs/reports/fix-<short-name>.md (cause, fix, tests, what remains UNVERIFIED), commit "fix: <short summary>", stop.
```
