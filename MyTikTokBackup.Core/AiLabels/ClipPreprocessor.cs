using System;

namespace MyTikTokBackup.Core.AiLabels
{
    /// <summary>
    /// CLIP pixel normalization (see Xenova/clip-vit-base-patch32 preprocessor_config.json).
    /// Resizing and cropping is done by <see cref="IMediaFrameExtractor"/>.
    /// </summary>
    public static class ClipPreprocessor
    {
        public const int Size = 224;
        public const int PixelCount = Size * Size;
        public const int TensorLength = 3 * PixelCount;

        private static readonly float[] Mean = { 0.48145466f, 0.4578275f, 0.40821073f };
        private static readonly float[] Std = { 0.26862954f, 0.26130258f, 0.27577711f };

        /// <summary>Writes the image as a normalized CHW tensor into <paramref name="destination"/>.</summary>
        public static void ToChw(RgbImage image, Span<float> destination)
        {
            if (image.Width != Size || image.Height != Size)
            {
                throw new ArgumentException($"Image must be {Size}x{Size}, is {image.Width}x{image.Height}");
            }

            var pixels = image.Rgba;
            for (int c = 0; c < 3; c++)
            {
                var plane = destination.Slice(c * PixelCount, PixelCount);
                var scale = 1f / (255f * Std[c]);
                var offset = Mean[c] / Std[c];
                for (int i = 0; i < PixelCount; i++)
                {
                    plane[i] = pixels[i * 4 + c] * scale - offset;
                }
            }
        }
    }
}
