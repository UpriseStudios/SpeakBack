using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Uprise.SpeakBack.UI
{
    /// <summary>
    /// Record button (Phase 1 §6, D-020). Raises intents only and never touches the microphone.
    /// Hold mode (default): press → <see cref="BeginRequested"/>, release → <see cref="ReleaseRequested"/>.
    /// Tap-to-toggle mode: first tap begins, next tap releases.
    /// Focus loss or disable while active → <see cref="CancelRequested"/>.
    /// Moving the finger off the button does not end a hold, because Unity still delivers pointer-up to the
    /// pressed object. Only the pointer that started a hold can end it.
    /// </summary>
    public sealed class HoldRecordButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private const int NoPointer = int.MinValue;

        [SerializeField] private bool tapToToggle;

        private bool _active;
        private int _pointerId = NoPointer;

        /// <summary>Start recording.</summary>
        public event Action BeginRequested;

        /// <summary>Stop recording and process.</summary>
        public event Action ReleaseRequested;

        /// <summary>Abandon the recording (focus lost, disabled).</summary>
        public event Action CancelRequested;

        /// <summary>Tap-to-toggle accessibility mode (D-020).</summary>
        public bool TapToToggle
        {
            get => tapToToggle;
            set
            {
                if (tapToToggle == value) return;
                ResetState();
                tapToToggle = value;
            }
        }

        /// <summary>When false, new presses are ignored (e.g. during playback or processing).</summary>
        public bool Interactable { get; set; } = true;

        /// <summary>Whether the button currently considers a recording in progress.</summary>
        public bool IsActive => _active;

        /// <summary>
        /// Forgets the current press without raising anything. Call when the capture ended elsewhere (e.g.
        /// auto-stop at the maximum length), so the eventual finger lift has no effect.
        /// </summary>
        public void ResetState()
        {
            _active = false;
            _pointerId = NoPointer;
        }

        /// <inheritdoc/>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (tapToToggle)
            {
                if (_active)
                {
                    ResetState();
                    ReleaseRequested?.Invoke();
                }
                else if (Interactable)
                {
                    _active = true;
                    BeginRequested?.Invoke();
                }

                return;
            }

            if (_active || !Interactable) return;
            _active = true;
            _pointerId = eventData.pointerId;
            BeginRequested?.Invoke();
        }

        /// <inheritdoc/>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (tapToToggle || !_active || eventData.pointerId != _pointerId) return;
            ResetState();
            ReleaseRequested?.Invoke();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) CancelIfActive();
        }

        private void OnDisable() => CancelIfActive();

        private void CancelIfActive()
        {
            if (!_active) return;
            ResetState();
            CancelRequested?.Invoke();
        }
    }
}
