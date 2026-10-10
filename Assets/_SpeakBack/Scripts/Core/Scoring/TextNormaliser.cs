using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Uprise.SpeakBack.Core.Scoring
{
    /// <summary>
    /// Text normalisation for word and character scorers (Phase 1 §12), applied identically to target and
    /// transcript: Unicode NFKC, invariant lowercase, punctuation and symbols → space, whitespace collapsed.
    /// </summary>
    /// <remarks>
    /// Unity's Mono class library (also used by IL2CPP) implements NFKC only partly: full-width forms fold,
    /// but ligatures such as "ﬁ" stay unexpanded, whereas .NET and Python expand them. Target text and
    /// English ASR transcripts don't contain such characters, so scores are unaffected in practice; the
    /// Python harness must not rely on compatibility folding beyond full-width forms for parity.
    /// </remarks>
    public static class TextNormaliser
    {
        /// <summary>The normalised string: lowercase words separated by single spaces.</summary>
        public static string Normalise(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var folded = text.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            var builder = new StringBuilder(folded.Length);
            var pendingSpace = false;
            foreach (var c in folded)
            {
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSymbol(c) || char.IsControl(c))
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }

                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        /// <summary>Word tokens for <c>word-ed-v1</c>.</summary>
        public static IReadOnlyList<string> Words(string text)
        {
            var normalised = Normalise(text);
            return normalised.Length == 0
                ? Array.Empty<string>()
                : normalised.Split(' ');
        }

        /// <summary>Character tokens (text elements, spaces removed) for <c>char-ed-v1</c>.</summary>
        public static IReadOnlyList<string> Characters(string text)
        {
            var normalised = Normalise(text).Replace(" ", string.Empty);
            var result = new List<string>(normalised.Length);
            var elements = StringInfo.GetTextElementEnumerator(normalised);
            while (elements.MoveNext())
            {
                result.Add((string)elements.Current);
            }

            return result;
        }
    }
}
