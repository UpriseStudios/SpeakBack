using System;
using System.Collections.Generic;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Content;
using Uprise.SpeakBack.Core.Scoring;

namespace Uprise.SpeakBack.Core.Tests.Scoring
{
    public sealed class ContractTests
    {
        [Test]
        public void RecognitionResult_FailureCannotBeSuccess()
        {
            Assert.Throws<ArgumentException>(() =>
                RecognitionResult.Failure(RecognitionStatus.Success, TokenKind.Word, null, "e", "m", 0));
        }

        [Test]
        public void RecognitionResult_FailureHasNoTokens_AndAFailureCode()
        {
            var result = RecognitionResult.Failure(RecognitionStatus.TimedOut, TokenKind.Phoneme, null, "e", "m", 5000);

            Assert.That(result.Tokens, Is.Empty);
            Assert.That(result.FailureCode, Is.EqualTo("TimedOut"));
        }

        [Test]
        public void RecognitionResult_SuccessMayBeEmpty()
        {
            var result = RecognitionResult.Success(TokenKind.Word, new RecognizedToken[0], null, "e", "m", 1);

            Assert.That(result.Status, Is.EqualTo(RecognitionStatus.Success));
            Assert.That(result.Tokens, Is.Empty);
            Assert.That(result.DisplayText, Is.EqualTo(string.Empty));
        }

        [Test]
        public void RecognizedToken_MissingConfidenceStaysMissing()
        {
            var token = new RecognizedToken("b");

            Assert.That(token.Confidence, Is.Null);
            Assert.That(token.StartSeconds, Is.Null);
        }

        [Test]
        public void RecognitionResult_CopiesTokens()
        {
            var tokens = new List<RecognizedToken> { new RecognizedToken("a") };
            var result = RecognitionResult.Success(TokenKind.Word, tokens, "a", "e", "m", 1);

            tokens.Add(new RecognizedToken("b"));

            Assert.That(result.Tokens.Count, Is.EqualTo(1));
        }

        [Test]
        public void ScoringOutcome_FailedNeedsAReason()
        {
            Assert.Throws<ArgumentException>(() => ScoringOutcome.Failed(ScoringFailure.None, "v"));
        }

        [Test]
        public void ScoringOutcome_ScoredRejectsOutOfRangeScore()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ScoringOutcome.Scored(101, Verdict.Perfect, "v", "verdict-v1", new string[0], new string[0], 0));
        }

        [Test]
        public void ScoringOutcome_CopiesComponents()
        {
            var components = new Dictionary<string, double> { ["timing"] = 0.5 };
            var outcome = ScoringOutcome.Scored(80, Verdict.Great, "v", "verdict-v1", new[] { "a" }, new[] { "a" }, 0, components: components);

            components["timing"] = 0.9;

            Assert.That(outcome.Components["timing"], Is.EqualTo(0.5));
        }

        [Test]
        public void PhraseInfo_CopiesSequences()
        {
            var phonemes = new[] { "b", "l" };
            var phrase = new PhraseInfo("id", "Bl", phonemes, 1, "sbps-v1");

            phonemes[0] = "x";

            Assert.That(phrase.Phonemes[0], Is.EqualTo("b"));
            Assert.That(phrase.AlternatePronunciations, Is.Empty);
        }

        [Test]
        public void RecognitionContext_CarriesNoTargetText()
        {
            // Unconstrained recognisers must not see the target (Phase 1 §11.2).
            var properties = typeof(RecognitionContext).GetProperties();

            Assert.That(Array.ConvertAll(properties, p => p.Name), Is.EquivalentTo(new[] { "PhraseId", "Locale", "RequestedKind" }));
        }
    }
}
