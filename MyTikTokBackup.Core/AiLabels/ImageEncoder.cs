using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;

namespace MyTikTokBackup.Core.AiLabels
{
    /// <summary>CLIP ViT-B/32 vision model (Xenova/clip-vit-base-patch32), returns L2-normalized image embeddings.</summary>
    public sealed class ImageEncoder : IDisposable
    {
        private const int BatchSize = 8;

        private readonly InferenceSession _session;
        private readonly string _inputName;
        private readonly string _outputName;

        public ImageEncoder(string modelPath, SessionOptions options)
        {
            _session = new InferenceSession(modelPath, options);
            _inputName = _session.InputMetadata.ContainsKey("pixel_values") ? "pixel_values" : _session.InputMetadata.Keys.First();
            _outputName = _session.OutputMetadata.ContainsKey("image_embeds") ? "image_embeds" : _session.OutputMetadata.Keys.First();
        }

        public float[][] Encode(IReadOnlyList<RgbImage> images)
        {
            var result = new float[images.Count][];
            for (int start = 0; start < images.Count; start += BatchSize)
            {
                var count = Math.Min(BatchSize, images.Count - start);
                var pixels = new float[count * ClipPreprocessor.TensorLength];
                for (int i = 0; i < count; i++)
                {
                    ClipPreprocessor.ToChw(images[start + i], pixels.AsSpan(i * ClipPreprocessor.TensorLength, ClipPreprocessor.TensorLength));
                }

                using var input = OrtValue.CreateTensorValueFromMemory(pixels, new long[] { count, 3, ClipPreprocessor.Size, ClipPreprocessor.Size });
                using var runOptions = new RunOptions();
                using var outputs = _session.Run(runOptions, new Dictionary<string, OrtValue> { [_inputName] = input }, new[] { _outputName });
                var embeddings = outputs[0].GetTensorDataAsSpan<float>();
                var size = embeddings.Length / count;

                for (int i = 0; i < count; i++)
                {
                    var embedding = embeddings.Slice(i * size, size).ToArray();
                    VectorMath.Normalize(embedding);
                    result[start + i] = embedding;
                }
            }
            return result;
        }

        public void Dispose()
        {
            _session.Dispose();
        }
    }
}
