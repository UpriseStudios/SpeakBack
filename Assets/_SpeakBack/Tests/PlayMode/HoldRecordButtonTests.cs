using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using Uprise.SpeakBack.UI;

namespace Uprise.SpeakBack.Tests
{
    public sealed class HoldRecordButtonTests
    {
        private GameObject _go;
        private HoldRecordButton _button;
        private int _begins;
        private int _releases;
        private int _cancels;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Hold", typeof(RectTransform));
            _button = _go.AddComponent<HoldRecordButton>();
            _begins = _releases = _cancels = 0;
            _button.BeginRequested += () => _begins++;
            _button.ReleaseRequested += () => _releases++;
            _button.CancelRequested += () => _cancels++;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        private static PointerEventData Pointer(int id) => new PointerEventData(null) { pointerId = id };

        [Test]
        public void Hold_PressBegins_ReleaseEnds()
        {
            _button.OnPointerDown(Pointer(1));
            _button.OnPointerUp(Pointer(1));

            Assert.That(_begins, Is.EqualTo(1));
            Assert.That(_releases, Is.EqualTo(1));
        }

        [Test]
        public void Hold_OtherPointerCannotEndOrRestart()
        {
            _button.OnPointerDown(Pointer(1));
            _button.OnPointerDown(Pointer(2));
            _button.OnPointerUp(Pointer(2));

            Assert.That(_begins, Is.EqualTo(1));
            Assert.That(_releases, Is.EqualTo(0));

            _button.OnPointerUp(Pointer(1));
            Assert.That(_releases, Is.EqualTo(1));
        }

        [Test]
        public void Hold_DoubleReleaseRaisesOnce()
        {
            _button.OnPointerDown(Pointer(1));
            _button.OnPointerUp(Pointer(1));
            _button.OnPointerUp(Pointer(1));

            Assert.That(_releases, Is.EqualTo(1));
        }

        [Test]
        public void ResetState_AfterAutoStop_MakesLateReleaseANoOp()
        {
            _button.OnPointerDown(Pointer(1));
            _button.ResetState();
            _button.OnPointerUp(Pointer(1));

            Assert.That(_releases, Is.EqualTo(0));
        }

        [Test]
        public void NotInteractable_IgnoresPress()
        {
            _button.Interactable = false;
            _button.OnPointerDown(Pointer(1));

            Assert.That(_begins, Is.EqualTo(0));
        }

        [Test]
        public void TapToToggle_FirstTapBegins_SecondTapReleases_LiftsIgnored()
        {
            _button.TapToToggle = true;

            _button.OnPointerDown(Pointer(1));
            _button.OnPointerUp(Pointer(1));
            Assert.That(_begins, Is.EqualTo(1));
            Assert.That(_releases, Is.EqualTo(0));

            _button.OnPointerDown(Pointer(1));
            Assert.That(_releases, Is.EqualTo(1));
        }

        [Test]
        public void FocusLoss_WhileActive_Cancels()
        {
            _button.OnPointerDown(Pointer(1));
            _go.SendMessage("OnApplicationFocus", false);

            Assert.That(_cancels, Is.EqualTo(1));
            Assert.That(_button.IsActive, Is.False);
        }

        [Test]
        public void FocusLoss_WhileIdle_DoesNothing()
        {
            _go.SendMessage("OnApplicationFocus", false);

            Assert.That(_cancels, Is.EqualTo(0));
        }
    }
}
