using System;

namespace Uprise.SpeakBack.Core.Scoring
{
    /// <summary>The four result verdicts (Master §6.4). Always shown with the number and text, never colour alone.</summary>
    public enum Verdict
    {
        /// <summary>Score below the Close threshold.</summary>
        TryAgain,

        /// <summary>Close threshold up to Great.</summary>
        Close,

        /// <summary>Great threshold up to Perfect.</summary>
        Great,

        /// <summary>Perfect threshold and above.</summary>
        Perfect,
    }

    /// <summary>
    /// Versioned verdict thresholds. Thresholds may be retuned after calibration (Master §6.4), so each
    /// set carries a version that is recorded with every scored outcome.
    /// </summary>
    public sealed class VerdictThresholds
    {
        /// <summary>Phase 1 thresholds: Perfect ≥ 90, Great ≥ 75, Close ≥ 50 (§7, §12).</summary>
        public static readonly VerdictThresholds V1 = new VerdictThresholds("verdict-v1", 90, 75, 50);

        /// <summary>Creates thresholds; requires 0 &lt; close &lt; great &lt; perfect ≤ 100.</summary>
        public VerdictThresholds(string version, int perfect, int great, int close)
        {
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Version is required.", nameof(version));
            if (!(0 < close && close < great && great < perfect && perfect <= 100))
            {
                throw new ArgumentException($"Thresholds must satisfy 0 < close < great < perfect <= 100 (got {close}/{great}/{perfect}).");
            }

            Version = version;
            Perfect = perfect;
            Great = great;
            Close = close;
        }

        /// <summary>Version recorded with each outcome, e.g. "verdict-v1".</summary>
        public string Version { get; }

        /// <summary>Lowest Perfect score.</summary>
        public int Perfect { get; }

        /// <summary>Lowest Great score.</summary>
        public int Great { get; }

        /// <summary>Lowest Close score.</summary>
        public int Close { get; }
    }

    /// <summary>Maps a 0–100 score to a <see cref="Verdict"/> using one set of thresholds.</summary>
    public sealed class VerdictPolicy
    {
        /// <summary>Policy with <see cref="VerdictThresholds.V1"/>.</summary>
        public static readonly VerdictPolicy Default = new VerdictPolicy(VerdictThresholds.V1);

        /// <summary>Creates a policy over <paramref name="thresholds"/>.</summary>
        public VerdictPolicy(VerdictThresholds thresholds)
        {
            Thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
        }

        /// <summary>Thresholds in use.</summary>
        public VerdictThresholds Thresholds { get; }

        /// <summary>Verdict for <paramref name="score"/>; throws when the score is outside 0–100.</summary>
        public Verdict ToVerdict(int score)
        {
            if (score < 0 || score > 100) throw new ArgumentOutOfRangeException(nameof(score), score, "Score must be 0–100.");
            if (score >= Thresholds.Perfect) return Verdict.Perfect;
            if (score >= Thresholds.Great) return Verdict.Great;
            if (score >= Thresholds.Close) return Verdict.Close;
            return Verdict.TryAgain;
        }
    }
}
