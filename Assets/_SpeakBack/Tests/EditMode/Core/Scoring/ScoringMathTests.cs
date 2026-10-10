using System;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Scoring;

namespace Uprise.SpeakBack.Core.Tests.Scoring
{
    public sealed class ScoringMathTests
    {
        private static string[] T(string tokens) => tokens.Length == 0 ? Array.Empty<string>() : tokens.Split(' ');

        [TestCase("a b c", "a b c", 0)]
        [TestCase("a b c", "a x c", 1)]
        [TestCase("a b c", "a b", 1)]
        [TestCase("a b", "a b c", 1)]
        [TestCase("a b", "b a", 2)]
        [TestCase("a b c", "", 3)]
        [TestCase("", "a b", 2)]
        [TestCase("k i t e n", "s i t i n g", 3)]
        public void EditDistance_Basics(string target, string observed, int expected)
        {
            Assert.That(SequenceSimilarity.EditDistance(T(target), T(observed)), Is.EqualTo(expected));
        }

        [Test]
        public void EditDistance_IsSymmetric_AndOrdinal()
        {
            var a = new[] { "iː", "ɪ", "ə" };
            var b = new[] { "ɪ", "ə", "ʌ", "ə" };

            Assert.That(SequenceSimilarity.EditDistance(a, b), Is.EqualTo(SequenceSimilarity.EditDistance(b, a)));
            Assert.That(SequenceSimilarity.EditDistance(new[] { "A" }, new[] { "a" }), Is.EqualTo(1));
        }

        [Test]
        public void EditDistance_MultiCharacterTokensAreAtomic()
        {
            // tʃ is one SBPS token; t + ʃ is two.
            Assert.That(SequenceSimilarity.EditDistance(new[] { "tʃ" }, new[] { "t", "ʃ" }), Is.EqualTo(2));
        }

        [TestCase(1, 0, 100)]
        [TestCase(2, 1, 50)]
        [TestCase(3, 1, 67)]
        [TestCase(8, 1, 88)]
        [TestCase(9, 1, 89)]
        [TestCase(7, 2, 71)]
        [TestCase(5, 5, 0)]
        [TestCase(5, 9, 0)]
        [TestCase(1000000, 1, 100)]
        public void Score_UsesIntegerFormula(int n, int d, int expected)
        {
            Assert.That(ScoreMath.Score(n, d), Is.EqualTo(expected));
        }

        [Test]
        public void Score_MatchesRoundedRatio_ForAllSmallCases()
        {
            for (var n = 1; n <= 200; n++)
            {
                for (var d = 0; d <= 2 * n; d++)
                {
                    // D-012 equivalence: floor(100·(1 − D/N) + 0.5), clamped at 0. Decimal division is exact
                    // whenever the ratio lands on .5, so no rounding boundary can be crossed by error.
                    var expected = d >= n ? 0 : (int)Math.Floor(100m * (n - d) / n + 0.5m);
                    Assert.That(ScoreMath.Score(n, d), Is.EqualTo(expected), $"N={n} D={d}");
                }
            }
        }

        [Test]
        public void Score_EmptyTarget_ThrowsTypedError()
        {
            var ex = Assert.Throws<EmptyTargetException>(() => ScoreMath.Score(0, 1));
            StringAssert.Contains(EmptyTargetException.Code, ex.Message);
        }

        [Test]
        public void Score_NegativeDistance_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ScoreMath.Score(3, -1));
        }

        [TestCase(0, Verdict.TryAgain)]
        [TestCase(49, Verdict.TryAgain)]
        [TestCase(50, Verdict.Close)]
        [TestCase(74, Verdict.Close)]
        [TestCase(75, Verdict.Great)]
        [TestCase(89, Verdict.Great)]
        [TestCase(90, Verdict.Perfect)]
        [TestCase(100, Verdict.Perfect)]
        public void Verdict_Boundaries(int score, Verdict expected)
        {
            Assert.That(VerdictPolicy.Default.ToVerdict(score), Is.EqualTo(expected));
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void Verdict_RejectsOutOfRange(int score)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => VerdictPolicy.Default.ToVerdict(score));
        }

        [Test]
        public void Thresholds_AreVersioned_AndValidated()
        {
            Assert.That(VerdictThresholds.V1.Version, Is.EqualTo("verdict-v1"));
            Assert.Throws<ArgumentException>(() => new VerdictThresholds("bad", 75, 90, 50));
            Assert.Throws<ArgumentException>(() => new VerdictThresholds("", 90, 75, 50));
        }
    }
}
