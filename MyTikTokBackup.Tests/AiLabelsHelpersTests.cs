using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyTikTokBackup.Core.AiLabels;
using NUnit.Framework;

namespace MyTikTokBackup.Tests
{
    public class AiLabelsHelpersTests
    {
        [Test]
        public void VideoTextBuilder_RemovesNoise()
        {
            var text = VideoTextBuilder.Build(
                "Przepis na szybki obiad 🍝 #gotowanie #fyp #dlaciebie @kucharz https://example.com/x #gotowanie",
                new[] { "gotowanie", "Fyp", "makaron", "" },
                "dźwięk oryginalny - kucharz");

            Assert.That(text, Is.EqualTo("Przepis na szybki obiad 🍝 gotowanie makaron"));
        }

        [Test]
        public void VideoTextBuilder_KeepsMusicTitle()
        {
            var text = VideoTextBuilder.Build(null, null, "Bella ciao");

            Assert.That(text, Is.EqualTo("Bella ciao"));
        }

        [Test]
        public void VideoTextBuilder_EmptyInput()
        {
            Assert.That(VideoTextBuilder.Build("#fyp #viral", new[] { "foryou" }, "original sound - x"), Is.EqualTo(""));
        }

        [Test]
        public void SafeTensorsReader_ReadsFloatTensor()
        {
            var header = Encoding.UTF8.GetBytes("""{"__metadata__":{"format":"pt"},"linear.weight":{"dtype":"F32","shape":[2,3],"data_offsets":[0,24]}}""");
            var data = new float[] { 1, 2, 3, 4, 5, 6 };
            var bytes = BitConverter.GetBytes((ulong)header.Length)
                .Concat(header)
                .Concat(data.SelectMany(BitConverter.GetBytes))
                .ToArray();

            var (tensor, shape) = SafeTensorsReader.ReadFloatTensor(bytes);

            Assert.That(shape, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(tensor, Is.EqualTo(data));
        }

        [Test]
        public void ClipPreprocessor_NormalizesToChw()
        {
            var rgba = new byte[ClipPreprocessor.PixelCount * 4];
            for (int i = 0; i < ClipPreprocessor.PixelCount; i++)
            {
                rgba[i * 4] = 255;
                rgba[i * 4 + 1] = 0;
                rgba[i * 4 + 2] = 128;
                rgba[i * 4 + 3] = 255;
            }
            var tensor = new float[ClipPreprocessor.TensorLength];

            ClipPreprocessor.ToChw(new RgbImage(rgba, ClipPreprocessor.Size, ClipPreprocessor.Size), tensor);

            Assert.That(tensor[0], Is.EqualTo((1f - 0.48145466f) / 0.26862954f).Within(1e-4));
            Assert.That(tensor[ClipPreprocessor.PixelCount + 5], Is.EqualTo((0f - 0.4578275f) / 0.26130258f).Within(1e-4));
            Assert.That(tensor[2 * ClipPreprocessor.PixelCount + 7], Is.EqualTo((128f / 255f - 0.40821073f) / 0.27577711f).Within(1e-4));
        }

        [Test]
        public void ClipPreprocessor_RejectsWrongSize()
        {
            Assert.Throws<ArgumentException>(() => ClipPreprocessor.ToChw(new RgbImage(new byte[16], 2, 2), new float[ClipPreprocessor.TensorLength]));
        }

        [Test]
        public void VectorMath_BlobRoundTrip()
        {
            var vector = new float[] { 0.5f, -1.25f, 3f };

            Assert.That(VectorMath.FromBlob(VectorMath.ToBlob(vector)), Is.EqualTo(vector));
            Assert.That(VectorMath.FromBlob(null), Is.Null);
            Assert.That(VectorMath.ToBlob(null), Is.Null);
        }

        [Test]
        public void VectorMath_MeanIsNormalized()
        {
            var mean = VectorMath.Mean(new List<float[]> { new float[] { 1, 0 }, new float[] { 0, 1 } });

            Assert.That(mean[0], Is.EqualTo(MathF.Sqrt(0.5f)).Within(1e-6));
            Assert.That(mean[1], Is.EqualTo(MathF.Sqrt(0.5f)).Within(1e-6));
        }
    }
}
