using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace MyTikTokBackup.Core.AiLabels
{
    /// <summary>
    /// Minimal reader of the safetensors format: 8-byte little-endian header length, JSON header, raw tensor data.
    /// </summary>
    public static class SafeTensorsReader
    {
        /// <summary>Reads a float32 tensor. When <paramref name="name"/> is null the first tensor is returned.</summary>
        public static (float[] Data, int[] Shape) ReadFloatTensor(string path, string name = null)
        {
            return ReadFloatTensor(File.ReadAllBytes(path), name);
        }

        public static (float[] Data, int[] Shape) ReadFloatTensor(byte[] bytes, string name = null)
        {
            var headerLength = checked((int)BitConverter.ToUInt64(bytes, 0));
            using var header = JsonDocument.Parse(bytes.AsMemory(8, headerLength));

            var tensor = header.RootElement.EnumerateObject()
                .Where(x => x.Name != "__metadata__")
                .First(x => name == null || x.Name == name)
                .Value;

            var dtype = tensor.GetProperty("dtype").GetString();
            if (dtype != "F32")
            {
                throw new NotSupportedException($"Unsupported safetensors dtype {dtype}");
            }

            var shape = tensor.GetProperty("shape").EnumerateArray().Select(x => x.GetInt32()).ToArray();
            var offsets = tensor.GetProperty("data_offsets").EnumerateArray().Select(x => x.GetInt64()).ToArray();
            var start = checked((int)(8 + headerLength + offsets[0]));
            var length = checked((int)(offsets[1] - offsets[0]));

            var data = MemoryMarshal.Cast<byte, float>(bytes.AsSpan(start, length)).ToArray();
            if (data.Length != shape.Aggregate(1, (a, b) => a * b))
            {
                throw new InvalidDataException("Safetensors shape doesn't match data length");
            }
            return (data, shape);
        }
    }
}
