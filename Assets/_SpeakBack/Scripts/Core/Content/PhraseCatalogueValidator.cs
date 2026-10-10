using System;
using System.Collections.Generic;
using System.Linq;

namespace Uprise.SpeakBack.Core.Content
{
    /// <summary>How serious a catalogue issue is.</summary>
    public enum IssueSeverity
    {
        /// <summary>Allowed during development but must be resolved before release (e.g. unreviewed phonemes).</summary>
        Warning,

        /// <summary>The catalogue cannot be used as is.</summary>
        Error,
    }

    /// <summary>One validation finding, tied to a phrase where possible.</summary>
    public sealed class CatalogueIssue
    {
        /// <summary>Creates an issue.</summary>
        public CatalogueIssue(IssueSeverity severity, string phraseId, string code, string message)
        {
            Severity = severity;
            PhraseId = phraseId;
            Code = code;
            Message = message;
        }

        /// <summary>Error or warning.</summary>
        public IssueSeverity Severity { get; }

        /// <summary>Phrase the issue belongs to, or null for file-level issues.</summary>
        public string PhraseId { get; }

        /// <summary>Stable machine-readable code, e.g. "UnknownPhoneme".</summary>
        public string Code { get; }

        /// <summary>Human-readable explanation for tools and reports.</summary>
        public string Message { get; }

        /// <inheritdoc/>
        public override string ToString() => $"{Severity} {Code} [{PhraseId ?? "-"}]: {Message}";
    }

    /// <summary>
    /// Checks a phrase catalogue (the JSON of <c>data/phrases/*.json</c>) against SBPS: required fields,
    /// unique IDs, known difficulty, non-empty phonemes, every phoneme and alternate in SBPS, declared
    /// counts and phone-set versions. Unreviewed phonemes are warnings, not errors. Audio is checked
    /// later by the content pipeline (P07).
    /// </summary>
    public static class PhraseCatalogueValidator
    {
        /// <summary>Difficulty tiers (Master §7).</summary>
        public static readonly IReadOnlyList<string> Difficulties = new[] { "Easy", "Tricky", "Hard", "Brutal", "Legendary" };

        /// <summary>The phonemeStatus value that marks Jamie's sign-off.</summary>
        public const string ReviewedStatus = "reviewed";

        /// <summary>Validates <paramref name="phrasesJson"/>; returns every issue found (empty when clean).</summary>
        public static IReadOnlyList<CatalogueIssue> Validate(string phrasesJson, SbpsInventory sbps)
        {
            if (sbps == null) throw new ArgumentNullException(nameof(sbps));
            var issues = new List<CatalogueIssue>();

            IReadOnlyDictionary<string, object> root;
            try
            {
                root = MiniJson.ParseObject(phrasesJson);
            }
            catch (FormatException e)
            {
                issues.Add(Error(null, "InvalidJson", e.Message));
                return issues;
            }

            if (!TryGet(() => JsonValues.String(root, "phoneSet"), out var phoneSet, out var problem))
            {
                issues.Add(Error(null, "MissingField", problem));
            }
            else if (phoneSet != sbps.Version)
            {
                issues.Add(Error(null, "PhoneSetMismatch", $"Catalogue uses '{phoneSet}' but the inventory is '{sbps.Version}'."));
            }

            if (!TryGet(() => JsonValues.Array(root, "phrases"), out var phrases, out problem))
            {
                issues.Add(Error(null, "MissingField", problem));
                return issues;
            }

            if (phrases.Count == 0) issues.Add(Error(null, "NoPhrases", "The catalogue has no phrases."));

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < phrases.Count; i++)
            {
                if (!(phrases[i] is IReadOnlyDictionary<string, object> phrase))
                {
                    issues.Add(Error(null, "InvalidPhrase", $"phrases[{i}] is not an object."));
                    continue;
                }

                ValidatePhrase(phrase, i, sbps, seenIds, issues);
            }

            return issues;
        }

