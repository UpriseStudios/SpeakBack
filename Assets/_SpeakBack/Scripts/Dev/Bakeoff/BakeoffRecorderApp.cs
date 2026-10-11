using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Uprise.SpeakBack.Audio;
using Uprise.SpeakBack.Core.Audio;
using Uprise.SpeakBack.Core.Content;
using Uprise.SpeakBack.Gameplay;
using Uprise.SpeakBack.UI;

namespace Uprise.SpeakBack.Dev.Bakeoff
{
    /// <summary>
    /// Dev-only Bake-off Recorder (D-016, Phase 1 §11.1 step 0). Collects consented adult attempts on real
    /// phones through the game's own capture path (MicrophoneRecorder → AudioProcessor):
    /// <list type="number">
    /// <item>Setup: pseudonymous speaker code, self-chosen group labels, consent reference and a mandatory 18+/consent confirmation.</item>
    /// <item>Record references (operator): Jamie records each phrase forwards once; the reversed reference is generated.</item>
    /// <item>Session: reversed reference → participant holds and copies → instant reveal of the FlippedAttempt → Keep / Redo / Skip.</item>
    /// <item>Export: kept takes written as WAV + JSON sidecar; a summary shows adb pull commands and Delete session.</item>
    /// </list>
    /// Compiled only in the editor and SPEAKBACK_DEV builds; never in release.
    /// </summary>
    public sealed class BakeoffRecorderApp : MonoBehaviour
    {
        private enum Mode
        {
            References,
            Session,
        }

        private enum Step
        {
            Idle,
            Starting,
            Recording,
            Processing,
            Reviewing,
        }

        [SerializeField] private MicrophoneRecorder recorder;
        [SerializeField] private AudioPlayback playback;
        [SerializeField] private RectTransform uiRoot;
        [SerializeField] private TextAsset phrasesJson;
        [SerializeField] private TextAsset sbpsJson;
        [SerializeField] private string gameSceneName = "03_Game";
        [SerializeField] private int maxTakesPerPhrase = 2;

        private readonly ProcessorConfig _config = ProcessorConfig.Default;

        private IReadOnlyList<PhraseInfo> _phrases = Array.Empty<PhraseInfo>();
        private BakeoffStorage _storage;
        private MicrophonePermissionService _permission;
        private BakeoffEnvironment _environment;

        private Mode _mode;
        private Step _step;
        private int _phraseIndex;
        private int _attemptVersion;
        private CancellationTokenSource _attemptCts;
        private AudioProcessResult _pending;
        private CaptureDiagnostics _pendingCapture;

        private BakeoffParticipant _participant;
        private string _sessionId;
        private int _takesPerPhrase;
        private int[] _kept;
        private int _totalKept;

        private RectTransform _setupScreen;
        private RectTransform _phraseScreen;
        private RectTransform _summaryScreen;
        private TMP_InputField _speakerInput;
        private TMP_InputField _consentInput;
        private Func<int> _accentIndex;
        private Func<int> _voiceIndex;
        private Func<int> _takesIndex;
        private Action _resetAccent;
        private Action _resetVoice;
        private Button _consentButton;
        private bool _consentConfirmed;
        private TextMeshProUGUI _setupStatus;
        private TextMeshProUGUI _referencesStatus;
        private Button _deleteAllButton;
        private bool _deleteAllArmed;

        private TextMeshProUGUI _phraseHeader;
        private TextMeshProUGUI _phraseText;
        private TextMeshProUGUI _phraseStatus;
        private TextMeshProUGUI _elapsedText;
        private Image _meterFill;
        private HoldRecordButton _hold;
        private TextMeshProUGUI _holdLabel;
        private Button _playReference;
        private Button _keep;
        private Button _redo;
        private Button _replay;
        private Button _hearRaw;
        private Button _saveReference;
        private Button _playForward;
        private Button _playReversed;
        private Button _skip;
        private Button _finish;

        private TextMeshProUGUI _summaryText;
        private Button _deleteSessionButton;
        private bool _deleteSessionArmed;
        private string _pullCommand;

        private PhraseInfo CurrentPhrase => _phrases[_phraseIndex];

