using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using MyTikTokBackup.Core.TikTok;
using Serilog;

namespace MyTikTokBackup.Core.Services
{
    public class ThumbnailsService
    {
        private readonly string _thumbnailsFolder;

        private string _authorFolder;
        private string _musicFolder;

        private HttpClient _client;

        public ThumbnailsService()
        {
            var appConfiguration = Ioc.Default.GetService<IAppConfiguration>();
            _thumbnailsFolder = Path.Combine(appConfiguration.DownloadsFolder, "Cache");
            _authorFolder = Path.Combine(_thumbnailsFolder, "Author");
            _musicFolder = Path.Combine(_thumbnailsFolder, "Music");
            _client = new HttpClient();
        }

        public async Task DownloadThumbnailsAsync(ItemInfo item, CancellationToken cancellationToken)
        {
            try
            {
                var downloadTasks = new[] {
                    DownloadAsync(item.Author.AvatarLarger, _authorFolder, $"{item.Author.Id}-AvatarLarger{GetExtension(item.Author.AvatarLarger)}", cancellationToken),
                    DownloadAsync(item.Author.AvatarThumb, _authorFolder, $"{item.Author.Id}-AvatarThumb{GetExtension(item.Author.AvatarThumb)}", cancellationToken),

                    DownloadAsync(item.Music.PlayUrl, _musicFolder, $"{item.Music.Id}", cancellationToken),
                    DownloadAsync(item.Music.CoverLarge, _musicFolder, $"{item.Music.Id}-CoverLarge", cancellationToken),
                    DownloadAsync(item.Music.CoverThumb, _musicFolder, $"{item.Music.Id}-CoverThumb", cancellationToken),
                };

                await Task.WhenAll(downloadTasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private async Task DownloadAsync(string url, string folderPath, string filename, CancellationToken cancellationToken)
        {
            try
            {
                Directory.CreateDirectory(folderPath);
                var filePath = Path.Combine(folderPath, $"{filename}{GetExtension(url)}");
                if (File.Exists(filePath))
                {
                    return;
                }
                await DownloadAsync(url, filePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Download {Filename}", filename);
            }
        }

        private async Task DownloadAsync(string url, string filePath, CancellationToken cancellationToken)
        {
            var tempPath = filePath + ".download";

            var msg = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
            using var response = await _client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    await contentStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                }
            }
            var fileInfo = new FileInfo(tempPath);
            fileInfo.MoveTo(filePath);
        }

        private string GetExtension(string url)
        {
            var uri = new Uri(url);
            return Path.GetExtension(uri.GetLeftPart(UriPartial.Path));
        }
    }
}
