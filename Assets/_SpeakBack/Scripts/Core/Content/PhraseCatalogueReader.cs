using System;
using System.Collections.Generic;
using System.Linq;

namespace Uprise.SpeakBack.Core.Content
{
    /// <summary>
    /// Reads a validated phrase catalogue into <see cref="PhraseInfo"/> objects in file order.
    /// Refuses catalogues with validation errors; warnings (e.g. unreviewed phonemes) are returned for display.
    /// </summary>
    public static class PhraseCatalogueReader
    {
        /// <summary>A read catalogue: the phrases plus any warnings.</summary>
        public sealed class Result
        {
            internal Result(IReadOnlyList<PhraseInfo> phrases, IReadOnlyList<CatalogueIssue> warnings)
            {
                Phrases = phrases;
                Warnings = warnings;
            }

            /// <summary>Phrases in catalogue order.</summary>
            public IReadOnlyList<PhraseInfo> Phrases { get; }

            /// <summary>Non-blocking issues.</summary>
            public IReadOnlyList<CatalogueIssue> Warnings { get; }
        }

        /// <summary>Validates and reads <paramref name="phrasesJson"/>; throws <see cref="FormatException"/> listing every error.</summary>
        public static Result Read(string phrasesJson, SbpsInventory sbps)
        {
            var issues = PhraseCatalogueValidator.Validate(phrasesJson, sbps);
            var errors = issues.Where(i => i.Severity == IssueSeverity.Error).ToList();
            if (errors.Count > 0)
            {
                throw new FormatException("Phrase catalogue has errors:\n" + string.Join("\n", errors));
            }

            var root = MiniJson.ParseObject(phrasesJson);
            var phrases = new List<PhraseInfo>();
            foreach (var item in JsonValues.Array(root, "phrases"))
            {
                var phrase = (IReadOnlyDictionary<string, object>)item;
                var alternates = JsonValues.Has(phrase, "alternatePronunciations")
                    ? JsonValues.Array(phrase, "alternatePronunciations").Select((a, i) => JsonValues.ToStrings(a, $"alternatePronunciations[{i}]")).ToArray()
                    : Array.Empty<IReadOnlyList<string>>();

                phrases.Add(new PhraseInfo(
                    JsonValues.String(phrase, "id"),
                    JsonValues.String(phrase, "targetText"),
                    JsonValues.StringArray(phrase, "phonemes"),
                    JsonValues.Int(phrase, "contentVersion"),
                    JsonValues.String(phrase, "phonemeVersion"),
                    alternates));
            }

            return new Result(phrases, issues.Where(i => i.Severity == IssueSeverity.Warning).ToList());
        }
    }
}
