using System;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Content;

namespace Uprise.SpeakBack.Core.Tests.Content
{
    public sealed class SbpsTests
    {
        private static SbpsInventory Load() => SbpsInventory.Parse(TestData.ReadText("data/phonemes/sbps.json"));

        [Test]
        public void Parse_LoadsFortyFourSymbols()
        {
            var sbps = Load();

            Assert.That(sbps.Version, Is.EqualTo("sbps-v1"));
            Assert.That(sbps.Consonants.Count, Is.EqualTo(24));
            Assert.That(sbps.Vowels.Count, Is.EqualTo(20));
            Assert.That(sbps.Symbols.Count, Is.EqualTo(44));
            Assert.That(sbps.Contains("tʃ"), Is.True);
            Assert.That(sbps.Contains("iː"), Is.True);
            Assert.That(sbps.Contains("ɡ"), Is.True, "IPA ɡ (U+0261)");
            Assert.That(sbps.Contains("g"), Is.False, "ASCII g is not an SBPS symbol");
            Assert.That(sbps.Contains("i:"), Is.False, "ASCII colon is not the length mark");
        }

        [Test]
        public void Parse_RejectsDuplicateSymbols()
        {
            const string json = "{\"version\":\"x\",\"consonants\":[\"p\",\"p\"],\"vowels\":[],\"normalisation\":{\"stripMarks\":[],\"dropTokens\":[]},\"mappings\":{\"arpabet\":{\"version\":\"a\",\"table\":{}}}}";

            Assert.Throws<FormatException>(() => SbpsInventory.Parse(json));
        }

        [Test]
        public void Parse_RejectsMappingToUnknownSymbol()
        {
            const string json = "{\"version\":\"x\",\"consonants\":[\"p\"],\"vowels\":[],\"normalisation\":{\"stripMarks\":[],\"dropTokens\":[]},\"mappings\":{\"arpabet\":{\"version\":\"a\",\"table\":{\"B\":\"b\"}}}}";

            Assert.Throws<FormatException>(() => SbpsInventory.Parse(json));
        }

        [Test]
        public void Arpabet_BlueBanana_MapsToSbps()
        {
            var mapped = Load().Arpabet.MapSequence("B L UW1 B AH0 N AE1 N AH0");

            Assert.That(mapped, Is.EqualTo(new[] { "b", "l", "uː", "b", "ə", "n", "æ", "n", "ə" }));
        }

        [TestCase("AH0", "ə")]
        [TestCase("AH1", "ʌ")]
        [TestCase("AH2", "ʌ")]
        [TestCase("ER0", "ə")]
        [TestCase("ER1", "ɜː")]
        [TestCase("iy1", "iː")]
        [TestCase("CH", "tʃ")]
        [TestCase("JH", "dʒ")]
        [TestCase("G", "ɡ")]
        public void Arpabet_StressHandling(string arpabet, string expected)
        {
            Assert.That(Load().Arpabet.Map(arpabet), Is.EqualTo(expected));
        }

        [Test]
        public void Arpabet_UnknownToken_IsReported()
        {
            var sbps = Load();

            Assert.That(sbps.Arpabet.Map("QQ"), Is.Null);
            var ex = Assert.Throws<FormatException>(() => sbps.Arpabet.MapSequence("B QQ1"));
            StringAssert.Contains("QQ1", ex.Message);
        }

        [Test]
        public void Normaliser_StripsStressAndDiacritics_KeepsLength()
        {
            var normaliser = new PhonemeNormaliser(Load());

            Assert.That(normaliser.Normalise("ˈiː"), Is.EqualTo("iː"));
            Assert.That(normaliser.Normalise("ˌɑː"), Is.EqualTo("ɑː"));
            Assert.That(normaliser.Normalise("n̩"), Is.EqualTo("n"), "syllabic mark");
            Assert.That(normaliser.Normalise("tʰ"), Is.EqualTo("t"), "aspiration");
            Assert.That(normaliser.Normalise("ã"), Is.EqualTo("a"), "precomposed nasal vowel");
            Assert.That(normaliser.Normalise("uː"), Is.EqualTo("uː"), "length is part of the symbol (P00 Q4)");
        }

        [Test]
        public void Normaliser_DropsSilenceAndPadding_KeepingOrder()
        {
            var normaliser = new PhonemeNormaliser(Load());

            var result = normaliser.NormaliseAll(new[] { "sil", "b", "<pad>", "ˈl", "|", "uː", "sp", "spn", "<unk>", "ˈ" });

            Assert.That(result, Is.EqualTo(new[] { "b", "l", "uː" }));
        }
    }
}
