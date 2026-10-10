using System;
using NUnit.Framework;
using Uprise.SpeakBack.Core.Audio;

namespace Uprise.SpeakBack.Core.Tests.Audio
{
    public sealed class AudioReverserTests
    {
        [Test]
        public void Mono_ReversesSamples()
        {
            var input = AudioBuffer.Create(new[] { 1f, 2f, 3f, 4f }, 16000, 1);

            var output = AudioReverser.ReverseFrames(input);

            Assert.That(output.ToArray(), Is.EqualTo(new[] { 4f, 3f, 2f, 1f }));
            Assert.That(output.SampleRate, Is.EqualTo(16000));
            Assert.That(output.Channels, Is.EqualTo(1));
        }

        [Test]
        public void Stereo_ReversesFrames_KeepingChannelOrder()
        {
            // [L0,R0,L1,R1] → [L1,R1,L0,R0] (Phase 1 §9): no channel swap.
            var input = AudioBuffer.Create(new[] { 0.1f, -0.1f, 0.2f, -0.2f }, 48000, 2);

            var output = AudioReverser.ReverseFrames(input);

            Assert.That(output.ToArray(), Is.EqualTo(new[] { 0.2f, -0.2f, 0.1f, -0.1f }));
        }

        [Test]
        public void Stereo_IsNotPlainArrayReverse()
        {
            var samples = new[] { 0.1f, -0.1f, 0.2f, -0.2f, 0.3f, -0.3f };
            var input = AudioBuffer.Create(samples, 48000, 2);
            var plainReverse = (float[])samples.Clone();
            Array.Reverse(plainReverse);

            var output = AudioReverser.ReverseFrames(input);

            Assert.That(output.ToArray(), Is.Not.EqualTo(plainReverse));
            Assert.That(output.ToArray(), Is.EqualTo(new[] { 0.3f, -0.3f, 0.2f, -0.2f, 0.1f, -0.1f }));
        }

        [Test]
        public void ThreeChannels_KeepsChannelOrder()
        {
            var input = AudioBuffer.Create(new[] { 1f, 2f, 3f, 4f, 5f, 6f }, 8000, 3);

            var output = AudioReverser.ReverseFrames(input);

            Assert.That(output.ToArray(), Is.EqualTo(new[] { 4f, 5f, 6f, 1f, 2f, 3f }));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void DoubleReversal_RestoresEveryFrame(int channels)
        {
            var rng = new TestSignals.Rng(42);
            var samples = new float[1001 * channels];
            for (var i = 0; i < samples.Length; i++) samples[i] = rng.NextSigned();
            var input = AudioBuffer.Create(samples, 44100, channels);

            var output = AudioReverser.ReverseFrames(AudioReverser.ReverseFrames(input));

            Assert.That(output.ToArray(), Is.EqualTo(samples));
        }

        [Test]
        public void Empty_StaysEmpty()
        {
            var output = AudioReverser.ReverseFrames(AudioBuffer.Silence(0, 16000, 2));

            Assert.That(output.FrameCount, Is.EqualTo(0));
            Assert.That(output.Channels, Is.EqualTo(2));
        }

        [Test]
        public void DoesNotModifyInput()
        {
            var input = AudioBuffer.Create(new[] { 1f, 2f, 3f }, 16000, 1);

            AudioReverser.ReverseFrames(input);

            Assert.That(input.ToArray(), Is.EqualTo(new[] { 1f, 2f, 3f }));
        }
    }
}