        private void Awake()
        {
            _storage = new BakeoffStorage(Application.persistentDataPath);
            _permission = new MicrophonePermissionService(MicrophonePermissionService.CreatePlatformBackend(() => recorder.HasAnyDevice));
            _environment = new BakeoffEnvironment(SystemInfo.deviceModel, SystemInfo.operatingSystem, Application.version, Application.unityVersion);

            BuildSetupScreen();
            BuildPhraseScreen();
            BuildSummaryScreen();

            try
            {
                _phrases = PhraseCatalogueLoader.Load(phrasesJson, sbpsJson, out _).Phrases;
            }
            catch (Exception e)
            {
                _phrases = Array.Empty<PhraseInfo>();
                _setupStatus.text = "Phrase data failed to load (run SpeakBack/Sync Data and rebuild): " + e.Message;
            }

            recorder.Completed += OnCaptureCompleted;
            _hold.BeginRequested += OnHoldBegin;
            _hold.ReleaseRequested += OnHoldRelease;
            _hold.CancelRequested += OnHoldCancel;

            ShowSetup();
        }

        private void OnDestroy()
        {
            if (recorder != null) recorder.Completed -= OnCaptureCompleted;
            CancelAttempt();
        }

        private void Update()
        {
            if (_step != Step.Recording) return;
            DevUi.SetMeter(_meterFill, recorder.Amplitude * 4f);
            _elapsedText.text = $"{recorder.ElapsedSeconds:0.0} s / {recorder.MaxSeconds} s";
        }

        // ---------------------------------------------------------------- setup

        private void BuildSetupScreen()
        {
            _setupScreen = DevUi.Screen(uiRoot, "SetupScreen");
            DevUi.Label(_setupScreen, "SpeakBack Bake-off Recorder", 56, 80, DevUi.Accent);
            DevUi.Label(_setupScreen, "Dev build only. Adults (18+) with signed consent only. No names here.", 34, 90, DevUi.Steel);
            _speakerInput = DevUi.Input(_setupScreen, "Speaker code, e.g. S01");
            _accentIndex = DevUi.Cycle(_setupScreen, "Accent", BakeoffParticipant.AccentGroups, -1, out _resetAccent);
            _voiceIndex = DevUi.Cycle(_setupScreen, "Voice", BakeoffParticipant.VoiceGroups, -1, out _resetVoice);
            _consentInput = DevUi.Input(_setupScreen, "Consent reference, e.g. C-2026-001");
            _takesIndex = DevUi.Cycle(_setupScreen, "Takes kept per phrase", TakeOptions(), 0, out _);
            _consentButton = DevUi.Button(_setupScreen, string.Empty, ToggleConsent, 140);
            RefreshConsentButton();
            _setupStatus = DevUi.Label(_setupScreen, string.Empty, 36, 110, DevUi.Danger);
            DevUi.Button(_setupScreen, "Start session", () => _ = StartSessionAsync(), 140, DevUi.Accent);
            DevUi.Spacer(_setupScreen);
            _referencesStatus = DevUi.Label(_setupScreen, string.Empty, 34, 60, DevUi.Steel);
            DevUi.Button(_setupScreen, "Record references (operator)", () => _ = StartReferencesAsync());
            var row = DevUi.Row(_setupScreen, 120);
            _deleteAllButton = DevUi.Button(row, "Delete ALL bake-off data", DeleteAll);
            DevUi.Button(row, "Open game scene", () => SceneManager.LoadScene(gameSceneName));
        }

        private IReadOnlyList<string> TakeOptions()
        {
            var options = new List<string>();
            for (var i = 1; i <= Math.Max(1, maxTakesPerPhrase); i++) options.Add(i.ToString());
            return options;
        }

        private void ToggleConsent()
        {
            _consentConfirmed = !_consentConfirmed;
            RefreshConsentButton();
        }

        private void RefreshConsentButton() =>
            DevUi.SetText(_consentButton, (_consentConfirmed ? "[X] " : "[  ] ") + "Signed consent form on file and participant confirmed 18+");

        private void ShowSetup()
        {
            CancelAttempt();
            playback.StopAll();
            _deleteAllArmed = false;
            DevUi.SetText(_deleteAllButton, "Delete ALL bake-off data");
            var recorded = 0;
            foreach (var phrase in _phrases)
            {
                if (_storage.HasReference(phrase.Id)) recorded++;
            }

            _referencesStatus.text = $"References recorded: {recorded}/{_phrases.Count}";
            Show(_setupScreen);
        }

