"""Golden vectors (D-013) for the Python scoring maths.

Runs under pytest, or without it:
    python Tools/BakeOff/tests/test_golden.py            # run all tests
    python Tools/BakeOff/tests/test_golden.py --table    # print name|distance|score per case
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

TOOLS_BAKEOFF = Path(__file__).resolve().parents[1]
REPO_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(TOOLS_BAKEOFF))

from bakeoff.scoring import EMPTY_TARGET, VERDICT_V1, EmptyTargetError, edit_distance, score, verdict  # noqa: E402


def _load(name: str) -> dict:
    return json.loads((REPO_ROOT / "data" / "golden" / name).read_text(encoding="utf-8"))


def golden_rows() -> list[tuple[str, int, int]]:
    rows = []
    for case in _load("similarity_cases.json")["cases"]:
        d = edit_distance(case["target"], case["observed"])
        rows.append((case["name"], d, score(len(case["target"]), d)))
    return rows


def test_similarity_cases() -> None:
    cases = _load("similarity_cases.json")["cases"]
    assert cases
    for case, (name, d, s) in zip(cases, golden_rows()):
        assert d == case["expectedDistance"], name
        assert s == case["expectedScore"], name


def test_error_cases() -> None:
    error_cases = _load("similarity_cases.json")["errorCases"]
    assert error_cases
    for case in error_cases:
        assert case["expectedError"] == EMPTY_TARGET
        try:
            score(len(case["target"]), edit_distance(case["target"], case["observed"]))
        except EmptyTargetError as e:
            assert e.code == EMPTY_TARGET
        else:
            raise AssertionError(f"{case['name']}: expected EmptyTarget")


def test_verdict_cases() -> None:
    data = _load("verdict_cases.json")
    t = data["thresholds"]
    assert (t["Perfect"], t["Great"], t["Close"]) == (VERDICT_V1.perfect, VERDICT_V1.great, VERDICT_V1.close)
    for case in data["cases"]:
        assert verdict(case["score"]) == case["expectedVerdict"], case["score"]
    for invalid in data["invalidScores"]:
        try:
            verdict(invalid)
        except ValueError:
            continue
        raise AssertionError(f"score {invalid} should be rejected")


def test_integer_formula_matches_rounded_ratio() -> None:
    from fractions import Fraction
    import math

    for n in range(1, 201):
        for d in range(0, 2 * n + 1):
            expected = 0 if d >= n else math.floor(Fraction(100 * (n - d), n) + Fraction(1, 2))
            assert score(n, d) == expected, (n, d)


def test_edit_distance_basics() -> None:
    assert edit_distance(list("kiten"), list("siting")) == 3
    assert edit_distance(["tʃ"], ["t", "ʃ"]) == 2
    assert edit_distance([], ["a", "b"]) == 2
    assert edit_distance(["a", "b"], ["b", "a"]) == 2


def _run_without_pytest() -> int:
    if "--table" in sys.argv:
        for name, d, s in golden_rows():
            print(f"GOLDEN|{name}|{d}|{s}")
        return 0
    tests = [obj for name, obj in sorted(globals().items()) if name.startswith("test_") and callable(obj)]
    failures = 0
    for test in tests:
        try:
            test()
            print(f"PASS {test.__name__}")
        except AssertionError as e:
            failures += 1
            print(f"FAIL {test.__name__}: {e}")
    print(f"{len(tests) - failures}/{len(tests)} passed")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(_run_without_pytest())
