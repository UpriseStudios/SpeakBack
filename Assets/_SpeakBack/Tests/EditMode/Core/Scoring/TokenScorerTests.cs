using System;
using System.Linq;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Content;
using Uprise.SpeakBack.Core.Scoring;

namespace Uprise.SpeakBack.Core.Tests.Scoring
{
    public sealed class TokenScorerTests
    {
        private static readonly PhonemeNormaliser Normaliser =
            new PhonemeNormaliser(SbpsInventory.Parse(TestData.ReadText("data/phonemes/sbps.json")));

        private static readonly PhraseInfo BlueBanana = new PhraseInfo(
            "sb_en_001", "Blue banana", new[] { "b", "l", "uː", "b", "ə", "n", "ɑː", "n", "ə" }, 1, "sbps-v1");

        // A Northern-style alternate for "please pass" (P00 Q6): pæs instead of pɑːs.
        private static readonly PhraseInfo PleasePass = new PhraseInfo(
            "test_pass", "Please pass", new[] { "p", "l", "iː", "z", "p", "ɑː", "s" }, 1, "sbps-v1",
            new[] { new[] { "p", "l", "iː", "z", "p", "æ", "s" } });

        private static RecognitionResult Phonemes(params string[] tokens) =>
            RecognitionResult.Success(TokenKind.Phoneme, tokens.Select(t => new RecognizedToken(t)), string.Join(" ", tokens), "engine-1", "model-1", 12.0);

        private static RecognitionResult Words(string transcript) =>
            RecognitionResult.Success(TokenKind.Word, transcript.Split(' ').Select(t => new RecognizedToken(t)), transcript, "asr-1", "tiny", 30.0);

        [Test]
        public void Phoneme_OneSubstitution_Scores89Great()
        {
            var scorer = new TokenScorer(ScorerPreset.PhonemeEdV1, Normaliser);

            var outcome = scorer.Score(BlueBanana, Phonemes("b", "l", "uː", "b", "ə", "n", "æ", "n", "ə"));

            Assert.That(outcome.Status, Is.EqualTo(ScoringStatus.Scored));
            Assert.That(outcome.Score, Is.EqualTo(89));
            Assert.That(outcome.Verdict, Is.EqualTo(Verdict.Great));
            Assert.That(outcome.EditDistance, Is.EqualTo(1));
            Assert.That(outcome.ScorerVersion, Is.EqualTo("phoneme-ed-v1"));
            Assert.That(outcome.VerdictPolicyVersion, Is.EqualTo("verdict-v1"));
            Assert.That(outcome.EngineVersion, Is.EqualTo("engine-1"));
            Assert.That(outcome.ModelVersion, Is.EqualTo("model-1"));
            Assert.That(outcome.Failure, Is.EqualTo(ScoringFailure.None));
        }

        [Test]
        public void Phoneme_ObservedTokensAreNormalised()
        {
            var scorer = new TokenScorer(ScorerPreset.PhonemeEdV1, Normaliser);

            var outcome = scorer.Score(BlueBanana, Phonemes("sil", "ˈb", "l", "uː", "|", "ˈb", "ə", "n", "ˈɑː", "n", "ə", "sil"));

            Assert.That(outcome.Score, Is.EqualTo(100));
            Assert.That(outcome.ObservedTokens, Is.EqualTo(BlueBanana.Phonemes));
        }

        [Test]
        public void EmptySuccessfulObservation_ScoresZero_TryAgain()
        {
            var scorer = new TokenScorer(ScorerPreset.PhonemeEdV1, Normaliser);

            var outcome = scorer.Score(BlueBanana, Phonemes());

            Assert.That(outcome.Status, Is.EqualTo(ScoringStatus.Scored));
            Assert.That(outcome.Score, Is.EqualTo(0));
            Assert.That(outcome.Verdict, Is.EqualTo(Verdict.TryAgain));
        }

        [TestCase(RecognitionStatus.Unavailable, ScoringFailure.RecognitionUnavailable)]
        [TestCase(RecognitionStatus.Failed, ScoringFailure.RecognitionFailed)]
        [TestCase(RecognitionStatus.TimedOut, ScoringFailure.TimedOut)]
        [TestCase(RecognitionStatus.Cancelled, ScoringFailure.Cancelled)]
        public void FailedRecognition_GivesNoScore(RecognitionStatus status, ScoringFailure expected)
        {
            var scorer = new TokenScorer(ScorerPreset.PhonemeEdV1, Normaliser);

            var outcome = scorer.Score(BlueBanana, RecognitionResult.Failure(status, TokenKind.Phoneme, "E42", "engine-1", "model-1", 5000));

            Assert.That(outcome.Status, Is.EqualTo(ScoringStatus.Failed));
            Assert.That(outcome.Score, Is.Null);
            Assert.That(outcome.Verdict, Is.Null);
            Assert.That(outcome.Failure, Is.EqualTo(expected));
            Assert.That(outcome.ScorerVersion, Is.EqualTo("phoneme-ed-v1"));
        }

