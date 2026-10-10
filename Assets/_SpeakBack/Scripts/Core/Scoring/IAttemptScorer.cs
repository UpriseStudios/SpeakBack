using System.Threading;
using System.Threading.Tasks;
using Uprise.SpeakBack.Core.Audio;
using Uprise.SpeakBack.Core.Content;

namespace Uprise.SpeakBack.Core.Scoring
{
    /// <summary>
    /// Scores one attempt (Master §27). Implementations are chosen by the bake-off; gameplay depends only
    /// on this interface. Uses Core's <see cref="PhraseInfo"/> rather than the PhraseDefinition
    /// ScriptableObject so Core stays engine-free (P00 finding A3).
    /// </summary>
    public interface IAttemptScorer
    {
        /// <summary>Version recorded with every outcome, e.g. "phoneme-ed-v1".</summary>
        string ScorerVersion { get; }

        /// <summary>Loads the engine and model once per session without blocking the UI.</summary>
        Task InitializeAsync(CancellationToken token);

        /// <summary>
        /// Scores <paramref name="forwardFacingMono"/>: the FlippedAttempt at the scorer rate, i.e.
        /// <see cref="AudioProcessResult.ScorerInput"/>. Failures return a Failed outcome, never a zero.
        /// </summary>
        Task<ScoringOutcome> ScoreAsync(AudioBuffer forwardFacingMono, PhraseInfo phrase, CancellationToken token);
    }

    /// <summary>
    /// Engine adapter beneath recognition-based scorers (Master §27): owns the engine and model lifecycle,
    /// PCM conversion and serialised native work. Phoneme output is mapped into SBPS by the adapter.
    /// </summary>
    public interface ISpeechRecognitionProvider
    {
        /// <summary>Recognises <paramref name="audio"/>. Failures are typed results, not exceptions.</summary>
        Task<RecognitionResult> RecognizeAsync(AudioBuffer audio, RecognitionContext context, CancellationToken token);
    }
}
