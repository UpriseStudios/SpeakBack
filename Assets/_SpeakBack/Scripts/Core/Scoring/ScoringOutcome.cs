using System;
using System.Collections.Generic;
using System.Linq;

namespace Uprise.SpeakBack.Core.Scoring
{
    /// <summary>Whether an attempt received a score.</summary>
    public enum ScoringStatus
    {
        /// <summary>A valid capture was evaluated successfully; Score and Verdict are set.</summary>
        Scored,

        /// <summary>No score. A technical outcome, never shown as zero (Master §6.5).</summary>
        Failed,
    }

    /// <summary>Typed reason for a missing score. UI maps these to neutral copy, never raw exception text.</summary>
    public enum ScoringFailure
    {
        /// <summary>Not a failure.</summary>
        None,

        /// <summary>Recogniser not available.</summary>
        RecognitionUnavailable,

        /// <summary>Recogniser reported an error.</summary>
        RecognitionFailed,

        /// <summary>Evaluation exceeded its deadline.</summary>
        TimedOut,

        /// <summary>Cancelled (Retry, Next, pause).</summary>
        Cancelled,

        /// <summary>Phrase has no target tokens: a content error.</summary>
        EmptyTarget,

        /// <summary>Recogniser produced words where phonemes were needed, or the reverse.</summary>
        TokenKindMismatch,

        /// <summary>Any other evaluation error.</summary>
        EvaluationFailed,
    }

    /// <summary>
    /// Immutable result of scoring one attempt (Phase 1 §5). A score exists only for a valid voice capture
    /// and successful evaluation. Every outcome carries the scorer and verdict-policy versions (§12).
    /// </summary>
    public sealed class ScoringOutcome
    {
        private static readonly IReadOnlyDictionary<string, double> NoComponents = new Dictionary<string, double>();

        private ScoringOutcome(
            ScoringStatus status, int? score, Verdict? verdict, string scorerVersion, string verdictPolicyVersion,
            IReadOnlyList<string> targetTokens, IReadOnlyList<string> observedTokens, int? editDistance, int matchedTargetIndex,
            IReadOnlyDictionary<string, double> components, ScoringFailure failure, string engineVersion, string modelVersion)
        {
            Status = status;
            Score = score;
            Verdict = verdict;
            ScorerVersion = scorerVersion ?? throw new ArgumentNullException(nameof(scorerVersion));
            VerdictPolicyVersion = verdictPolicyVersion;
            TargetTokens = targetTokens ?? Array.Empty<string>();
            ObservedTokens = observedTokens ?? Array.Empty<string>();
            EditDistance = editDistance;
            MatchedTargetIndex = matchedTargetIndex;
            Components = components ?? NoComponents;
            Failure = failure;
            EngineVersion = engineVersion ?? string.Empty;
            ModelVersion = modelVersion ?? string.Empty;
        }

        /// <summary>Scored or Failed.</summary>
        public ScoringStatus Status { get; }

        /// <summary>0–100 when scored; null otherwise.</summary>
        public int? Score { get; }

        /// <summary>Verdict when scored; null otherwise.</summary>
        public Verdict? Verdict { get; }

        /// <summary>Scorer version, e.g. "phoneme-ed-v1".</summary>
        public string ScorerVersion { get; }

        /// <summary>Verdict thresholds version, e.g. "verdict-v1"; null when not scored.</summary>
        public string VerdictPolicyVersion { get; }

        /// <summary>Target tokens actually compared (the matched pronunciation for best-of scorers).</summary>
        public IReadOnlyList<string> TargetTokens { get; }

        /// <summary>Observed tokens after normalisation. Shown only on the result screen; never logged.</summary>
        public IReadOnlyList<string> ObservedTokens { get; }

        /// <summary>Edit distance when scored.</summary>
        public int? EditDistance { get; }

        /// <summary>0 for the primary target; 1+ for an alternate pronunciation (best-of scorers only).</summary>
        public int MatchedTargetIndex { get; }

        /// <summary>Optional component diagnostics (e.g. timing similarity); empty for single scorers.</summary>
        public IReadOnlyDictionary<string, double> Components { get; }

        /// <summary>Why there is no score; None when scored.</summary>
        public ScoringFailure Failure { get; }

        /// <summary>Recogniser engine version.</summary>
        public string EngineVersion { get; }

        /// <summary>Recogniser model version.</summary>
        public string ModelVersion { get; }

        /// <summary>A scored outcome.</summary>
        public static ScoringOutcome Scored(
            int score, Verdict verdict, string scorerVersion, string verdictPolicyVersion,
            IEnumerable<string> targetTokens, IEnumerable<string> observedTokens, int editDistance, int matchedTargetIndex = 0,
            IReadOnlyDictionary<string, double> components = null, string engineVersion = null, string modelVersion = null)
        {
            if (score < 0 || score > 100) throw new ArgumentOutOfRangeException(nameof(score), score, "Score must be 0–100.");
            if (editDistance < 0) throw new ArgumentOutOfRangeException(nameof(editDistance));
            return new ScoringOutcome(ScoringStatus.Scored, score, verdict, scorerVersion,
                verdictPolicyVersion ?? throw new ArgumentNullException(nameof(verdictPolicyVersion)),
                Copy(targetTokens), Copy(observedTokens), editDistance, matchedTargetIndex,
                components?.ToDictionary(p => p.Key, p => p.Value),
                ScoringFailure.None, engineVersion, modelVersion);
        }

        /// <summary>An unscored outcome with a typed reason.</summary>
        public static ScoringOutcome Failed(
            ScoringFailure failure, string scorerVersion, string engineVersion = null, string modelVersion = null)
        {
            if (failure == ScoringFailure.None) throw new ArgumentException("A failed outcome needs a reason.", nameof(failure));
            return new ScoringOutcome(ScoringStatus.Failed, null, null, scorerVersion, null,
                null, null, null, 0, null, failure, engineVersion, modelVersion);
        }

        private static IReadOnlyList<string> Copy(IEnumerable<string> tokens) =>
            (tokens ?? throw new ArgumentNullException(nameof(tokens))).ToArray();
    }
}
