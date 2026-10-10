# Tools/BakeOff

Offline scorer bake-off harness (D-014). Python 3.11+. Never shipped, and never fed recordings from inside the repo.

## Status

- **P03:** `bakeoff/scoring.py` holds the shared scoring maths (edit distance, the D-012 integer score, verdict-v1). It mirrors `Assets/_SpeakBack/Scripts/Core/Scoring` and must pass the same golden vectors in `data/golden/`.
- **P05:** adds ingest, candidates, metrics and the report.

## Golden tests

These use only the standard library and run with or without pytest:

```bash
python Tools/BakeOff/tests/test_golden.py           # run tests
python Tools/BakeOff/tests/test_golden.py --table   # name|distance|score per golden case
```

On Windows, use `python` or `py -3.12`, not `python3`, which is the Microsoft Store stub. A venv's executables live in `.venv\Scripts\`, not `.venv/bin/`.
