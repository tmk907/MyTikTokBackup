using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MyTikTokBackup.Core.AiLabels
{
    public record AiModelFile(string FileName, string Url, string Sha256, long Size);

    /// <summary>
    /// Models used by AI labeling. Both share the CLIP ViT-B/32 512-d embedding space:
    /// the multilingual text model was distilled into the space of the CLIP image encoder.
    /// </summary>
    public static class AiModelCatalog
    {
        private const string TextRepo = "https://huggingface.co/sentence-transformers/clip-ViT-B-32-multilingual-v1/resolve/58edf8cada9e398793dca955574a48cbb7f18be2/";
        private const string VisionRepo = "https://huggingface.co/Xenova/clip-vit-base-patch32/resolve/6b7c523703ae26e39be4b03774966f8571c7f523/";

        /// <summary>Stored with every embedding; changing it forces embeddings to be recomputed.</summary>
        public const string ModelVersion = "mclip-b32-q8+clipvis-b32-q8+v1";

        public const int EmbeddingSize = 512;

        public const string TextModel = "text.onnx";
        public const string Vocab = "vocab.txt";
        public const string Dense = "dense.safetensors";
        public const string VisionModel = "vision.onnx";

        public static IReadOnlyList<AiModelFile> Files { get; } = new[]
        {
            RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? new AiModelFile(TextModel, TextRepo + "onnx/model_qint8_arm64.onnx", "3bed77e83926519660b5c0833cb3321cfde952331edec4f7d501c7d384b8a8a7", 135336307)
                : new AiModelFile(TextModel, TextRepo + "onnx/model_quint8_avx2.onnx", "fbc8fbeaa5237d96bd1bf430057c70d34de8334a463e933a5faa305f6caeed9c", 135377779),
            new AiModelFile(Vocab, TextRepo + "vocab.txt", "fe0fda7c425b48c516fc8f160d594c8022a0808447475c1a7c6d6479763f310c", 995526),
            new AiModelFile(Dense, TextRepo + "2_Dense/model.safetensors", "d12568dc7300970a4d3dbb49068ad16cd89b99840b74b026f8e48071e9414f74", 1572984),
            new AiModelFile(VisionModel, VisionRepo + "onnx/vision_model_quantized.onnx", "583fd1110a514667812fee7d684952aaf82a99b959760c8d7dca7e0ab9839299", 89117001),
        };
    }
}
