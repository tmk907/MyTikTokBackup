using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Toolkit.Mvvm.Messaging;
using Serilog;
using MyTikTokBackup.Core.Helpers;
using MyTikTokBackup.Core.Messages;
using MyTikTokBackup.Core.Models;
using MyTikTokBackup.Core.TikTok;
using MyTikTokBackup.Core.Database;

namespace MyTikTokBackup.Core.Services
{
    public record VideoSource(string UserUniqueId, string Type);

    public record QueueState(int Downloaded, int Total);

    public interface IDownloadsManager
    {
        IEnumerable<IDownloadQueueItem> ItemsToDownload { get; }

        void Cancel();
        Task QueueVideos(string user, DownloadType type, IEnumerable<ItemInfo> items);
    }

    public class DownloadsManager : IDownloadsManager
    {
        private readonly DownloadService2 _downloadService;
        private readonly ILocalVideosService _localVideosService;
        private readonly IAppConfiguration _appConfiguration;
        private readonly ConcurrentDictionary<string, IDownloadQueueItem> _itemsToDownload;

        public DownloadsManager(ILocalVideosService localVideosService, IAppConfiguration appConfiguration)
        {
            _downloadService = new DownloadService2();
            _localVideosService = localVideosService;
            _appConfiguration = appConfiguration;
            _itemsToDownload = new ConcurrentDictionary<string, IDownloadQueueItem>();
            StrongReferenceMessenger.Default.Register<DownloadStatusChanged>(this, (r, m) =>
            {
                if (m.DownloadStatus == DownloadStatus.Downloaded) downloadedCount++;
            });
        }

        private int downloadedCount = 0;
        public IEnumerable<IDownloadQueueItem> ItemsToDownload => _itemsToDownload.Values.ToList();

        public async Task QueueVideos(string user, DownloadType type, IEnumerable<ItemInfo> items)
        {
            _localVideosService.Refresh();
            var toDownload = items
                .Where(x => !_itemsToDownload.ContainsKey(x.Id))
                .ToList();
            var queue = new List<IDownloadQueueItem>();
            foreach(var item in toDownload)
            {
                if (item.ImagePost?.Images?.Count > 0)
                {
                    var videoSource = new VideoSource(user, type.ToString());
                    var filePath = PrepareImageFilePath(videoSource, item);
                    var status = GetStatus(item.Id, item.ImagePost.Images.FirstOrDefault()?.ImageURL.UrlList.FirstOrDefault());
                    var queueItem = ImagePostDownloadQueueItem.Create(item, videoSource, filePath, status);
                    queueItem.ItemInfo = item;
                    queueItem.IsVideoDownloaded = _localVideosService.GetPath(item.Video.Id) != "";
                    _itemsToDownload.TryAdd(queueItem.VideoId, queueItem);
                    queue.Add(queueItem);
                }
                else
                {
                    var videoSource = new VideoSource(user, type.ToString());
                    var filePath = PrepareFilePath(videoSource, item);
                    var status = GetStatus(item.Id, item.Video.PlayAddr);
                    var queueItem = DownloadQueueItem.Create(item, videoSource, filePath, status);
                    queueItem.ItemInfo = item;
                    queueItem.IsVideoDownloaded = _localVideosService.GetPath(item.Video.Id) != "";
                    _itemsToDownload.TryAdd(queueItem.VideoId, queueItem);
                    queue.Add(queueItem);
                }
            }
            if (type == DownloadType.Favorite || type == DownloadType.Posted)
            {
                var t = FeedType.Liked;
                if (type == DownloadType.Posted)
                {
                    t = FeedType.Posted;
                }
                var helper = new DatabaseHelper();
                await helper.AddOrUpdateProfileVideos(user, t, items.Select(x => x.Video.Id));
            }

            Log.Information($"{nameof(QueueVideos)} items: {items.Count()}, to download: {toDownload.Count} queue {queue.Count} _itemsToDownload {_itemsToDownload.Count}");
            _downloadService.QueueItems(queue);
        }

        public void Cancel()
        {
            _itemsToDownload.Clear();
            downloadedCount = 0;
            _downloadService.Cancel();
        }

