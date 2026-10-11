using System;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Content;

namespace Uprise.SpeakBack.Core.Tests.Content
{
    public sealed class PhraseCatalogueReaderTests
    {
        private static SbpsInventory Sbps() => SbpsInventory.Parse(TestData.ReadText("data/phonemes/sbps.json"));

        [Test]
        public void ReadsPhase1Catalogue_InOrder()
        {
            var result = PhraseCatalogueReader.Read(TestData.ReadText("data/phrases/phase1_phrases.json"), Sbps());

            Assert.That(result.Phrases.Count, Is.EqualTo(10));
            Assert.That(result.Phrases[0].Id, Is.EqualTo("sb_en_001"));
            Assert.That(result.Phrases[0].TargetText, Is.EqualTo("Blue banana"));
            Assert.That(result.Phrases[0].Phonemes.Count, Is.EqualTo(9));
            Assert.That(result.Phrases[9].Id, Is.EqualTo("sb_en_010"));
            Assert.That(result.Phrases[6].AlternatePronunciations.Count, Is.EqualTo(2));
            Assert.That(result.Phrases[0].PhonemeVersion, Is.EqualTo("sbps-v1"));
            Assert.That(result.Warnings.Count, Is.EqualTo(10));
        }

        [Test]
        public void RefusesCatalogueWithErrors()
        {
            var broken = TestData.ReadText("data/phrases/phase1_phrases.json").Replace("\"sb_en_002\"", "\"sb_en_001\"");

            var ex = Assert.Throws<FormatException>(() => PhraseCatalogueReader.Read(broken, Sbps()));
            StringAssert.Contains("DuplicateId", ex.Message);
        }
    }
}
