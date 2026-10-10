using System;
using System.Collections.Generic;
using System.Linq;

namespace Uprise.SpeakBack.Core.Content
{
    /// <summary>
    /// The SpeakBack Phone Set (D-011), loaded from the text of <c>data/phonemes/sbps.json</c>.
    /// Core does no file IO: Runtime (TextAsset) and tools/tests (file read) supply the JSON.
    /// Multi-character symbols (tʃ, iː, aɪ…) are single tokens; length (ː) is part of the symbol (P00 Q4).
    /// </summary>
    public sealed class SbpsInventory
    {
        private readonly HashSet<string> _symbols;

        private SbpsInventory(
            string version, IReadOnlyList<string> consonants, IReadOnlyList<string> vowels,
            IReadOnlyList<string> stripMarks, IReadOnlyList<string> dropTokens, ArpabetMapping arpabet)
        {
            Version = version;
            Consonants = consonants;
            Vowels = vowels;
            Symbols = consonants.Concat(vowels).ToArray();
            _symbols = new HashSet<string>(Symbols, StringComparer.Ordinal);
            StripMarks = stripMarks;
            DropTokens = dropTokens;
            Arpabet = arpabet;
        }

        /// <summary>Inventory version, e.g. "sbps-v1".</summary>
        public string Version { get; }

        /// <summary>Consonant symbols in file order.</summary>
        public IReadOnlyList<string> Consonants { get; }

        /// <summary>Vowel symbols in file order.</summary>
        public IReadOnlyList<string> Vowels { get; }

        /// <summary>All symbols: consonants then vowels.</summary>
        public IReadOnlyList<string> Symbols { get; }

        /// <summary>Stress and diacritic marks removed from recognised tokens before scoring.</summary>
        public IReadOnlyList<string> StripMarks { get; }

        /// <summary>Silence and padding tokens dropped before scoring.</summary>
        public IReadOnlyList<string> DropTokens { get; }

        /// <summary>ARPAbet (CMUdict) → SBPS mapping table.</summary>
        public ArpabetMapping Arpabet { get; }

        /// <summary>Whether <paramref name="token"/> is an SBPS symbol (exact, ordinal).</summary>
        public bool Contains(string token) => token != null && _symbols.Contains(token);

        /// <summary>Parses the SBPS JSON; throws <see cref="FormatException"/> on missing fields or duplicate symbols.</summary>
        public static SbpsInventory Parse(string json)
        {
            var root = MiniJson.ParseObject(json);
            var consonants = JsonValues.StringArray(root, "consonants");
            var vowels = JsonValues.StringArray(root, "vowels");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var symbol in consonants.Concat(vowels))
            {
                if (string.IsNullOrEmpty(symbol)) throw new FormatException("Empty SBPS symbol.");
                if (!seen.Add(symbol)) throw new FormatException($"Duplicate SBPS symbol '{symbol}'.");
            }

            var normalisation = JsonValues.Object(root, "normalisation");
            var arpabetJson = JsonValues.Object(JsonValues.Object(root, "mappings"), "arpabet");
            var table = JsonValues.Object(arpabetJson, "table");
            var arpabetTable = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in table)
            {
                var target = pair.Value as string ?? throw new FormatException($"ARPAbet '{pair.Key}' must map to a string.");
                if (!seen.Contains(target)) throw new FormatException($"ARPAbet '{pair.Key}' maps to '{target}', which is not in SBPS.");
                arpabetTable[pair.Key] = target;
            }

            return new SbpsInventory(
                JsonValues.String(root, "version"),
                consonants,
                vowels,
                JsonValues.StringArray(normalisation, "stripMarks"),
                JsonValues.StringArray(normalisation, "dropTokens"),
                new ArpabetMapping(JsonValues.String(arpabetJson, "version"), arpabetTable));
        }
    }

    /// <summary>
    /// ARPAbet → SBPS conversion following the rules in sbps.json: a token whose exact form is in the
    /// table (AH0, ER0: unstressed schwa) maps directly; otherwise stress digits are stripped before lookup.
    /// CMUdict is US English, so every mapped phrase still needs UK review.
    /// </summary>
    public sealed class ArpabetMapping
    {
        private readonly IReadOnlyDictionary<string, string> _table;

        internal ArpabetMapping(string version, IReadOnlyDictionary<string, string> table)
        {
            Version = version;
            _table = table;
        }

        /// <summary>Mapping table version, e.g. "arpabet-to-sbps-v1".</summary>
        public string Version { get; }

        /// <summary>SBPS symbol for one ARPAbet token, or null when the token is unknown.</summary>
        public string Map(string arpabetToken)
        {
            if (string.IsNullOrWhiteSpace(arpabetToken)) return null;
            var token = arpabetToken.Trim().ToUpperInvariant();
            if (_table.TryGetValue(token, out var exact)) return exact;
            var stripped = token.TrimEnd('0', '1', '2');
            return _table.TryGetValue(stripped, out var mapped) ? mapped : null;
        }

        /// <summary>Maps a whitespace-separated ARPAbet sequence; throws <see cref="FormatException"/> naming any unknown token.</summary>
        public IReadOnlyList<string> MapSequence(string arpabet)
        {
            if (arpabet == null) throw new ArgumentNullException(nameof(arpabet));
            var tokens = arpabet.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new string[tokens.Length];
            for (var i = 0; i < tokens.Length; i++)
            {
                result[i] = Map(tokens[i]) ?? throw new FormatException($"Unknown ARPAbet token '{tokens[i]}' at {i}.");
            }

            return result;
        }
    }
}