        private string PrepareFilePath(VideoSource videoSource, ItemInfo item)
        {
            var folderPath = Path.Combine(_appConfiguration.DownloadsFolder, videoSource.UserUniqueId, videoSource.Type);
            Directory.CreateDirectory(folderPath);

            var videoName = $"{item.Author.UniqueId} - {item.Desc} [{item.Video.Id}].mp4";

            var maxPathLength = 255;
            var maxFilenameLength = maxPathLength - folderPath.Length;
            if (videoName.Length > maxFilenameLength)
            {
                var maxDescLength = maxFilenameLength - $"{item.Author.UniqueId} -  [{item.Video.Id}].mp4".Length;
                var dots = "...";
                var desc = item.Desc.Substring(0, maxDescLength - dots.Length);
                videoName = $"{item.Author.UniqueId} - {desc}{dots} [{item.Video.Id}].mp4";
            }
            videoName = FilePathHelper.RemoveForbiddenChars(videoName);

            var filePath = Path.Combine(folderPath, videoName);
            return filePath;
        }

        private string PrepareImageFilePath(VideoSource videoSource, ItemInfo item)
        {
            var folderPath = Path.Combine(_appConfiguration.DownloadsFolder, videoSource.UserUniqueId, videoSource.Type);
            Directory.CreateDirectory(folderPath);
            
            var videoName = $"{item.Author.UniqueId} - {item.Desc} (%%)[{item.Video.Id}].jpeg";

            var maxPathLength = 255;
            var maxFilenameLength = maxPathLength - folderPath.Length;
            if (videoName.Length > maxFilenameLength)
            {
                var maxDescLength = maxFilenameLength - $"{item.Author.UniqueId} - (%%)[{item.Video.Id}].jpeg".Length;
                var dots = "...";
                var desc = item.Desc.Substring(0, maxDescLength - dots.Length);
                videoName = $"{item.Author.UniqueId} - {desc}{dots} (%%)[{item.Video.Id}].jpeg";
            }
            videoName = FilePathHelper.RemoveForbiddenChars(videoName);

            var filePath = Path.Combine(folderPath, videoName);
            return filePath;
        }

        private DownloadStatus GetStatus(string id, string address)
        {
            if (_localVideosService.GetPath(id) != "")
            {
                Log.Warning($"{nameof(GetStatus)} Video id {id} is already downloaded");
            }
            if (string.IsNullOrEmpty(address))
            {
                Log.Error($"{nameof(GetStatus)} Video {id} has no download address.");
                return DownloadStatus.Error;
            }
            //if (File.Exists(item.FilePath))
            //{
            //    Log.Warning($"{nameof(CanDownload)} Video id {item.VideoId} is already downloaded {item.AuthorUniqueId} {item.Description}");
            //    return DownloadStatus.Downloaded;
            //}
            return DownloadStatus.NotDownloaded;
        }
    }

    public class DownloadService2
    {
        private readonly BlockingCollection<IDownloadQueueItem> _queue;
        private HttpClient _client;
        private ActionBlock<IDownloadQueueItem> _actionBlock;
        private CancellationTokenSource cts;

        private MetadataService metadataService = new MetadataService();
        private ThumbnailsService thumbnailsService = new ThumbnailsService();

        public DownloadService2()
        {            
            _client = new HttpClient();
            cts = new CancellationTokenSource();
            _queue = new BlockingCollection<IDownloadQueueItem>();
            int maxParallel = 4;
            _actionBlock = new ActionBlock<IDownloadQueueItem>((item) => Download(item, cts.Token),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = maxParallel,
                    MaxDegreeOfParallelism = maxParallel,
                });
            Task.Run(async () => await Consume());
        }

