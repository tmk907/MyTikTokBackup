using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Toolkit.Mvvm.Messaging;
using Serilog;
using MyTikTokBackup.Core.Messages;
using MyTikTokBackup.Core.Models;
using MyTikTokBackup.Core.TikTok;

namespace MyTikTokBackup.Core.Services
{
    public interface IDownloadQueueItem
    {
        DownloadStatus DownloadStatus { get; }
        VideoSource VideoSource { get; }
        string FilePath { get; }
    }

    public class DownloadQueueItem : IDownloadQueueItem
    {
        public List<Header> Headers { get; set; }
        public string Description { get; set; }
        public string VideoId { get; set; }
        public string PlayAddress { get; set; }
        public string FilePath { get; set; }
        public VideoSource VideoSource { get; set; }

        public ItemInfo ItemInfo { get; set; }

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

        public bool IsVideoDownloaded { get; set; }

        public static DownloadQueueItem Create(ItemInfo fav, VideoSource videoSource, string filePath, DownloadStatus downloadStatus = DownloadStatus.NotDownloaded)
        {
            return new DownloadQueueItem
            {
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

    public class ImagePostDownloadQueueItem : IDownloadQueueItem
    {
        public List<Header> Headers { get; set; }
        public string Description { get; set; }
        public string VideoId { get; set; }
        public string FilePath { get; private set; }
        public VideoSource VideoSource { get; set; }

        public ItemInfo ItemInfo { get; set; }

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

        public bool IsVideoDownloaded { get; set; }

        public List<(string url,string filePath)> ImageUrls { get; private set; } = new List<(string url, string filePath)>();

        public static ImagePostDownloadQueueItem Create(ItemInfo fav, VideoSource videoSource, string filePath, DownloadStatus downloadStatus = DownloadStatus.NotDownloaded)
        {
            var item = new ImagePostDownloadQueueItem
            {
                Description = fav.Desc,
                Headers = fav.Headers.ToList(),
                VideoId = fav.Id,
                VideoSource = videoSource,
                downloadStatus = downloadStatus
            };

            var urls = fav.ImagePost.Images
                .Select(x => x.ImageURL.UrlList.FirstOrDefault())
                .ToList();

            item.ImageUrls = urls
                .Where(url => url != null)
                .Select((url, index) => (url, filePath.Replace("%%", index.ToString())))
                .ToList();
            item.FilePath = item.ImageUrls.FirstOrDefault().filePath;

            return item;
        }
    }
}