        private static void ValidatePhrase(
            IReadOnlyDictionary<string, object> phrase, int index, SbpsInventory sbps, HashSet<string> seenIds, List<CatalogueIssue> issues)
        {
            if (!TryGet(() => JsonValues.String(phrase, "id"), out var id, out var problem) || string.IsNullOrWhiteSpace(id))
            {
                issues.Add(Error(null, "MissingId", $"phrases[{index}]: {problem ?? "empty id"}"));
                id = $"phrases[{index}]";
            }
            else if (!seenIds.Add(id))
            {
                issues.Add(Error(id, "DuplicateId", $"ID '{id}' appears more than once."));
            }

            foreach (var field in new[] { "targetText", "locale", "category", "difficulty", "phonemeStatus", "phonemeVersion" })
            {
                if (!TryGet(() => JsonValues.String(phrase, field), out var value, out problem) || string.IsNullOrWhiteSpace(value))
                {
                    issues.Add(Error(id, "MissingField", problem ?? $"'{field}' is empty."));
                }
            }

            if (!TryGet(() => JsonValues.Int(phrase, "contentVersion"), out _, out problem))
            {
                issues.Add(Error(id, "MissingField", problem));
            }

            if (TryGet(() => JsonValues.String(phrase, "difficulty"), out var difficulty, out _) && !Difficulties.Contains(difficulty))
            {
                issues.Add(Error(id, "UnknownDifficulty", $"'{difficulty}' is not one of {string.Join("/", Difficulties)}."));
            }

            if (TryGet(() => JsonValues.String(phrase, "phonemeVersion"), out var phonemeVersion, out _) && phonemeVersion != sbps.Version)
            {
                issues.Add(Error(id, "PhoneSetMismatch", $"phonemeVersion '{phonemeVersion}' is not '{sbps.Version}'."));
            }

            if (!TryGet(() => JsonValues.StringArray(phrase, "phonemes"), out var phonemes, out problem))
            {
                issues.Add(Error(id, "MissingField", problem));
            }
            else
            {
                if (phonemes.Count == 0) issues.Add(Error(id, "EmptyPhonemes", "Phoneme sequence is empty."));
                CheckSymbols(phonemes, "phonemes", id, sbps, issues);

                if (JsonValues.Has(phrase, "phonemeCount") &&
                    TryGet(() => JsonValues.Int(phrase, "phonemeCount"), out var declared, out _) && declared != phonemes.Count)
                {
                    issues.Add(Error(id, "PhonemeCountMismatch", $"phonemeCount is {declared} but there are {phonemes.Count} phonemes."));
                }
            }

            if (JsonValues.Has(phrase, "alternatePronunciations"))
            {
                if (!TryGet(() => JsonValues.Array(phrase, "alternatePronunciations"), out var alternates, out problem))
                {
                    issues.Add(Error(id, "InvalidField", problem));
                }
                else
                {
                    for (var a = 0; a < alternates.Count; a++)
                    {
                        var label = $"alternatePronunciations[{a}]";
                        if (!TryGet(() => JsonValues.ToStrings(alternates[a], label), out var alternate, out problem))
                        {
                            issues.Add(Error(id, "InvalidField", problem));
                            continue;
                        }

                        if (alternate.Count == 0) issues.Add(Error(id, "EmptyPhonemes", $"{label} is empty."));
                        CheckSymbols(alternate, label, id, sbps, issues);
                    }
                }
            }

            if (TryGet(() => JsonValues.String(phrase, "phonemeStatus"), out var status, out _) && status != ReviewedStatus)
            {
                issues.Add(new CatalogueIssue(IssueSeverity.Warning, id, "PhonemesNotReviewed",
                    $"phonemeStatus is '{status}'; phonemes need sign-off (checklists/phoneme-signoff.md)."));
            }
        }

        private static void CheckSymbols(IReadOnlyList<string> tokens, string label, string id, SbpsInventory sbps, List<CatalogueIssue> issues)
        {
            for (var p = 0; p < tokens.Count; p++)
            {
                if (!sbps.Contains(tokens[p]))
                {
                    var codepoints = string.Join(" ", tokens[p].Select(c => $"U+{(int)c:X4}"));
                    issues.Add(Error(id, "UnknownPhoneme", $"{label}[{p}] '{tokens[p]}' ({codepoints}) is not in {sbps.Version}."));
                }
            }
        }

        private static CatalogueIssue Error(string phraseId, string code, string message) =>
            new CatalogueIssue(IssueSeverity.Error, phraseId, code, message);

        private static bool TryGet<T>(Func<T> read, out T value, out string problem)
        {
            try
            {
                value = read();
                problem = null;
                return true;
            }
            catch (FormatException e)
            {
                value = default;
                problem = e.Message;
                return false;
            }
        }
    }
}
