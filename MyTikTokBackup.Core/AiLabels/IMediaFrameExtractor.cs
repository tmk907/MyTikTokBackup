using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyTikTokBackup.Core.AiLabels
{
    /// <summary>RGBA8 pixels, row-major.</summary>
    public record RgbImage(byte[] Rgba, int Width, int Height);

    /// <summary>
    /// Extracts images for the CLIP image encoder. Returned images are already resized
    /// (shortest edge) and center-cropped to <see cref="ClipPreprocessor.Size"/> x <see cref="ClipPreprocessor.Size"/>.
    /// </summary>
    public interface IMediaFrameExtractor
    {
        Task<IReadOnlyList<RgbImage>> ExtractVideoFramesAsync(string path, int count, CancellationToken cancellationToken);
        Task<RgbImage> LoadImageAsync(string path, CancellationToken cancellationToken);
    }
}
