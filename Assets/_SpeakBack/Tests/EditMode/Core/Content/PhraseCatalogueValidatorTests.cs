using System.Linq;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Content;

namespace Uprise.SpeakBack.Core.Tests.Content
{
    public sealed class PhraseCatalogueValidatorTests
    {
        private static SbpsInventory Sbps() => SbpsInventory.Parse(TestData.ReadText("data/phonemes/sbps.json"));

        private static string Catalogue(string phrase) =>
            "{\"version\":\"t\",\"phoneSet\":\"sbps-v1\",\"phrases\":[" + phrase + "]}";

        private const string GoodPhrase =
            "{\"id\":\"x1\",\"targetText\":\"Blue banana\",\"locale\":\"en-GB\",\"category\":\"Food\",\"difficulty\":\"Easy\"," +
            "\"phonemes\":[\"b\",\"l\",\"uː\"],\"phonemeCount\":3,\"phonemeVersion\":\"sbps-v1\",\"phonemeStatus\":\"reviewed\"," +
            "\"alternatePronunciations\":[],\"contentVersion\":1}";

        [Test]
        public void RealPhase1Catalogue_HasNoErrors_AndWarnsOnDraftPhonemes()
        {
            var issues = PhraseCatalogueValidator.Validate(TestData.ReadText("data/phrases/phase1_phrases.json"), Sbps());

            var errors = issues.Where(i => i.Severity == IssueSeverity.Error).ToList();
            var warnings = issues.Where(i => i.Severity == IssueSeverity.Warning).ToList();
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(warnings.Count, Is.EqualTo(10));
            Assert.That(warnings.Select(w => w.Code), Is.All.EqualTo("PhonemesNotReviewed"));
        }

        [Test]
        public void ReviewedPhrase_IsClean()
        {
            Assert.That(PhraseCatalogueValidator.Validate(Catalogue(GoodPhrase), Sbps()), Is.Empty);
        }

        [Test]
        public void DuplicateIds_AreErrors()
        {
            var issues = PhraseCatalogueValidator.Validate(Catalogue(GoodPhrase + "," + GoodPhrase), Sbps());

            Assert.That(issues.Select(i => i.Code), Has.Member("DuplicateId"));
        }

        [TestCase("\"uː\"", "\"u:\"", "UnknownPhoneme")] // ASCII colon
        [TestCase("\"l\"", "\"g\"", "UnknownPhoneme")] // ASCII g
        [TestCase("\"phonemeCount\":3", "\"phonemeCount\":4", "PhonemeCountMismatch")]
        [TestCase("\"Easy\"", "\"Medium\"", "UnknownDifficulty")]
        [TestCase("\"phonemes\":[\"b\",\"l\",\"uː\"],\"phonemeCount\":3", "\"phonemes\":[],\"phonemeCount\":0", "EmptyPhonemes")]
        [TestCase("\"phonemeVersion\":\"sbps-v1\"", "\"phonemeVersion\":\"sbps-v0\"", "PhoneSetMismatch")]
        [TestCase("\"targetText\":\"Blue banana\",", "", "MissingField")]
        [TestCase("\"alternatePronunciations\":[]", "\"alternatePronunciations\":[[\"b\",\"x\"]]", "UnknownPhoneme")]
        [TestCase("\"contentVersion\":1", "\"contentVersion\":\"1\"", "MissingField")]
        public void BrokenPhrase_IsReported(string find, string replace, string expectedCode)
        {
            var issues = PhraseCatalogueValidator.Validate(Catalogue(GoodPhrase.Replace(find, replace)), Sbps());

            Assert.That(issues.Where(i => i.Severity == IssueSeverity.Error).Select(i => i.Code), Has.Member(expectedCode));
        }

        [Test]
        public void UnknownPhoneme_MessageShowsCodepoints()
        {
            var issues = PhraseCatalogueValidator.Validate(Catalogue(GoodPhrase.Replace("\"l\"", "\"g\"")), Sbps());

            StringAssert.Contains("U+0067", issues.Single(i => i.Code == "UnknownPhoneme").Message);
        }

        [Test]
        public void WrongPhoneSet_IsError()
        {
            var json = Catalogue(GoodPhrase).Replace("\"phoneSet\":\"sbps-v1\"", "\"phoneSet\":\"arpabet\"");

            Assert.That(PhraseCatalogueValidator.Validate(json, Sbps()).Select(i => i.Code), Has.Member("PhoneSetMismatch"));
        }

        [Test]
        public void MalformedJson_IsSingleError()
        {
            var issues = PhraseCatalogueValidator.Validate("{\"phrases\": [", Sbps());

            Assert.That(issues.Single().Code, Is.EqualTo("InvalidJson"));
        }
    }
}