        [Test]
        public void WrongTokenKind_IsTypedFailure()
        {
            var scorer = new TokenScorer(ScorerPreset.PhonemeEdV1, Normaliser);

            var outcome = scorer.Score(BlueBanana, Words("blue banana"));

            Assert.That(outcome.Failure, Is.EqualTo(ScoringFailure.TokenKindMismatch));
        }

        [Test]
        public void EmptyTarget_IsContentFailure_NotZero()
        {
            var scorer = new TokenScorer(ScorerPreset.PhonemeEdV1, Normaliser);
            var empty = new PhraseInfo("bad", "Blue", Array.Empty<string>(), 1, "sbps-v1");

            var outcome = scorer.Score(empty, Phonemes("b"));

            Assert.That(outcome.Status, Is.EqualTo(ScoringStatus.Failed));
            Assert.That(outcome.Failure, Is.EqualTo(ScoringFailure.EmptyTarget));
        }

        [Test]
        public void Word_SpecExamples()
        {
            // Phase 1 §12 worked examples for the word scorer.
            var scorer = new TokenScorer(ScorerPreset.WordEdV1, Normaliser);
            var penguin = new PhraseInfo("sb_en_008", "Never trust a talking penguin", new[] { "n" }, 1, "sbps-v1");

            Assert.That(scorer.Score(penguin, Words("Never trust a talking penguin")).Score, Is.EqualTo(100));
            Assert.That(scorer.Score(penguin, Words("never trust the talking penguin")).Score, Is.EqualTo(80));
            Assert.That(scorer.Score(BlueBanana, Words("blue")).Score, Is.EqualTo(50));
            Assert.That(scorer.Score(BlueBanana, Words("banana blue")).Score, Is.EqualTo(0));
            Assert.That(scorer.Score(BlueBanana, Words("Blue, banana!")).Score, Is.EqualTo(100));
            Assert.That(scorer.Score(BlueBanana, Words("blue bandana")).Score, Is.EqualTo(50));
        }

        [Test]
        public void Char_ComparesCharactersWithoutSpaces()
        {
            var scorer = new TokenScorer(ScorerPreset.CharEdV1, Normaliser);

            var outcome = scorer.Score(BlueBanana, Words("blue bandana"));

            // bluebanana (10) vs bluebandana (11): one insertion → (200·9 + 10) / 20 = 90.
            Assert.That(outcome.EditDistance, Is.EqualTo(1));
            Assert.That(outcome.Score, Is.EqualTo(90));
            Assert.That(outcome.ScorerVersion, Is.EqualTo("char-ed-v1"));
        }

        [Test]
        public void PrimaryOnlyPreset_IgnoresAlternates()
        {
            var scorer = new TokenScorer(ScorerPreset.PhonemeEdV1, Normaliser);

            var outcome = scorer.Score(PleasePass, Phonemes("p", "l", "iː", "z", "p", "æ", "s"));

            Assert.That(outcome.Score, Is.EqualTo(86));
            Assert.That(outcome.MatchedTargetIndex, Is.EqualTo(0));
        }

        [Test]
        public void BestOfPreset_UsesMatchingAlternate()
        {
            var scorer = new TokenScorer(ScorerPreset.PhonemeEdBestV1, Normaliser);

            var outcome = scorer.Score(PleasePass, Phonemes("p", "l", "iː", "z", "p", "æ", "s"));

            Assert.That(outcome.Score, Is.EqualTo(100));
            Assert.That(outcome.MatchedTargetIndex, Is.EqualTo(1));
            Assert.That(outcome.TargetTokens, Is.EqualTo(PleasePass.AlternatePronunciations[0]));
            Assert.That(outcome.ScorerVersion, Is.EqualTo("phoneme-ed-best-v1"));
        }

        [Test]
        public void BestOfPreset_PrefersPrimaryOnTie()
        {
            var scorer = new TokenScorer(ScorerPreset.PhonemeEdBestV1, Normaliser);

            var outcome = scorer.Score(PleasePass, Phonemes("p", "l", "iː", "z", "p", "ʌ", "s"));

            Assert.That(outcome.MatchedTargetIndex, Is.EqualTo(0));
        }

        [Test]
        public void Presets_NeedTheRightRecogniserOutput()
        {
            Assert.That(ScorerPreset.PhonemeEdV1.RequiredKind, Is.EqualTo(TokenKind.Phoneme));
            Assert.That(ScorerPreset.WordEdV1.RequiredKind, Is.EqualTo(TokenKind.Word));
            Assert.That(ScorerPreset.CharEdV1.RequiredKind, Is.EqualTo(TokenKind.Word));
        }
    }
}
