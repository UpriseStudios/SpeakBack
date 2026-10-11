using System;
using UnityEngine;
using Uprise.SpeakBack.Core.Content;

namespace Uprise.SpeakBack.Gameplay
{
    /// <summary>
    /// Loads the phrase catalogue and SBPS from TextAssets synced from <c>data/</c> (P00 Q8:
    /// SpeakBack/Sync Data). P07 replaces this with PhraseDefinition assets; until then it is the runtime source.
    /// </summary>
    public static class PhraseCatalogueLoader
    {
        /// <summary>Parses SBPS, then validates and reads the phrases; throws <see cref="FormatException"/> on content errors.</summary>
        public static PhraseCatalogueReader.Result Load(TextAsset phrasesJson, TextAsset sbpsJson, out SbpsInventory sbps)
        {
            if (phrasesJson == null) throw new ArgumentNullException(nameof(phrasesJson));
            if (sbpsJson == null) throw new ArgumentNullException(nameof(sbpsJson));

            sbps = SbpsInventory.Parse(sbpsJson.text);
            return PhraseCatalogueReader.Read(phrasesJson.text, sbps);
        }
    }
}
