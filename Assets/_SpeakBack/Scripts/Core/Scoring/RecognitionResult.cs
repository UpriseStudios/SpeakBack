using System;
using System.Collections.Generic;
using System.Linq;

namespace Uprise.SpeakBack.Core.Scoring
{
    /// <summary>Outcome of one recognition job (Phase 1 §5).</summary>
    public enum RecognitionStatus
    {
        /// <summary>Recognition finished; tokens may still be empty.</summary>
        Success,

        /// <summary>Engine or model not available (e.g. not initialised).</summary>
        Unavailable,

        /// <summary>Engine reported an error.</summary>
        Failed,

        /// <summary>Exceeded the evaluation deadline.</summary>
        TimedOut,

        /// <summary>Cancelled, e.g. by Retry, Next or pause.</summary>
        Cancelled,
    }

    /// <summary>What a recogniser's tokens represent.</summary>
    public enum TokenKind
    {
        /// <summary>Words of a transcript.</summary>
        Word,

        /// <summary>Phonemes, already mapped into SBPS by the engine adapter.</summary>
        Phoneme,
    }

    /// <summary>One recognised token. Missing timing or confidence stays missing (null), never invented.</summary>
    public sealed class RecognizedToken
    {
        /// <summary>Creates a token.</summary>
        public RecognizedToken(string text, double? startSeconds = null, double? endSeconds = null, float? confidence = null)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            Confidence = confidence;
        }

        /// <summary>Token text.</summary>
        public string Text { get; }

        /// <summary>Start time in the scorer input, when the engine provides it.</summary>
        public double? StartSeconds { get; }

        /// <summary>End time in the scorer input, when the engine provides it.</summary>
        public double? EndSeconds { get; }

        /// <summary>Engine confidence, when provided. Diagnostic only; not a calibrated probability (Master §6.2).</summary>
        public float? Confidence { get; }
    }

    /// <summary>
    /// Result of a recognition job (Phase 1 §5). A failure never masquerades as an empty success:
    /// only <see cref="Success"/> results carry tokens, and a success may legitimately have none.
    /// </summary>
    public sealed class RecognitionResult
    {
        private RecognitionResult(
            RecognitionStatus status, TokenKind kind, IReadOnlyList<RecognizedToken> tokens, string displayText,
            string engineVersion, string modelVersion, double processingMilliseconds, string failureCode)
        {
            Status = status;
            Kind = kind;
            Tokens = tokens;
            DisplayText = displayText;
            EngineVersion = engineVersion ?? string.Empty;
            ModelVersion = modelVersion ?? string.Empty;
            ProcessingMilliseconds = processingMilliseconds;
            FailureCode = failureCode;
        }

        /// <summary>Success or the failure category.</summary>
        public RecognitionStatus Status { get; }

        /// <summary>Whether tokens are words or phonemes.</summary>
        public TokenKind Kind { get; }

        /// <summary>Recognised tokens in order; empty for failures.</summary>
        public IReadOnlyList<RecognizedToken> Tokens { get; }

        /// <summary>Human-readable form for the result screen's "Heard after flip" line. Never logged.</summary>
        public string DisplayText { get; }

        /// <summary>Engine version string.</summary>
        public string EngineVersion { get; }

        /// <summary>Model version string.</summary>
        public string ModelVersion { get; }

        /// <summary>Time the engine spent, in milliseconds.</summary>
        public double ProcessingMilliseconds { get; }

        /// <summary>Engine-specific failure code for development logs; null on success.</summary>
        public string FailureCode { get; }

        /// <summary>A successful recognition (zero tokens allowed).</summary>
        public static RecognitionResult Success(
            TokenKind kind, IEnumerable<RecognizedToken> tokens, string displayText,
            string engineVersion, string modelVersion, double processingMilliseconds)
        {
            var list = (tokens ?? throw new ArgumentNullException(nameof(tokens))).ToArray();
            if (list.Any(t => t == null)) throw new ArgumentException("Null token.", nameof(tokens));
            return new RecognitionResult(RecognitionStatus.Success, kind, list, displayText ?? string.Empty,
                engineVersion, modelVersion, processingMilliseconds, null);
        }

        /// <summary>A failed recognition; <paramref name="status"/> must not be Success.</summary>
        public static RecognitionResult Failure(
            RecognitionStatus status, TokenKind kind, string failureCode,
            string engineVersion, string modelVersion, double processingMilliseconds)
        {
            if (status == RecognitionStatus.Success) throw new ArgumentException("Use Success() for successful results.", nameof(status));
            return new RecognitionResult(status, kind, Array.Empty<RecognizedToken>(), string.Empty,
                engineVersion, modelVersion, processingMilliseconds, failureCode ?? status.ToString());
        }
    }

    /// <summary>What a recogniser is told about the job. Carries no target text, so unconstrained recognisers stay unconstrained (§11.2).</summary>
    public sealed class RecognitionContext
    {
        /// <summary>Creates a context.</summary>
        public RecognitionContext(string phraseId, string locale, TokenKind requestedKind)
        {
            PhraseId = phraseId ?? throw new ArgumentNullException(nameof(phraseId));
            Locale = locale ?? throw new ArgumentNullException(nameof(locale));
            RequestedKind = requestedKind;
        }

        /// <summary>Phrase being attempted (for logging and late-result checks, not for constraining recognition).</summary>
        public string PhraseId { get; }

        /// <summary>Locale, e.g. "en-GB".</summary>
        public string Locale { get; }

        /// <summary>Token kind the scorer needs.</summary>
        public TokenKind RequestedKind { get; }
    }
}
