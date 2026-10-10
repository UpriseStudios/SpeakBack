"""Phase 1 scoring maths, identical to SpeakBack.Core (C#).

Mirrors Assets/_SpeakBack/Scripts/Core/Scoring/{SequenceSimilarity,ScoreMath,Verdict}.cs.
Both implementations must pass data/golden/*.json with identical numbers (D-013).
Pure standard library; no dependencies.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Sequence

EMPTY_TARGET = "EmptyTarget"


class EmptyTargetError(ValueError):
    """Score requested against an empty target: a content error, never a player score."""

    code = EMPTY_TARGET

    def __init__(self) -> None:
        super().__init__("Target token count must be greater than zero (EmptyTarget).")


def edit_distance(target: Sequence[str], observed: Sequence[str]) -> int:
    """Levenshtein distance over tokens; insert, delete and substitute all cost 1."""
    longer, shorter = (target, observed) if len(target) >= len(observed) else (observed, target)
    previous = list(range(len(shorter) + 1))
    for i, token in enumerate(longer, start=1):
        current = [i] + [0] * len(shorter)
        for j, other in enumerate(shorter, start=1):
            current[j] = min(
                previous[j - 1] + (0 if token == other else 1),
                previous[j] + 1,
                current[j - 1] + 1,
            )
        previous = current
    return previous[len(shorter)]


def score(target_count: int, distance: int) -> int:
    """D-012: D >= N ? 0 : (200*(N-D) + N) // (2*N). Integer arithmetic only."""
    if target_count <= 0:
        raise EmptyTargetError()
    if distance < 0:
        raise ValueError("distance must not be negative")
    if distance >= target_count:
        return 0
    return (200 * (target_count - distance) + target_count) // (2 * target_count)


@dataclass(frozen=True)
class VerdictThresholds:
    """Versioned thresholds; Phase 1 is verdict-v1 (90/75/50)."""

    version: str
    perfect: int
    great: int
    close: int

    def __post_init__(self) -> None:
        if not (0 < self.close < self.great < self.perfect <= 100):
            raise ValueError("thresholds must satisfy 0 < close < great < perfect <= 100")


VERDICT_V1 = VerdictThresholds("verdict-v1", perfect=90, great=75, close=50)


def verdict(value: int, thresholds: VerdictThresholds = VERDICT_V1) -> str:
    """Verdict name for a 0-100 score, using the same names as the C# Verdict enum."""
    if not isinstance(value, int) or isinstance(value, bool) or not 0 <= value <= 100:
        raise ValueError(f"score must be an integer 0-100, got {value!r}")
    if value >= thresholds.perfect:
        return "Perfect"
    if value >= thresholds.great:
        return "Great"
    if value >= thresholds.close:
        return "Close"
    return "TryAgain"
