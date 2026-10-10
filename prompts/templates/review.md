# Review template

Use after any prompt when you want a second opinion before moving on. Run in a **fresh** session so the reviewer hasn't seen the work being produced.

## Prompt

```
Review the work from prompt PNN. Do not change any code in this session.

Read CLAUDE.md, docs/DECISIONS.md, the spec sections listed at the top of prompts/PNN-*.md, the prompt itself and docs/reports/PNN-report.md. Then inspect the diff for the PNN commit (git show --stat <hash>, then the files).

Check, in this order:
1. Spec compliance: every task in the prompt done? Any silent deviation?
2. Correctness: logic errors, off-by-one, threading/main-thread violations, async without cancellation, stale round IDs, resource leaks (AudioClips, mic, native handles).
3. Safety/privacy: audio/tokens logged or written, network use, Dev code reachable in release, licences outside D-018.
4. Tests: do they test behaviour that matters, including failure paths? Anything claimed as verified that wasn't run?
5. Simplicity: anything over-built for Phase 1?

Run the test suites to confirm the report's claims.

Write docs/reports/PNN-review.md: findings ranked by severity (Blocker / Major / Minor / Nit), each with file:line, the problem, a concrete failure scenario and a suggested fix. End with "Ready for next prompt: yes/no". Commit "PNN: review" and stop.
```
