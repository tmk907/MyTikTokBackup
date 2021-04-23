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
using EnumsNET;

namespace MyTikTokBackup.Core.Services
{
    public record VideoSource(string UserUniqueId, string Type);

    public class DownloadQueueItem
    {
        public List<Header> Headers { get; set; }
        public string AuthorUniqueId { get; set; }
        public string Description { get; set; }
        public string VideoId { get; set; }
        public string PlayAddress { get; set; }
        public string FilePath { get; set; }
        public VideoSource VideoSource { get; set; }

        private DownloadStatus downloadStatus = DownloadStatus.NotDownloaded;
        public DownloadStatus DownloadStatus
        {
            get { return downloadStatus; }
            set 
            { 
                downloadStatus = value;
                try
                {
                    StrongReferenceMessenger.Default.Send(new DownloadStatusChanged(VideoId, value, FilePath));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }

        public static DownloadQueueItem Create(ItemInfo fav, VideoSource videoSource, string filePath, DownloadStatus downloadStatus = DownloadStatus.NotDownloaded)
        {
            return new DownloadQueueItem
            {
                AuthorUniqueId = fav.Author.UniqueId,
                Description = fav.Desc,
                Headers = fav.Headers.ToList(),
                PlayAddress = fav.Video.PlayAddr,
                VideoId = fav.Id,
                FilePath = filePath,
                VideoSource = videoSource,
                downloadStatus = downloadStatus
            };
        }
    }

    public record QueueState(int Downloaded, int Total);

    public interface IDownloadsManager
    {
        IEnumerable<DownloadQueueItem> ItemsToDownload { get; }

        void Cancel();
        void QueueVideos(string user, DownloadType type, IEnumerable<ItemInfo> items);
        QueueState GetQueueState();
    }

    public class DownloadsManager : IDownloadsManager
    {
        private readonly DownloadService2 _downloadService;
        private readonly ILocalVideosService _localVideosService;
        private readonly IAppConfiguration _appConfiguration;
        private readonly ConcurrentDictionary<string, DownloadQueueItem> _itemsToDownload;

        public DownloadsManager(ILocalVideosService localVideosService, IAppConfiguration appConfiguration)
        {
            _downloadService = new DownloadService2();
            _localVideosService = localVideosService;
            _appConfiguration = appConfiguration;
            _itemsToDownload = new ConcurrentDictionary<string, DownloadQueueItem>();
            StrongReferenceMessenger.Default.Register<DownloadStatusChanged>(this, (r, m) =>
            {
                if (m.DownloadStatus == DownloadStatus.Downloaded) downloadedCount++;
            });
        }

        private int downloadedCount = 0;
        public IEnumerable<DownloadQueueItem> ItemsToDownload => _itemsToDownload.Values.ToList();

        public void QueueVideos(string user, DownloadType type, IEnumerable<ItemInfo> items)
        {
            _localVideosService.Refresh();
            var toDownload = items
                .Where(x => !_itemsToDownload.ContainsKey(x.Id))
                .ToList();
            var queue = new List<DownloadQueueItem>();
            foreach(var item in toDownload)
            {
                var videoSource = new VideoSource(user, type.ToString());
                var filePath = PrepareFilePath(videoSource, item);
                var status = GetStatus(item.Id, item.Video.PlayAddr);
                var queueItem = DownloadQueueItem.Create(item, videoSource, filePath, status);
                _itemsToDownload.TryAdd(queueItem.VideoId, queueItem);
                queue.Add(queueItem);
            }

            _downloadService.QueueItems(queue);
        }

        public void Cancel()
        {
            _itemsToDownload.Clear();
            downloadedCount = 0;
            _downloadService.Cancel();
        }

        public QueueState GetQueueState()
        {
            var downloadedCount = _itemsToDownload.Count(x => x.Value.DownloadStatus == DownloadStatus.Downloaded);
            return new QueueState(downloadedCount, _itemsToDownload.Count);
        }

        private string PrepareFilePath(VideoSource videoSource, ItemInfo item)
        {
            var videoName = $"{item.Author.UniqueId} - {item.Desc} [{item.Id}]";
            videoName = FilePathHelper.RemoveForbiddenChars(videoName);
            var folderPath = Path.Combine(_appConfiguration.DownloadsFolder, videoSource.UserUniqueId, videoSource.Type);
            Directory.CreateDirectory(folderPath);
            var filePath = Path.Combine(folderPath, videoName + ".mp4");
            return filePath;
        }

        private DownloadStatus GetStatus(string id, string address)
        {
            if (_localVideosService.GetPath(id) != "")
            {
                Log.Warning($"{nameof(GetStatus)} Video id {id} is already downloaded");
                return DownloadStatus.Downloaded;
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
        private readonly BlockingCollection<DownloadQueueItem> _queue;
        private HttpClient _client;
        private ActionBlock<DownloadQueueItem> _actionBlock;
        private CancellationTokenSource cts;

        public DownloadService2()
        {
            _client = new HttpClient();
            cts = new CancellationTokenSource();
            _queue = new BlockingCollection<DownloadQueueItem>();
            int maxParallel = 4;
            _actionBlock = new ActionBlock<DownloadQueueItem>((item) => Download(item, cts.Token),
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

        public void QueueItems(IEnumerable<DownloadQueueItem> items)
        {
            foreach (var item in items.Where(x => x.DownloadStatus != DownloadStatus.Downloaded))
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

        private async Task Download(DownloadQueueItem item, CancellationToken cancellationToken)
        {
            Log.Information($"Start download {item.VideoId} {item.Description}");
            item.DownloadStatus = DownloadStatus.Downloading;
            try
            {
                var msg = new HttpRequestMessage(HttpMethod.Get, new Uri(item.PlayAddress));
                foreach (var header in item.Headers)
                {
                    msg.Headers.Add(header.Name, header.Value);
                }
                using var response = await _client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                Log.Information($"Download response success {item.VideoId} {item.Description}");

                using Stream contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(item.FilePath, FileMode.Create);
                await contentStream.CopyToAsync(fileStream, cancellationToken);
                
                item.DownloadStatus = DownloadStatus.Downloaded;
            }
            catch (Exception ex)
            {
                Log.Information($"Download response error {item.VideoId} {item.Description}");
                Log.Error(ex.ToString());
                if (File.Exists(item.FilePath))
                {
                    Log.Warning($"{nameof(Download)} Delete file {item.FilePath}");
                    File.Delete(item.FilePath);
                }
                item.DownloadStatus = DownloadStatus.Error;
            }
        }
    }

    public static class ListExtenstions
    {
        public static void RemoveIfExist<T>(this List<T> list, Func<T,bool> predicate)
        {
            var item = list.FirstOrDefault(predicate);
            if (item != null)
            {
                list.Remove(item);
            }
        }
    }
}
