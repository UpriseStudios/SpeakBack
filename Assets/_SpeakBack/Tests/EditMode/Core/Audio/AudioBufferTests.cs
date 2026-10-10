using System;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Core.Tests.Audio
{
    public sealed class AudioBufferTests
    {
        [Test]
        public void Create_ExposesShape()
        {
            var buffer = AudioBuffer.Create(new[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f }, 48000, 2);

            Assert.That(buffer.SampleRate, Is.EqualTo(48000));
            Assert.That(buffer.Channels, Is.EqualTo(2));
            Assert.That(buffer.SampleCount, Is.EqualTo(6));
            Assert.That(buffer.FrameCount, Is.EqualTo(3));
            Assert.That(buffer.DurationSeconds, Is.EqualTo(3.0 / 48000).Within(1e-12));
        }

        [Test]
        public void Create_CopiesInput_SoLaterChangesDoNotLeakIn()
        {
            var source = new[] { 0.1f, 0.2f };
            var buffer = AudioBuffer.Create(source, 16000, 1);

            source[0] = 0.9f;

            Assert.That(buffer.Samples[0], Is.EqualTo(0.1f));
        }

        [Test]
        public void ToArray_ReturnsIndependentCopy()
        {
            var buffer = AudioBuffer.Create(new[] { 0.1f, 0.2f }, 16000, 1);

            var copy = buffer.ToArray();
            copy[0] = 0.9f;

            Assert.That(buffer.Samples[0], Is.EqualTo(0.1f));
        }

        [Test]
        public void Create_AllowsEmptyBuffer()
        {
            var buffer = AudioBuffer.Create(Array.Empty<float>(), 16000, 1);

            Assert.That(buffer.FrameCount, Is.EqualTo(0));
            Assert.That(buffer.DurationSeconds, Is.EqualTo(0.0));
        }

        [TestCase(0)]
        [TestCase(-16000)]
        public void Create_RejectsNonPositiveRate(int rate)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AudioBuffer.Create(new[] { 0f }, rate, 1));
        }

        [TestCase(0)]
        [TestCase(-2)]
        public void Create_RejectsNonPositiveChannels(int channels)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AudioBuffer.Create(new[] { 0f }, 16000, channels));
        }

        [Test]
        public void Create_RejectsPartialFrame()
        {
            Assert.Throws<ArgumentException>(() => AudioBuffer.Create(new[] { 0f, 0f, 0f }, 16000, 2));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Create_RejectsNonFiniteSamples(float bad)
        {
            Assert.Throws<ArgumentException>(() => AudioBuffer.Create(new[] { 0f, bad, 0f }, 16000, 1));
        }

        [Test]
        public void Create_RejectsNullArray()
        {
            Assert.Throws<ArgumentNullException>(() => AudioBuffer.Create((float[])null, 16000, 1));
        }

        [Test]
        public void SliceFrames_CopiesWholeFrames()
        {
            var buffer = AudioBuffer.Create(new[] { 1f, -1f, 2f, -2f, 3f, -3f }, 8000, 2);

            var slice = buffer.SliceFrames(1, 2);

            Assert.That(slice.ToArray(), Is.EqualTo(new[] { 2f, -2f, 3f, -3f }));
            Assert.That(slice.Channels, Is.EqualTo(2));
            Assert.That(slice.SampleRate, Is.EqualTo(8000));
        }

        [TestCase(-1, 1)]
        [TestCase(0, 4)]
        [TestCase(3, 1)]
        [TestCase(1, -1)]
        public void SliceFrames_RejectsOutOfRange(int start, int count)
        {
            var buffer = AudioBuffer.Create(new[] { 1f, 2f, 3f }, 8000, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SliceFrames(start, count));
        }

        [Test]
        public void Silence_HasRequestedShape()
        {
            var buffer = AudioBuffer.Silence(10, 16000, 2);

            Assert.That(buffer.FrameCount, Is.EqualTo(10));
            Assert.That(buffer.ToArray(), Is.All.EqualTo(0f));
        }
    }
}
