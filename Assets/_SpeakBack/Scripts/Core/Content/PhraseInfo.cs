using System;
using System.Collections.Generic;
using System.Linq;

namespace Uprise.SpeakBack.Core.Content
{
    /// <summary>
    /// Engine-free view of a phrase for scorers (P03, P00 finding A3). Runtime converts its
    /// <c>PhraseDefinition</c> ScriptableObject into this, so Core never sees Unity types. Immutable.
    /// </summary>
    public sealed class PhraseInfo
    {
        /// <summary>Creates phrase info; copies the sequences.</summary>
        public PhraseInfo(
            string id,
            string targetText,
            IReadOnlyList<string> phonemes,
            int contentVersion,
            string phonemeVersion,
            IReadOnlyList<IReadOnlyList<string>> alternatePronunciations = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Phrase ID is required.", nameof(id));
            Id = id;
            TargetText = targetText ?? throw new ArgumentNullException(nameof(targetText));
            Phonemes = (phonemes ?? throw new ArgumentNullException(nameof(phonemes))).ToArray();
            ContentVersion = contentVersion;
            PhonemeVersion = phonemeVersion;
            AlternatePronunciations = (alternatePronunciations ?? Array.Empty<IReadOnlyList<string>>())
                .Select(a => (IReadOnlyList<string>)(a ?? throw new ArgumentException("Null alternate pronunciation.")).ToArray())
                .ToArray();
        }

        /// <summary>Stable phrase ID, e.g. "sb_en_001".</summary>
        public string Id { get; }

        /// <summary>Forward target text shown to the player.</summary>
        public string TargetText { get; }

        /// <summary>Primary pronunciation in SBPS.</summary>
        public IReadOnlyList<string> Phonemes { get; }

        /// <summary>Other accepted pronunciations in SBPS (used only by best-of scorers).</summary>
        public IReadOnlyList<IReadOnlyList<string>> AlternatePronunciations { get; }

        /// <summary>Content version of the phrase entry.</summary>
        public int ContentVersion { get; }

        /// <summary>Phone-set version the phonemes are written in, e.g. "sbps-v1".</summary>
        public string PhonemeVersion { get; }
    }
}
