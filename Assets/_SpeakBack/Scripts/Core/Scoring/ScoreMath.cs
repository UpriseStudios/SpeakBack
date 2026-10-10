using System;

namespace Uprise.SpeakBack.Core.Scoring
{
    /// <summary>Thrown when a score is requested against an empty target: a content error, never a player score.</summary>
    public sealed class EmptyTargetException : ArgumentException
    {
        /// <summary>Error code shared with the golden vectors and the Python harness.</summary>
        public const string Code = "EmptyTarget";

        /// <summary>Creates the exception.</summary>
        public EmptyTargetException()
            : base("Target token count must be greater than zero (EmptyTarget).", "targetCount")
        {
        }
    }

    /// <summary>
    /// Phase 1 score formula (§12, D-012) in integer arithmetic so C# and Python agree exactly:
    /// score = D ≥ N ? 0 : (200·(N − D) + N) / (2·N), integer division. Equivalent to
    /// floor(100·(1 − D/N) + 0.5) without floating-point drift.
    /// </summary>
    public static class ScoreMath
    {
        /// <summary>Score 0–100 for <paramref name="distance"/> edits against <paramref name="targetCount"/> target tokens.</summary>
        public static int Score(int targetCount, int distance)
        {
            if (targetCount <= 0) throw new EmptyTargetException();
            if (distance < 0) throw new ArgumentOutOfRangeException(nameof(distance), distance, "Must not be negative.");
            if (distance >= targetCount) return 0;

            long n = targetCount;
            long d = distance;
            return (int)((200 * (n - d) + n) / (2 * n));
        }
    }
}