        private async Task Consume()
        {
            while (true)
            {
                var item = _queue.Take();
                try
                {
                    await _actionBlock.SendAsync(item);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }

        public void QueueItems(IEnumerable<IDownloadQueueItem> items)
        {
            foreach (var item in items)
            {
                _queue.Add(item);
            }
        }

        public void Cancel()
        {
            cts.Cancel();
            cts.Dispose();
            cts = new CancellationTokenSource();
            while (_queue.TryTake(out _)) { }
        }

        private async Task Download(IDownloadQueueItem item, CancellationToken cancellationToken)
        {
            switch (item)
            {
                case DownloadQueueItem videoItem:
                    await DownloadVideo(videoItem, cancellationToken); 
                    break;
                case ImagePostDownloadQueueItem imageItem:
                    await DownloadImagePost(imageItem, cancellationToken);
                    break;
                default: break;
            }
        }

        private async Task DownloadVideo(DownloadQueueItem item, CancellationToken cancellationToken)
        {
            Log.Information($"Start download {item.VideoId} {item.Description}");
            item.DownloadStatus = DownloadStatus.Downloading;
            var tempPath = item.FilePath + ".download";
            try
            {
                await metadataService.AddOrUpdateMetadataFromVideo(item.ItemInfo).ConfigureAwait(false);
                await thumbnailsService.DownloadThumbnailsAsync(item.ItemInfo, cancellationToken).ConfigureAwait(false);

                if (item.IsVideoDownloaded || File.Exists(item.FilePath))
                {
                    item.DownloadStatus = DownloadStatus.Downloaded;
                    return;
                }

                await DownloadVideoFile(item, tempPath, cancellationToken).ConfigureAwait(false);
                item.DownloadStatus = DownloadStatus.Downloaded;
            }
            catch (Exception ex)
            {
                Log.Information($"Download error {item.VideoId} {item.Description}");
                Log.Error(ex.ToString());
                if (File.Exists(tempPath))
                {
                    Log.Warning($"{nameof(Download)} Delete file {tempPath}");
                    File.Delete(tempPath);
                }
                item.DownloadStatus = DownloadStatus.Error;
            }
        }

        private async Task DownloadImagePost(ImagePostDownloadQueueItem item, CancellationToken cancellationToken)
        {
            Log.Information($"Start download {item.VideoId} {item.Description}");
            item.DownloadStatus = DownloadStatus.Downloading;
            try
            {
                await metadataService.AddOrUpdateMetadataFromVideo(item.ItemInfo).ConfigureAwait(false);
                await thumbnailsService.DownloadThumbnailsAsync(item.ItemInfo, cancellationToken).ConfigureAwait(false);

                if (item.IsVideoDownloaded || File.Exists(item.FilePath))
                {
                    item.DownloadStatus = DownloadStatus.Downloaded;
                    return;
                }

                var downloadTasks = item.ImageUrls
                    .Select(x => DownloadFile(x.url, x.filePath, item.Headers, cancellationToken));

                await Task.WhenAll(downloadTasks).ConfigureAwait(false);

                item.DownloadStatus = DownloadStatus.Downloaded;
            }
            catch (Exception ex)
            {
                Log.Information($"Download error {item.VideoId} {item.Description}");
                Log.Error(ex.ToString());
                if (File.Exists(item.FilePath))
                {
                    Log.Warning($"{nameof(Download)} Delete file {item.FilePath}");
                    File.Delete(item.FilePath);
                }
                item.DownloadStatus = DownloadStatus.Error;
            }
        }

        private async Task DownloadVideoFile(DownloadQueueItem item, string tempPath, CancellationToken cancellationToken)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get, new Uri(item.PlayAddress));
            foreach (var header in item.Headers)
            {
                msg.Headers.Add(header.Name, header.Value);
            }
            var response = await _client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            Log.Information($"Download response success {item.VideoId} {item.Description}");

            using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    await contentStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                }
            }
            var fileInfo = new FileInfo(tempPath);
            fileInfo.MoveTo(item.FilePath);
        }

        private async Task DownloadFile(string url, string filePath, List<Header> headers, CancellationToken cancellationToken)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
            foreach (var header in headers)
            {
                msg.Headers.Add(header.Name, header.Value);
            }
            var response = await _client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            Log.Information($"File download response success {filePath}");

            using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await contentStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    public static class ListExtenstions
    {
        public static void RemoveIfExist<T>(this List<T> list, Func<T,bool> predicate)
        {
            var items = list.Where(predicate);
            foreach(var item in items)
            {
                list.Remove(item);
            }
        }
    }
}
