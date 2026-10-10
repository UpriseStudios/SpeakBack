using System;
using System.Collections.Generic;
using System.Text;

namespace Uprise.SpeakBack.Core.Content
{
    /// <summary>
    /// Prepares recognised phoneme tokens for scoring (Phase 1 §12): strip stress and diacritic marks,
    /// return NFC, drop silence and padding tokens. Length (ː) is kept because it is part of
    /// SBPS symbols (D-011, P00 Q4). Mapping an engine's phone set into SBPS happens before this, in
    /// that engine's versioned mapping table.
    /// </summary>
    public sealed class PhonemeNormaliser
    {
        private readonly IReadOnlyList<string> _stripMarks;
        private readonly HashSet<string> _dropTokens;

        /// <summary>Uses the marks and drop tokens from <paramref name="inventory"/>.</summary>
        public PhonemeNormaliser(SbpsInventory inventory)
            : this(inventory?.StripMarks ?? throw new ArgumentNullException(nameof(inventory)), inventory.DropTokens)
        {
        }

        /// <summary>Uses explicit marks and drop tokens.</summary>
        public PhonemeNormaliser(IReadOnlyList<string> stripMarks, IReadOnlyList<string> dropTokens)
        {
            _stripMarks = stripMarks ?? throw new ArgumentNullException(nameof(stripMarks));
            _dropTokens = new HashSet<string>(dropTokens ?? throw new ArgumentNullException(nameof(dropTokens)), StringComparer.Ordinal);
        }

        /// <summary>The normalised token, or null when it should be dropped.</summary>
        public string Normalise(string token)
        {
            if (token == null) return null;
            var trimmed = token.Trim();
            if (_dropTokens.Contains(trimmed)) return null;

            // Decompose first so a mark folded into a precomposed letter (e.g. ã = a + U+0303) can be
            // stripped, then recompose. No SBPS symbol has a canonical decomposition.
            var builder = new StringBuilder(trimmed.Normalize(NormalizationForm.FormD));
            foreach (var mark in _stripMarks)
            {
                builder.Replace(mark, string.Empty);
            }

            var result = builder.ToString().Normalize(NormalizationForm.FormC).Trim();
            return result.Length == 0 || _dropTokens.Contains(result) ? null : result;
        }

        /// <summary>Normalises a sequence, dropping removed tokens and keeping order.</summary>
        public IReadOnlyList<string> NormaliseAll(IEnumerable<string> tokens)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            var result = new List<string>();
            foreach (var token in tokens)
            {
                var normalised = Normalise(token);
                if (normalised != null) result.Add(normalised);
            }

            return result;
        }
    }
}
