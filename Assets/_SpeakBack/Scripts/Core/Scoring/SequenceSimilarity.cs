using System;
using System.Collections.Generic;

namespace Uprise.SpeakBack.Core.Scoring
{
    /// <summary>
    /// Deterministic token edit distance shared by every scorer (Phase 1 §12): Levenshtein with
    /// insert, delete and substitute all costing 1; tokens compared ordinally.
    /// </summary>
    public static class SequenceSimilarity
    {
        /// <summary>Edit distance between <paramref name="target"/> and <paramref name="observed"/>. O(n·m) time, O(min(n, m)) memory.</summary>
        public static int EditDistance(IReadOnlyList<string> target, IReadOnlyList<string> observed)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (observed == null) throw new ArgumentNullException(nameof(observed));

            // Distance is symmetric, so keep the shorter sequence as the row to bound memory.
            var longer = target.Count >= observed.Count ? target : observed;
            var shorter = ReferenceEquals(longer, target) ? observed : target;

            var previous = new int[shorter.Count + 1];
            var current = new int[shorter.Count + 1];
            for (var j = 0; j <= shorter.Count; j++) previous[j] = j;

            for (var i = 1; i <= longer.Count; i++)
            {
                current[0] = i;
                var token = longer[i - 1];
                for (var j = 1; j <= shorter.Count; j++)
                {
                    var substitution = previous[j - 1] + (string.Equals(token, shorter[j - 1], StringComparison.Ordinal) ? 0 : 1);
                    var deletion = previous[j] + 1;
                    var insertion = current[j - 1] + 1;
                    current[j] = Math.Min(substitution, Math.Min(deletion, insertion));
                }

                var swap = previous;
                previous = current;
                current = swap;
            }

            return previous[shorter.Count];
        }
    }
}
