using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Uprise.SpeakBack.UI;

namespace Uprise.SpeakBack.Tests
{
    public sealed class SafeAreaFitterTests
    {
        [Test]
        public void ComputeAnchors_InsetsForNotchAndGestureBar()
        {
            var safe = new Rect(0f, 48f, 1080f, 2340f - 48f - 120f);

            SafeAreaFitter.ComputeAnchors(safe, new Vector2Int(1080, 2340), out var min, out var max);

            Assert.That(min.x, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(min.y, Is.EqualTo(48f / 2340f).Within(1e-6f));
            Assert.That(max.x, Is.EqualTo(1f).Within(1e-6f));
            Assert.That(max.y, Is.EqualTo((2340f - 120f) / 2340f).Within(1e-6f));
        }

        [Test]
        public void ComputeAnchors_ZeroScreen_FallsBackToFullScreen()
        {
            SafeAreaFitter.ComputeAnchors(new Rect(0, 0, 0, 0), Vector2Int.zero, out var min, out var max);

            Assert.That(min, Is.EqualTo(Vector2.zero));
            Assert.That(max, Is.EqualTo(Vector2.one));
        }

        [UnityTest]
        public IEnumerator Fitter_AppliesNormalisedAnchors_InPlayMode()
        {
            var canvas = new GameObject("Canvas", typeof(Canvas));
            var root = new GameObject("SafeAreaRoot", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            root.AddComponent<SafeAreaFitter>();

            yield return null;

            var rect = (RectTransform)root.transform;
            Assert.That(rect.anchorMin.x, Is.InRange(0f, 1f));
            Assert.That(rect.anchorMax.y, Is.InRange(0f, 1f));
            Assert.That(rect.anchorMin.y, Is.LessThanOrEqualTo(rect.anchorMax.y));

            Object.Destroy(canvas);
        }
    }
}
