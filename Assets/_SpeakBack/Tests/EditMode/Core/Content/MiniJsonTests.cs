using System;
using System.Collections.Generic;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Content;

namespace Uprise.SpeakBack.Core.Tests.Content
{
    public sealed class MiniJsonTests
    {
        [Test]
        public void Parses_NestedStructures()
        {
            var root = MiniJson.ParseObject("{\"a\": [1, 2.5, -3e2], \"b\": {\"c\": true, \"d\": null}, \"e\": false}");

            var a = (List<object>)root["a"];
            Assert.That(a, Is.EqualTo(new object[] { 1.0, 2.5, -300.0 }));
            var b = (Dictionary<string, object>)root["b"];
            Assert.That(b["c"], Is.EqualTo(true));
            Assert.That(b["d"], Is.Null);
            Assert.That(root["e"], Is.EqualTo(false));
        }

        [Test]
        public void Parses_StringEscapes_AndIpa()
        {
            var root = MiniJson.ParseObject("{\"s\": \"a\\\"b\\\\c\\n\\u0261\\u02D0\", \"ipa\": \"tʃ ɡ iː\"}");

            Assert.That(root["s"], Is.EqualTo("a\"b\\c\nɡː"));
            Assert.That(root["ipa"], Is.EqualTo("tʃ ɡ iː"));
        }

        [Test]
        public void Tolerates_LeadingBom()
        {
            Assert.That(MiniJson.Parse("﻿[1]"), Is.EqualTo(new object[] { 1.0 }));
        }

        [TestCase("")]
        [TestCase("{")]
        [TestCase("{\"a\":}")]
        [TestCase("{\"a\":1,}")]
        [TestCase("[1 2]")]
        [TestCase("{\"a\":1}{")]
        [TestCase("{\"a\":1,\"a\":2}")]
        [TestCase("\"unterminated")]
        [TestCase("{'a':1}")]
        [TestCase("tru")]
        public void Rejects_MalformedInput(string json)
        {
            Assert.Throws<FormatException>(() => MiniJson.Parse(json));
        }

        [Test]
        public void ParsesEveryRepoDataFile()
        {
            foreach (var path in new[]
            {
                "data/phonemes/sbps.json",
                "data/phrases/phase1_phrases.json",
                "data/golden/similarity_cases.json",
                "data/golden/verdict_cases.json",
                "data/dialogue/uprise_phase1.json",
            })
            {
                Assert.DoesNotThrow(() => MiniJson.ParseObject(TestData.ReadText(path)), path);
            }
        }

        [Test]
        public void JsonValues_NameTheBadField()
        {
            var root = MiniJson.ParseObject("{\"n\": 1.5, \"s\": 3}");

            StringAssert.Contains("'n'", Assert.Throws<FormatException>(() => JsonValues.Int(root, "n")).Message);
            StringAssert.Contains("'s'", Assert.Throws<FormatException>(() => JsonValues.String(root, "s")).Message);
            StringAssert.Contains("'missing'", Assert.Throws<FormatException>(() => JsonValues.String(root, "missing")).Message);
        }
    }
}
