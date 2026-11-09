using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyTikTokBackup.Core.AiLabels;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace MyTikTokBackup.Desktop.Services
{
    /// <summary>
    /// Extracts video frames with MediaComposition and decodes / resizes / center-crops them with BitmapDecoder.
    /// </summary>
    public class WinRtMediaFrameExtractor : IMediaFrameExtractor
    {
        private static readonly double[] FramePositions = { 0.15, 0.40, 0.65, 0.90 };

        public async Task<IReadOnlyList<RgbImage>> ExtractVideoFramesAsync(string path, int count, CancellationToken cancellationToken)
        {
            var file = await StorageFile.GetFileFromPathAsync(path).AsTask(cancellationToken);
            var frames = new List<RgbImage>();
            try
            {
                var clip = await MediaClip.CreateFromFileAsync(file).AsTask(cancellationToken);
                var composition = new MediaComposition();
                composition.Clips.Add(clip);

                // Decode at ~2x the model input size, keeping the aspect ratio.
                var properties = clip.GetVideoEncodingProperties();
                var scale = properties.Width > 0 && properties.Height > 0
                    ? Math.Min(1.0, 2.0 * ClipPreprocessor.Size / Math.Min(properties.Width, properties.Height))
                    : 1.0;
                var width = (int)Math.Round(properties.Width * scale);
                var height = (int)Math.Round(properties.Height * scale);

                var duration = clip.OriginalDuration;
                var times = FramePositions.Take(count).Select(x => TimeSpan.FromTicks((long)(duration.Ticks * x))).ToList();
                var thumbnails = await composition.GetThumbnailsAsync(times, width, height, VideoFramePrecision.NearestKeyFrame).AsTask(cancellationToken);
                foreach (var thumbnail in thumbnails)
                {
                    using (thumbnail)
                    {
                        frames.Add(await DecodeAsync(thumbnail, cancellationToken));
                    }
                }
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // Missing codec (e.g. HEVC) or corrupted file - fall back to the system thumbnail.
                frames.Clear();
            }

            if (frames.Count == 0)
            {
                using var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.VideosView, 448).AsTask(cancellationToken);
                if (thumbnail != null && thumbnail.Type == ThumbnailType.Image)
                {
                    frames.Add(await DecodeAsync(thumbnail, cancellationToken));
                }
            }
            return frames;
        }

        public async Task<RgbImage> LoadImageAsync(string path, CancellationToken cancellationToken)
        {
            var file = await StorageFile.GetFileFromPathAsync(path).AsTask(cancellationToken);
            using var stream = await file.OpenReadAsync().AsTask(cancellationToken);
            return await DecodeAsync(stream, cancellationToken);
        }

        private static async Task<RgbImage> DecodeAsync(IRandomAccessStream stream, CancellationToken cancellationToken)
        {
            var decoder = await BitmapDecoder.CreateAsync(stream).AsTask(cancellationToken);

            // Resize the shortest edge to the target size, then center-crop.
            var size = (uint)ClipPreprocessor.Size;
            var width = decoder.OrientedPixelWidth;
            var height = decoder.OrientedPixelHeight;
            var scale = (double)size / Math.Min(width, height);
            var scaledWidth = Math.Max(size, (uint)Math.Round(width * scale));
            var scaledHeight = Math.Max(size, (uint)Math.Round(height * scale));

            var transform = new BitmapTransform
            {
                ScaledWidth = scaledWidth,
                ScaledHeight = scaledHeight,
                InterpolationMode = BitmapInterpolationMode.Cubic,
                Bounds = new BitmapBounds
                {
                    X = (scaledWidth - size) / 2,
                    Y = (scaledHeight - size) / 2,
                    Width = size,
                    Height = size,
                },
            };

            var pixels = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Rgba8,
                BitmapAlphaMode.Ignore,
                transform,
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.DoNotColorManage).AsTask(cancellationToken);

            return new RgbImage(pixels.DetachPixelData(), (int)size, (int)size);
        }
    }
}
