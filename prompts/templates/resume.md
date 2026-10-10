# Resume template

Use when a session ran out of room or stopped part-way through a prompt.

## Prompt

```
Resume prompt PNN, which was interrupted. Read CLAUDE.md, prompts/PNN-*.md and any partial docs/reports/PNN-report.md. Check git log and git status to see exactly what was done and committed, and what is uncommitted.

1. List the prompt's tasks, marking each done / partly done / not started, with evidence (files, tests).
2. If there are uncommitted changes, decide whether they are sound (run tests). Keep good work; revert broken partial work rather than building on it, and say which you did.
3. Complete the remaining tasks only. Do not redo finished work or start the next prompt.
4. Finish exactly as the original prompt says (report, single commit, stop).
```
