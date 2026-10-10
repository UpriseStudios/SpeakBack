using System;
using System.Collections.Generic;
using System.Linq;
using Uprise.SpeakBack.Core.Content;

namespace Uprise.SpeakBack.Core.Scoring
{
    /// <summary>Unit compared by a token scorer.</summary>
    public enum ScoringUnit
    {
        /// <summary>SBPS phonemes.</summary>
        Phoneme,

        /// <summary>Normalised words.</summary>
        Word,

        /// <summary>Normalised characters, spaces removed.</summary>
        Character,
    }

    /// <summary>A named, versioned token-scoring configuration (Phase 1 §12).</summary>
    public sealed class ScorerPreset
    {
        /// <summary>Phoneme edit distance against the primary pronunciation (the Phase 1 candidate).</summary>
        public static readonly ScorerPreset PhonemeEdV1 = new ScorerPreset("phoneme-ed-v1", ScoringUnit.Phoneme, false);

        /// <summary>
        /// Phoneme edit distance against the best of the primary and alternate pronunciations. Bake-off
        /// comparison for regional fairness (P00 Q6); not a shipping scorer unless the bake-off says so.
        /// </summary>
        public static readonly ScorerPreset PhonemeEdBestV1 = new ScorerPreset("phoneme-ed-best-v1", ScoringUnit.Phoneme, true);

        /// <summary>Word edit distance on normalised text.</summary>
        public static readonly ScorerPreset WordEdV1 = new ScorerPreset("word-ed-v1", ScoringUnit.Word, false);

        /// <summary>Character edit distance on normalised text without spaces.</summary>
        public static readonly ScorerPreset CharEdV1 = new ScorerPreset("char-ed-v1", ScoringUnit.Character, false);

        private ScorerPreset(string version, ScoringUnit unit, bool usesAlternates)
        {
            Version = version;
            Unit = unit;
            UsesAlternates = usesAlternates;
        }

        /// <summary>Scorer version recorded with outcomes.</summary>
        public string Version { get; }

        /// <summary>Tokens compared.</summary>
        public ScoringUnit Unit { get; }

        /// <summary>Whether alternate pronunciations are also tried (best score wins).</summary>
        public bool UsesAlternates { get; }

        /// <summary>Recogniser output this preset needs.</summary>
        public TokenKind RequiredKind => Unit == ScoringUnit.Phoneme ? TokenKind.Phoneme : TokenKind.Word;
    }

    /// <summary>
    /// Turns a phrase and a <see cref="RecognitionResult"/> into a <see cref="ScoringOutcome"/> using the
    /// shared maths (Phase 1 §12). An empty successful observation scores 0; a failed recognition gives
    /// no score with a typed failure. No confidence multipliers, duration rewards or invented components.
    /// </summary>
    public sealed class TokenScorer
    {
        private readonly PhonemeNormaliser _phonemeNormaliser;
        private readonly VerdictPolicy _verdictPolicy;

        /// <summary>Creates a scorer for <paramref name="preset"/>.</summary>
        public TokenScorer(ScorerPreset preset, PhonemeNormaliser phonemeNormaliser, VerdictPolicy verdictPolicy = null)
        {
            Preset = preset ?? throw new ArgumentNullException(nameof(preset));
            _phonemeNormaliser = phonemeNormaliser ?? throw new ArgumentNullException(nameof(phonemeNormaliser));
            _verdictPolicy = verdictPolicy ?? VerdictPolicy.Default;
        }

        /// <summary>Preset in use.</summary>
        public ScorerPreset Preset { get; }

        /// <summary>Scores <paramref name="recognition"/> against <paramref name="phrase"/>.</summary>
        public ScoringOutcome Score(PhraseInfo phrase, RecognitionResult recognition)
        {
            if (phrase == null) throw new ArgumentNullException(nameof(phrase));
            if (recognition == null) throw new ArgumentNullException(nameof(recognition));

            var engine = recognition.EngineVersion;
            var model = recognition.ModelVersion;

            if (recognition.Status != RecognitionStatus.Success)
            {
                return ScoringOutcome.Failed(MapFailure(recognition.Status), Preset.Version, engine, model);
            }

            if (recognition.Kind != Preset.RequiredKind)
            {
                return ScoringOutcome.Failed(ScoringFailure.TokenKindMismatch, Preset.Version, engine, model);
            }

            var targets = Targets(phrase);
            if (targets.Any(t => t.Count == 0))
            {
                return ScoringOutcome.Failed(ScoringFailure.EmptyTarget, Preset.Version, engine, model);
            }

            var observed = Observed(recognition);

            var bestIndex = 0;
            var bestDistance = SequenceSimilarity.EditDistance(targets[0], observed);
            var bestScore = ScoreMath.Score(targets[0].Count, bestDistance);
            for (var i = 1; i < targets.Count; i++)
            {
                var distance = SequenceSimilarity.EditDistance(targets[i], observed);
                var score = ScoreMath.Score(targets[i].Count, distance);
                if (score > bestScore)
                {
                    bestIndex = i;
                    bestDistance = distance;
                    bestScore = score;
                }
            }

            return ScoringOutcome.Scored(
                bestScore, _verdictPolicy.ToVerdict(bestScore), Preset.Version, _verdictPolicy.Thresholds.Version,
                targets[bestIndex], observed, bestDistance, bestIndex, engineVersion: engine, modelVersion: model);
        }

        /// <summary>Target sequences for the preset: primary first, then alternates when the preset uses them.</summary>
        public IReadOnlyList<IReadOnlyList<string>> Targets(PhraseInfo phrase)
        {
            if (phrase == null) throw new ArgumentNullException(nameof(phrase));
            switch (Preset.Unit)
            {
                case ScoringUnit.Phoneme:
                    var targets = new List<IReadOnlyList<string>> { phrase.Phonemes };
                    if (Preset.UsesAlternates) targets.AddRange(phrase.AlternatePronunciations);
                    return targets;
                case ScoringUnit.Word:
                    return new[] { TextNormaliser.Words(phrase.TargetText) };
                default:
                    return new[] { TextNormaliser.Characters(phrase.TargetText) };
            }
        }

        private IReadOnlyList<string> Observed(RecognitionResult recognition)
        {
            switch (Preset.Unit)
            {
                case ScoringUnit.Phoneme:
                    return _phonemeNormaliser.NormaliseAll(recognition.Tokens.Select(t => t.Text));
                case ScoringUnit.Word:
                    return TextNormaliser.Words(JoinTokens(recognition));
                default:
                    return TextNormaliser.Characters(JoinTokens(recognition));
            }
        }

        private static string JoinTokens(RecognitionResult recognition) =>
            string.Join(" ", recognition.Tokens.Select(t => t.Text));

        private static ScoringFailure MapFailure(RecognitionStatus status)
        {
            switch (status)
            {
                case RecognitionStatus.Unavailable: return ScoringFailure.RecognitionUnavailable;
                case RecognitionStatus.TimedOut: return ScoringFailure.TimedOut;
                case RecognitionStatus.Cancelled: return ScoringFailure.Cancelled;
                default: return ScoringFailure.RecognitionFailed;
            }
        }
    }
}