        private async Task StartSessionAsync()
        {
            var participant = BakeoffParticipant.TryCreate(
                _speakerInput.text, _accentIndex(), _voiceIndex(), _consentInput.text, _consentConfirmed, out var problem);
            if (participant == null)
            {
                _setupStatus.text = problem;
                return;
            }

            if (_phrases.Count == 0 || !await EnsureMicrophoneAsync(_setupStatus)) return;

            try
            {
                _sessionId = _storage.CreateSession(participant.SpeakerCode, DateTime.UtcNow);
            }
            catch (Exception e)
            {
                _setupStatus.text = "Could not create the session folder: " + e.Message;
                return;
            }

            _participant = participant;
            _takesPerPhrase = _takesIndex() + 1;
            _kept = new int[_phrases.Count];
            _totalKept = 0;
            _setupStatus.text = string.Empty;
            _mode = Mode.Session;
            Show(_phraseScreen);
            EnterPhrase(0, autoplay: true);
        }

        private async Task StartReferencesAsync()
        {
            if (_phrases.Count == 0 || !await EnsureMicrophoneAsync(_setupStatus)) return;
            _setupStatus.text = string.Empty;
            _mode = Mode.References;
            Show(_phraseScreen);
            EnterPhrase(0, autoplay: false);
        }

