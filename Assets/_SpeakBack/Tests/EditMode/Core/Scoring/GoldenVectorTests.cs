using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Content;
using Uprise.SpeakBack.Core.Scoring;

namespace Uprise.SpeakBack.Core.Tests.Scoring
{
    /// <summary>
    /// Shared golden vectors (D-013). The Python harness runs the same files
    /// (Tools/BakeOff/tests/test_golden.py); both must produce identical numbers.
    /// </summary>
    public sealed class GoldenVectorTests
    {
        private const string SimilarityFile = "data/golden/similarity_cases.json";
        private const string VerdictFile = "data/golden/verdict_cases.json";

        public static IEnumerable<TestCaseData> SimilarityCases()
        {
            var root = MiniJson.ParseObject(TestData.ReadText(SimilarityFile));
            foreach (var item in JsonValues.Array(root, "cases"))
            {
                var c = (IReadOnlyDictionary<string, object>)item;
                var name = JsonValues.String(c, "name");
                yield return new TestCaseData(
                        name,
                        JsonValues.StringArray(c, "target"),
                        JsonValues.StringArray(c, "observed"),
                        JsonValues.Int(c, "expectedDistance"),
                        JsonValues.Int(c, "expectedScore"))
                    .SetName("Golden_" + name);
            }
        }

        [TestCaseSource(nameof(SimilarityCases))]
        public void SimilarityCase(string name, IReadOnlyList<string> target, IReadOnlyList<string> observed, int expectedDistance, int expectedScore)
        {
            var distance = SequenceSimilarity.EditDistance(target, observed);
            var score = ScoreMath.Score(target.Count, distance);

            // Machine-readable line for the C#/Python comparison table in the P03 report.
            TestContext.WriteLine($"GOLDEN|{name}|{distance}|{score}");
            Assert.That(distance, Is.EqualTo(expectedDistance), name);
            Assert.That(score, Is.EqualTo(expectedScore), name);
        }

        [Test]
        public void ErrorCases_RaiseTheirExpectedError()
        {
            var root = MiniJson.ParseObject(TestData.ReadText(SimilarityFile));
            var errorCases = JsonValues.Array(root, "errorCases");
            Assert.That(errorCases.Count, Is.GreaterThan(0));

            foreach (var item in errorCases)
            {
                var c = (IReadOnlyDictionary<string, object>)item;
                var target = JsonValues.StringArray(c, "target");
                var observed = JsonValues.StringArray(c, "observed");
                Assert.That(JsonValues.String(c, "expectedError"), Is.EqualTo(EmptyTargetException.Code));

                Assert.Throws<EmptyTargetException>(() =>
                    ScoreMath.Score(target.Count, SequenceSimilarity.EditDistance(target, observed)));
            }
        }

        [Test]
        public void SimilarityFile_StatesTheD012Formula()
        {
            var root = MiniJson.ParseObject(TestData.ReadText(SimilarityFile));

            StringAssert.Contains("(200*(N-D)+N) / (2*N)", JsonValues.String(root, "formula"));
        }

        [Test]
        public void VerdictCases_MatchPolicy()
        {
            var root = MiniJson.ParseObject(TestData.ReadText(VerdictFile));
            var thresholds = JsonValues.Object(root, "thresholds");
            var policy = VerdictPolicy.Default;

            Assert.That(JsonValues.Int(thresholds, "Perfect"), Is.EqualTo(policy.Thresholds.Perfect));
            Assert.That(JsonValues.Int(thresholds, "Great"), Is.EqualTo(policy.Thresholds.Great));
            Assert.That(JsonValues.Int(thresholds, "Close"), Is.EqualTo(policy.Thresholds.Close));

            foreach (var item in JsonValues.Array(root, "cases"))
            {
                var c = (IReadOnlyDictionary<string, object>)item;
                var score = JsonValues.Int(c, "score");
                var expected = (Verdict)Enum.Parse(typeof(Verdict), JsonValues.String(c, "expectedVerdict"));

                Assert.That(policy.ToVerdict(score), Is.EqualTo(expected), $"score {score}");
            }

            foreach (var invalid in JsonValues.Array(root, "invalidScores").Cast<double>())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => policy.ToVerdict((int)invalid), $"score {invalid}");
            }
        }
    }
}
