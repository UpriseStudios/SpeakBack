using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Uprise.SpeakBack.Dev.Bakeoff
{
    /// <summary>
    /// Pseudonymous participant details for one bake-off session (Phase 1 §11.1 step 0, §15; D-017).
    /// Only a speaker code, coarse self-chosen group labels and a consent reference are kept. There are
    /// no names, contact details or free-text fields.
    /// </summary>
    public sealed class BakeoffParticipant
    {
        /// <summary>Coarse accent regions (fixed list; the participant chooses).</summary>
        public static readonly IReadOnlyList<string> AccentGroups = new[]
        {
            "Southern England", "Midlands", "Northern England", "Scotland", "Wales", "Northern Ireland", "Other UK", "Non-UK",
        };

        /// <summary>Coarse voice ranges (fixed list; the participant chooses).</summary>
        public static readonly IReadOnlyList<string> VoiceGroups = new[] { "higher", "middle", "lower" };

        private static readonly Regex SpeakerPattern = new Regex(@"^S\d{2,3}$");
        private static readonly Regex ConsentPattern = new Regex(@"^[A-Za-z0-9-]{1,24}$");

        private BakeoffParticipant(string speakerCode, string accent, string voice, string consentRef)
        {
            SpeakerCode = speakerCode;
            Accent = accent;
            Voice = voice;
            ConsentRef = consentRef;
        }

        /// <summary>Pseudonymous code, e.g. "S01".</summary>
        public string SpeakerCode { get; }

        /// <summary>One of <see cref="AccentGroups"/>.</summary>
        public string Accent { get; }

        /// <summary>One of <see cref="VoiceGroups"/>.</summary>
        public string Voice { get; }

        /// <summary>Reference to the signed consent form on file (an ID, not a name).</summary>
        public string ConsentRef { get; }

        /// <summary>
        /// Validates the setup form. <paramref name="accentIndex"/> and <paramref name="voiceIndex"/> are -1 when
        /// not chosen. Returns null and a problem description when anything is missing or malformed, including
        /// an unticked consent confirmation.
        /// </summary>
        public static BakeoffParticipant TryCreate(
            string speakerCode, int accentIndex, int voiceIndex, string consentRef, bool consentConfirmed, out string problem)
        {
            var code = (speakerCode ?? string.Empty).Trim().ToUpperInvariant();
            var consent = (consentRef ?? string.Empty).Trim();

            if (!SpeakerPattern.IsMatch(code)) problem = "Speaker code must look like S01 (S plus 2–3 digits).";
            else if (accentIndex < 0 || accentIndex >= AccentGroups.Count) problem = "Choose an accent group (the participant picks).";
            else if (voiceIndex < 0 || voiceIndex >= VoiceGroups.Count) problem = "Choose a voice range (the participant picks).";
            else if (!ConsentPattern.IsMatch(consent)) problem = "Consent reference must be an ID: letters, digits and dashes only (no names).";
            else if (!consentConfirmed) problem = "Tick the consent confirmation: signed form on file and participant is 18 or over.";
            else problem = null;

            return problem == null ? new BakeoffParticipant(code, AccentGroups[accentIndex], VoiceGroups[voiceIndex], consent) : null;
        }
    }
}