        private async Task<bool> EnsureMicrophoneAsync(TextMeshProUGUI status)
        {
            status.text = "This tool records the microphone only while the button is held.";
            MicrophonePermissionState state;
            try
            {
                state = await _permission.EnsureMicrophoneAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            switch (state)
            {
                case MicrophonePermissionState.Granted:
                    status.text = string.Empty;
                    return true;
                case MicrophonePermissionState.DeniedPermanently:
                    status.text = "Microphone access is blocked. Opening app settings: allow Microphone, then come back.";
                    _permission.OpenAppSettings();
                    return false;
                case MicrophonePermissionState.Unsupported:
                    status.text = "No microphone found on this device.";
                    return false;
                default:
                    status.text = "Microphone permission was refused. Tap again to be asked again.";
                    return false;
            }
        }

        private void DeleteAll()
        {
            if (!_deleteAllArmed)
            {
                _deleteAllArmed = true;
                DevUi.SetText(_deleteAllButton, "Tap again to delete everything");
                return;
            }

            try
            {
                _storage.DeleteAll();
                _setupStatus.text = "All bake-off sessions and references deleted from this phone.";
            }
            catch (Exception e)
            {
                _setupStatus.text = "Delete failed: " + e.Message;
            }

            ShowSetup();
        }

        // ---------------------------------------------------------------- phrase screen

        private void BuildPhraseScreen()
        {
            _phraseScreen = DevUi.Screen(uiRoot, "PhraseScreen");
            _phraseHeader = DevUi.Label(_phraseScreen, string.Empty, 34, 100, DevUi.Steel);
            _phraseText = DevUi.Label(_phraseScreen, string.Empty, 72, 200);
            _phraseText.alignment = TextAlignmentOptions.Center;
            _phraseStatus = DevUi.Label(_phraseScreen, string.Empty, 40, 140, DevUi.Accent);
            _phraseStatus.alignment = TextAlignmentOptions.Center;
            _playReference = DevUi.Button(_phraseScreen, "Play reversed reference", () => _ = PlayReferenceAsync());
            DevUi.Meter(_phraseScreen, out _meterFill);
            _elapsedText = DevUi.Label(_phraseScreen, string.Empty, 32, 50, DevUi.Steel);
            _elapsedText.alignment = TextAlignmentOptions.Center;
            _hold = DevUi.HoldButton(_phraseScreen, out _holdLabel);

            var review = DevUi.Row(_phraseScreen, 120);
            _keep = DevUi.Button(review, "Keep", Keep, 120, DevUi.Accent);
            _redo = DevUi.Button(review, "Redo", Redo);
            var listen = DevUi.Row(_phraseScreen, 120);
            _replay = DevUi.Button(listen, "Hear flip again", () => _ = PlayPendingAsync(flipped: true));
            _hearRaw = DevUi.Button(listen, "Hear what was said", () => _ = PlayPendingAsync(flipped: false));

            _saveReference = DevUi.Button(_phraseScreen, "Save reference", SaveReference, 120, DevUi.Accent);
            var refs = DevUi.Row(_phraseScreen, 120);
            _playForward = DevUi.Button(refs, "Play forward", () => _ = PlaySavedReferenceAsync(reversed: false));
            _playReversed = DevUi.Button(refs, "Play reversed", () => _ = PlaySavedReferenceAsync(reversed: true));

            DevUi.Spacer(_phraseScreen);
            var nav = DevUi.Row(_phraseScreen, 120);
            _skip = DevUi.Button(nav, "Skip phrase", NextPhrase);
            _finish = DevUi.Button(nav, "Finish", Finish);
        }

        private void EnterPhrase(int index, bool autoplay)
        {
            CancelAttempt();
            playback.StopAll();
            _phraseIndex = index;
            _pending = null;
            _pendingCapture = null;
            SetStep(Step.Idle);

            var phrase = CurrentPhrase;
            _phraseText.text = phrase.TargetText;
            var hasReference = _storage.HasReference(phrase.Id);

            if (_mode == Mode.Session)
            {
                _phraseHeader.text = $"Session {_participant.SpeakerCode} · phrase {index + 1}/{_phrases.Count} · {phrase.Id}\n" +
                                     $"Take {_kept[index] + 1} of {_takesPerPhrase} · kept so far: {_totalKept}";
                if (!hasReference)
                {
                    _phraseStatus.text = "No reference recorded for this phrase. Skip it (record references first).";
                    _hold.Interactable = false;
                    return;
                }

                _phraseStatus.text = "Listen to the reversed phrase, then hold and copy it.";
                if (autoplay) _ = PlayReferenceAsync();
            }
            else
            {
                _phraseHeader.text = $"References · phrase {index + 1}/{_phrases.Count} · {phrase.Id} · " +
                                     (hasReference ? "recorded (recording again replaces it)" : "not recorded yet");
                _phraseStatus.text = "Operator: hold and say the phrase normally, forwards, in a quiet room.";
            }
        }

        private void SetStep(Step step)
        {
            _step = step;
            var session = _mode == Mode.Session;
            var reviewing = step == Step.Reviewing;
            var busy = step == Step.Starting || step == Step.Recording || step == Step.Processing;

            _hold.Interactable = step == Step.Idle || reviewing;
            _holdLabel.text = step == Step.Starting ? "Starting mic…"
                : step == Step.Recording ? (_hold.TapToToggle ? "Tap to stop" : "Release to flip")
                : step == Step.Processing ? "Flipping…"
                : "Hold to record";

            _playReference.gameObject.SetActive(session);
            _playReference.interactable = !busy;
            _keep.gameObject.SetActive(session && reviewing);
            _redo.gameObject.SetActive(reviewing);
            _replay.gameObject.SetActive(session && reviewing);
            _hearRaw.gameObject.SetActive(session && reviewing);
            _saveReference.gameObject.SetActive(!session && reviewing);
            _playForward.gameObject.SetActive(!session);
            _playReversed.gameObject.SetActive(!session);
            _playForward.interactable = _playReversed.interactable = !busy;
            _skip.interactable = _finish.interactable = !busy;
            DevUi.SetText(_skip, session ? "Skip phrase" : "Next phrase");
            DevUi.SetText(_finish, session ? "Finish session" : "Done");

            if (step != Step.Recording)
            {
                DevUi.SetMeter(_meterFill, 0f);
                _elapsedText.text = string.Empty;
            }
        }

        // ---------------------------------------------------------------- capture

        private async void OnHoldBegin()
        {
            if (_step != Step.Idle && _step != Step.Reviewing)
            {
                _hold.ResetState();
                return;
            }

            // Starting a new take abandons any unsaved one (an implicit redo).
            _pending = null;
            _pendingCapture = null;
            playback.StopAll();
            CancelAttempt();
            _attemptCts = new CancellationTokenSource();
            var version = ++_attemptVersion;
            SetStep(Step.Starting);
            _phraseStatus.text = "Starting the microphone… wait for “Speak now”.";

            CaptureStartResult start;
            try
            {
                start = await recorder.StartCaptureAsync(_attemptCts.Token);
            }
            catch (Exception e)
            {
                FailAttempt("Microphone error: " + e.Message);
                return;
            }

            if (version != _attemptVersion) return;

            switch (start.Status)
            {
                case CaptureStatus.Ok:
                    if (_step == Step.Starting)
                    {
                        SetStep(Step.Recording);
                        _phraseStatus.text = _hold.TapToToggle ? "Speak now. Tap to stop." : "Speak now. Release to flip.";
                    }

                    break;
                case CaptureStatus.Cancelled:
                    FailAttempt("Released too early. Hold until it says “Speak now”.");
                    break;
                case CaptureStatus.StartTimeout:
                    FailAttempt("The microphone didn't start in time. Try again.");
                    break;
                default:
                    FailAttempt("Microphone unavailable (" + start.Status + "). Check permission and try again.");
                    break;
            }
        }

        private void OnHoldRelease()
        {
            if (_step == Step.Starting || _step == Step.Recording) recorder.StopCapture();
        }

        private void OnHoldCancel() => recorder.CancelCapture();

        private void OnCaptureCompleted(CaptureResult result)
        {
            _hold.ResetState();
            if (result.Status != CaptureStatus.Ok)
            {
                FailAttempt("Recording cancelled.");
                return;
            }

            _ = ProcessAsync(result, _attemptVersion);
        }

        private async Task ProcessAsync(CaptureResult capture, int version)
        {
            SetStep(Step.Processing);
            _phraseStatus.text = "Flipping…";

            AudioProcessResult processed;
            try
            {
                processed = await Task.Run(() => AudioProcessor.Process(capture.Audio, _config));
            }
            catch (Exception e)
            {
                if (version == _attemptVersion) FailAttempt("Processing failed: " + e.Message);
                return;
            }

            // Late result for an abandoned attempt (phrase changed, cancelled): discard it.
            if (version != _attemptVersion || _step != Step.Processing) return;

            if (processed.Status == AudioProcessStatus.NoVoice)
            {
                FailAttempt("Didn't hear anything. Hold the button and speak up a little.");
                return;
            }

            if (processed.Status == AudioProcessStatus.TooShort)
            {
                FailAttempt("That was very short. Hold the button for the whole phrase.");
                return;
            }

            _pending = processed;
            _pendingCapture = capture.Diagnostics;
            SetStep(Step.Reviewing);

            if (_mode == Mode.Session)
            {
                _phraseStatus.text = "Here's the flip. Keep it (good or bad), or Redo for a technical problem.";
                await PlayAsync(processed.FlippedAttempt);
            }
            else
            {
                _phraseStatus.text = "Check it, then Save reference (or hold to record again).";
                await PlayAsync(processed.RawAttempt);
            }
        }

        private void FailAttempt(string message)
        {
            _hold.ResetState();
            SetStep(Step.Idle);
            _phraseStatus.text = message;
        }

        private void CancelAttempt()
        {
            _attemptVersion++;
            if (_attemptCts != null)
            {
                _attemptCts.Cancel();
                _attemptCts.Dispose();
                _attemptCts = null;
            }

            if (recorder != null) recorder.CancelCapture();
        }

        // ---------------------------------------------------------------- review actions

        private void Keep()
        {
            if (_step != Step.Reviewing || _pending == null || _mode != Mode.Session) return;

            var phrase = CurrentPhrase;
            var takeNumber = _kept[_phraseIndex] + 1;
            var takeId = $"{phrase.Id}_{_participant.SpeakerCode}_t{takeNumber}";
            try
            {
                var sidecar = BakeoffSidecar.Build(takeId, _sessionId, phrase, _participant, _environment,
                    _pendingCapture, _pending, _config, DateTime.UtcNow);
                _storage.WriteTake(_sessionId, takeId, _pending.ScorerInput, _pending.RawAttempt, sidecar);
            }
            catch (Exception e)
            {
                _phraseStatus.text = "Saving failed: " + e.Message;
                return;
            }

            _kept[_phraseIndex] = takeNumber;
            _totalKept++;
            if (takeNumber >= _takesPerPhrase)
            {
                NextPhrase();
                return;
            }

            EnterPhrase(_phraseIndex, autoplay: false);
            _phraseStatus.text = $"Kept {takeId}. Record take {takeNumber + 1}.";
        }

        private void Redo()
        {
            playback.StopAll();
            _pending = null;
            _pendingCapture = null;
            SetStep(Step.Idle);
            _phraseStatus.text = "Discarded. Hold to try again.";
        }

        private void SaveReference()
        {
            if (_step != Step.Reviewing || _pending == null || _mode != Mode.References) return;
            try
            {
                _storage.SaveReference(CurrentPhrase.Id, _pending.RawAttempt);
            }
            catch (Exception e)
            {
                _phraseStatus.text = "Saving failed: " + e.Message;
                return;
            }

            NextPhrase();
        }

        private void NextPhrase()
        {
            if (_phraseIndex + 1 < _phrases.Count) EnterPhrase(_phraseIndex + 1, autoplay: _mode == Mode.Session);
            else Finish();
        }

        private void Finish()
        {
            if (_mode == Mode.References) ShowSetup();
            else ShowSummary();
        }

        // ---------------------------------------------------------------- playback

        private async Task PlayReferenceAsync()
        {
            if (_mode != Mode.Session || !_storage.HasReference(CurrentPhrase.Id)) return;
            AudioBuffer reference;
            try
            {
                reference = _storage.LoadReversedReference(CurrentPhrase.Id);
            }
            catch (Exception e)
            {
                _phraseStatus.text = "Reference could not be read: " + e.Message;
                return;
            }

            await PlayAsync(reference);
        }

        private Task PlayPendingAsync(bool flipped) =>
            _pending == null ? Task.CompletedTask : PlayAsync(flipped ? _pending.FlippedAttempt : _pending.RawAttempt);

        private async Task PlaySavedReferenceAsync(bool reversed)
        {
            if (_pending != null)
            {
                await PlayAsync(reversed ? _pending.FlippedAttempt : _pending.RawAttempt);
                return;
            }

            var id = CurrentPhrase.Id;
            if (!_storage.HasReference(id))
            {
                _phraseStatus.text = "Nothing recorded for this phrase yet.";
                return;
            }

            await PlayAsync(reversed ? _storage.LoadReversedReference(id) : _storage.LoadForwardReference(id));
        }

        private Task PlayAsync(AudioBuffer buffer) => playback.PlayAsync(buffer, destroyCancellationToken);

        // ---------------------------------------------------------------- summary

        private void BuildSummaryScreen()
        {
            _summaryScreen = DevUi.Screen(uiRoot, "SummaryScreen");
            DevUi.Label(_summaryScreen, "Session complete", 56, 80, DevUi.Accent);
            _summaryText = DevUi.Label(_summaryScreen, string.Empty, 32, 760);
            DevUi.Button(_summaryScreen, "Copy adb command", () => GUIUtility.systemCopyBuffer = _pullCommand ?? string.Empty);
            _deleteSessionButton = DevUi.Button(_summaryScreen, "Delete this session", DeleteSession);
            DevUi.Spacer(_summaryScreen);
            DevUi.Button(_summaryScreen, "Next participant", NextParticipant, 140, DevUi.Accent);
        }

        private void ShowSummary()
        {
            CancelAttempt();
            playback.StopAll();
            var commands = _storage.PullCommands(_sessionId, Application.identifier);
            _pullCommand = commands[0];
            _deleteSessionArmed = false;
            DevUi.SetText(_deleteSessionButton, "Delete this session");
            _summaryText.text =
                $"Speaker {_participant.SpeakerCode} · session {_sessionId}\n" +
                $"Kept takes: {_totalKept}\n\n" +
                $"Saved on the phone in:\n{_storage.SessionFolder(_sessionId)}\n\n" +
                "Copy to a folder OUTSIDE the repo (e.g. ~/speakback-bakeoff/data/):\n" +
                string.Join("\n", commands) + "\n\n" +
                "Check the files arrived, then delete the session from the phone.";
            Show(_summaryScreen);
        }

        private void DeleteSession()
        {
            if (!_deleteSessionArmed)
            {
                _deleteSessionArmed = true;
                DevUi.SetText(_deleteSessionButton, "Tap again to delete from this phone");
                return;
            }

            try
            {
                _storage.DeleteSession(_sessionId);
                DevUi.SetText(_deleteSessionButton, "Session deleted");
            }
            catch (Exception e)
            {
                DevUi.SetText(_deleteSessionButton, "Delete failed: " + e.Message);
            }
        }

        private void NextParticipant()
        {
            _participant = null;
            _sessionId = null;
            _speakerInput.text = string.Empty;
            _consentInput.text = string.Empty;
            _resetAccent();
            _resetVoice();
            _consentConfirmed = false;
            RefreshConsentButton();
            _setupStatus.text = string.Empty;
            ShowSetup();
        }

        private void Show(RectTransform screen)
        {
            _setupScreen.gameObject.SetActive(screen == _setupScreen);
            _phraseScreen.gameObject.SetActive(screen == _phraseScreen);
            _summaryScreen.gameObject.SetActive(screen == _summaryScreen);
        }
    }
}
