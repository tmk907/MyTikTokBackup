using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics.Tensors;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.Tokenizers;

namespace MyTikTokBackup.Core.AiLabels
{
    /// <summary>
    /// sentence-transformers/clip-ViT-B-32-multilingual-v1: DistilBERT (ONNX) -> mean pooling -> Dense 768->512 -> L2 normalize.
    /// The ONNX graph contains only the transformer, pooling and the dense layer are done here.
    /// </summary>
    public sealed class TextEncoder : IDisposable
    {
        private const int MaxTokens = 128;
        private const int BatchSize = 16;

        private readonly InferenceSession _session;
        private readonly BertTokenizer _tokenizer;
        private readonly float[] _denseWeights; // [out, in] row-major
        private readonly int _inFeatures;
        private readonly int _outFeatures;
        private readonly bool _hasTokenTypeIds;
        private readonly string _outputName;

        public TextEncoder(string modelPath, string vocabPath, string densePath, SessionOptions options)
        {
            _tokenizer = BertTokenizer.Create(vocabPath, new BertOptions
            {
                LowerCaseBeforeTokenization = false,
                RemoveNonSpacingMarks = false,
                IndividuallyTokenizeCjk = true,
            });

            var (weights, shape) = SafeTensorsReader.ReadFloatTensor(densePath);
            _denseWeights = weights;
            _outFeatures = shape[0];
            _inFeatures = shape[1];

            _session = new InferenceSession(modelPath, options);
            _hasTokenTypeIds = _session.InputMetadata.ContainsKey("token_type_ids");
            _outputName = _session.OutputMetadata.ContainsKey("last_hidden_state")
                ? "last_hidden_state"
                : _session.OutputMetadata.Keys.First();
        }

        public IReadOnlyList<int> Tokenize(string text)
        {
            var ids = _tokenizer.EncodeToIds(text ?? "", addSpecialTokens: false);
            var result = new List<int>(Math.Min(ids.Count, MaxTokens - 2) + 2) { _tokenizer.ClassificationTokenId };
            result.AddRange(ids.Take(MaxTokens - 2));
            result.Add(_tokenizer.SeparatorTokenId);
            return result;
        }

        public float[][] Encode(IReadOnlyList<string> texts)
        {
            var result = new float[texts.Count][];
            for (int start = 0; start < texts.Count; start += BatchSize)
            {
                var count = Math.Min(BatchSize, texts.Count - start);
                EncodeBatch(texts, start, count, result);
            }
            return result;
        }

        private void EncodeBatch(IReadOnlyList<string> texts, int start, int count, float[][] result)
        {
            var tokenized = Enumerable.Range(start, count).Select(i => Tokenize(texts[i])).ToList();
            var length = tokenized.Max(x => x.Count);

            var inputIds = new long[count * length];
            var attentionMask = new long[count * length];
            Array.Fill(inputIds, _tokenizer.PaddingTokenId);
            for (int b = 0; b < count; b++)
            {
                for (int t = 0; t < tokenized[b].Count; t++)
                {
                    inputIds[b * length + t] = tokenized[b][t];
                    attentionMask[b * length + t] = 1;
                }
            }

            var shape = new long[] { count, length };
            using var inputIdsValue = OrtValue.CreateTensorValueFromMemory(inputIds, shape);
            using var attentionMaskValue = OrtValue.CreateTensorValueFromMemory(attentionMask, shape);
            using var tokenTypeIdsValue = _hasTokenTypeIds ? OrtValue.CreateTensorValueFromMemory(new long[count * length], shape) : null;

            var inputs = new Dictionary<string, OrtValue>
            {
                ["input_ids"] = inputIdsValue,
                ["attention_mask"] = attentionMaskValue,
            };
            if (_hasTokenTypeIds)
            {
                inputs["token_type_ids"] = tokenTypeIdsValue;
            }

            using var runOptions = new RunOptions();
            using var outputs = _session.Run(runOptions, inputs, new[] { _outputName });
            var hidden = outputs[0].GetTensorDataAsSpan<float>(); // [count, length, inFeatures]

            var pooled = new float[_inFeatures];
            for (int b = 0; b < count; b++)
            {
                Array.Clear(pooled);
                var tokens = tokenized[b].Count;
                for (int t = 0; t < tokens; t++)
                {
                    TensorPrimitives.Add(pooled, hidden.Slice((b * length + t) * _inFeatures, _inFeatures), pooled);
                }
                TensorPrimitives.Divide(pooled, tokens, pooled);

                var embedding = new float[_outFeatures];
                for (int o = 0; o < _outFeatures; o++)
                {
                    embedding[o] = TensorPrimitives.Dot(_denseWeights.AsSpan(o * _inFeatures, _inFeatures), pooled);
                }
                VectorMath.Normalize(embedding);
                result[start + b] = embedding;
            }
        }

        public void Dispose()
        {
            _session.Dispose();
        }
    }
}
