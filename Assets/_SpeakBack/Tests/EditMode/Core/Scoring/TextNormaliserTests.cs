using NUnit.Framework;
using Uprise.SpeakBack.Core.Scoring;

namespace Uprise.SpeakBack.Core.Tests.Scoring
{
    public sealed class TextNormaliserTests
    {
        [TestCase("Never trust a talking penguin", "never trust a talking penguin")]
        [TestCase("  Blue   BANANA!  ", "blue banana")]
        [TestCase("Hello, robot.", "hello robot")]
        [TestCase("hello-robot", "hello robot")]
        [TestCase("Please pass the biscuits?!", "please pass the biscuits")]
        [TestCase("Ｂｌｕｅ　ｂａｎａｎａ", "blue banana")] // NFKC folds full-width forms and ideographic space
        // No ligature case (e.g. "ﬁ"): Unity's Mono/IL2CPP NFKC leaves it unexpanded while .NET expands it.
        // See TextNormaliser remarks; shared tests only cover behaviour both runtimes agree on.
        [TestCase("tab\tand\nnewline", "tab and newline")]
        [TestCase("...", "")]
        [TestCase("", "")]
        public void Normalise(string input, string expected)
        {
            Assert.That(TextNormaliser.Normalise(input), Is.EqualTo(expected));
        }

        [Test]
        public void Words_SplitNormalisedText()
        {
            Assert.That(TextNormaliser.Words("The robot stole MY sandwich."),
                Is.EqualTo(new[] { "the", "robot", "stole", "my", "sandwich" }));
        }

        [Test]
        public void Words_EmptyText_GivesNoTokens()
        {
            Assert.That(TextNormaliser.Words("  ?! "), Is.Empty);
        }

        [Test]
        public void Characters_DropSpaces()
        {
            Assert.That(TextNormaliser.Characters("Blue banana"),
                Is.EqualTo(new[] { "b", "l", "u", "e", "b", "a", "n", "a", "n", "a" }));
        }

        [Test]
        public void Characters_KeepCombinedLettersAsOneElement()
        {
            // "e" + combining acute composes under NFKC to one element.
            Assert.That(TextNormaliser.Characters("café"), Is.EqualTo(new[] { "c", "a", "f", "é" }));
        }

        [Test]
        public void TargetAndTranscript_NormaliseIdentically()
        {
            Assert.That(TextNormaliser.Words("never trust the talking penguin"),
                Is.EqualTo(TextNormaliser.Words("NEVER, trust... the TALKING penguin!")));
        }
    }
}
