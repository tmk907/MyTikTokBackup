using System;
using System.Collections.Generic;
using System.Numerics.Tensors;
using System.Runtime.InteropServices;

namespace MyTikTokBackup.Core.AiLabels
{
    public static class VectorMath
    {
        public static byte[] ToBlob(float[] vector)
        {
            return vector == null ? null : MemoryMarshal.AsBytes(vector.AsSpan()).ToArray();
        }

        public static float[] FromBlob(byte[] blob)
        {
            return blob == null || blob.Length == 0 ? null : MemoryMarshal.Cast<byte, float>(blob).ToArray();
        }

        public static void Normalize(Span<float> vector)
        {
            var norm = TensorPrimitives.Norm(vector);
            if (norm > 0)
            {
                TensorPrimitives.Divide(vector, norm, vector);
            }
        }

        /// <summary>Normalized mean of normalized vectors.</summary>
        public static float[] Mean(IReadOnlyList<float[]> vectors)
        {
            if (vectors.Count == 0) return null;
            var result = new float[vectors[0].Length];
            foreach (var vector in vectors)
            {
                TensorPrimitives.Add(result, vector, result);
            }
            Normalize(result);
            return result;
        }

        public static float Dot(float[] a, float[] b) => TensorPrimitives.Dot(a, b);
    }
}
