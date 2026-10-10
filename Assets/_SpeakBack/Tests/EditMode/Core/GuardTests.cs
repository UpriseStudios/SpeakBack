using System;
using NUnit.Framework;
using Uprise.SpeakBack.Core;

namespace Uprise.SpeakBack.Core.Tests
{
    // Shared by Unity EditMode and Tools/DotnetTests: keep to NUnit APIs present in Unity's NUnit 3.5.
    public sealed class GuardTests
    {
        [TestCase(1)]
        [TestCase(16000)]
        [TestCase(int.MaxValue)]
        public void Positive_ReturnsValue_WhenGreaterThanZero(int value)
        {
            Assert.That(Guard.Positive(value, "value"), Is.EqualTo(value));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void Positive_Throws_WhenZeroOrNegative(int value)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Positive(value, "sampleRate"));
            Assert.That(ex.ParamName, Is.EqualTo("sampleRate"));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(0.5f)]
        public void Finite_ReturnsValue_WhenFinite(float value)
        {
            Assert.That(Guard.Finite(value, "sample"), Is.EqualTo(value));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Finite_Throws_WhenNotFinite(float value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Finite(value, "sample"));
        }
    }
}
