using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using MvvmHelpers;
using Serilog;
using MyTikTokBackup.Core.Messages;
using MyTikTokBackup.Core.Models;
using MyTikTokBackup.Core.Services;
using System.Collections.ObjectModel;
using System.IO;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class DownloadsViewModel : ObservableObject
    {
        private readonly IDownloadsManager _downloadsManager;
        private readonly IDispatcher _dispatcher;
        private readonly IStorageService _storageService;

        public DownloadsViewModel(IDownloadsManager downloadsManager, IDispatcher dispatcher, IStorageService storageService)
        {
            _downloadsManager = downloadsManager;
            _dispatcher = dispatcher;
            _storageService = storageService;
            CancelCommand = new RelayCommand(Cancel);
        }

        public async Task Init()
        {
            UpdateState();
            UpdateQueueByUser();

            StrongReferenceMessenger.Default.Register<DownloadStatusChanged>(this, (r, m) =>
            {
                Log.Information($"DownloadStatusChanged {m.Id} {m.DownloadStatus}");

                _dispatcher.Run(() =>
                {
                    UpdateState();
                });
            });
        }

        public async Task UnInit()
        {
            StrongReferenceMessenger.Default.Unregister<DownloadStatusChanged>(this);
        }

        private int downloaded;
        public int Downloaded
        {
            get { return downloaded; }
            set { SetProperty(ref downloaded, value); }
        }

        private int total;
        public int Total
        {
            get { return total; }
            set { SetProperty(ref total, value); }
        }


        private int error;
        public int Error
        {
            get { return error; }
            set { SetProperty(ref error, value); }
        }

        public ObservableRangeCollection<string> DownloadingVideos { get; } = new ObservableRangeCollection<string>();

        public ObservableCollection<UserQueueState> QueueByUser { get; } = new ObservableCollection<UserQueueState>();

        public IRelayCommand CancelCommand { get; }

        private void Cancel()
        {
            _downloadsManager.Cancel();
        }

        public async Task OpenFolder(UserQueueState item)
        {
            var filePath = _downloadsManager.ItemsToDownload
                .FirstOrDefault(x => x.VideoSource == item.VideoSource)?.FilePath;
            if (string.IsNullOrEmpty(filePath)) return;
            await _storageService.OpenFolder(Path.GetDirectoryName(filePath));
        }

        private void UpdateState()
        {
            UpdateDownloadingVideos();
            UpdateQueueByUser();

            Total = _downloadsManager.ItemsToDownload.Count();
            Downloaded = _downloadsManager.ItemsToDownload.Count(x => x.DownloadStatus == DownloadStatus.Downloaded);
            Error = _downloadsManager.ItemsToDownload.Count(x => x.DownloadStatus == DownloadStatus.Error);
        }

        private void UpdateDownloadingVideos()
        {
            var downloading = _downloadsManager.ItemsToDownload
                .Where(x => x.DownloadStatus == DownloadStatus.Downloading)
                .Select(x => System.IO.Path.GetFileNameWithoutExtension(x.FilePath)).ToList();
            var toAdd = downloading.Where(x => !DownloadingVideos.Contains(x)).ToList();
            var toRemove = DownloadingVideos.Except(downloading).ToList();
            foreach (var item in toAdd)
            {
                DownloadingVideos.Add(item);
            }
            foreach (var item in toRemove)
            {
                DownloadingVideos.Remove(item);
            }
        }

        private void UpdateQueueByUser()
        {
            var grouped = _downloadsManager.ItemsToDownload.GroupBy(x => x.VideoSource).ToList();
            var toRemove = QueueByUser.Where(x => !grouped.Any(g => g.Key == x.VideoSource)).ToList();
            toRemove.ForEach(x => QueueByUser.Remove(x));
            foreach (var group in grouped)
            {
                var a = QueueByUser.FirstOrDefault(x => x.VideoSource == group.Key);
                if (a == null)
                {
                    a = new UserQueueState();                    
                    QueueByUser.Add(a);
                }
                a.VideoSource = group.Key;
                a.Downloaded = group.Count(x => x.DownloadStatus == DownloadStatus.Downloaded);
                a.Error = group.Count(x => x.DownloadStatus == DownloadStatus.Error);
                a.Total = group.Count();
            }
        }
    }

    public class UserQueueState : ObservableObject
    {
        private int downloaded;
        public int Downloaded
        {
            get { return downloaded; }
            set { SetProperty(ref downloaded, value); }
        }

        private int total;
        public int Total
        {
            get { return total; }
            set { SetProperty(ref total, value); }
        }


        private int error;
        public int Error
        {
            get { return error; }
            set { SetProperty(ref error, value); }
        }

        public VideoSource VideoSource { get; set; }

        public override bool Equals(object obj)
        {
            return obj is UserQueueState state &&
                   EqualityComparer<VideoSource>.Default.Equals(VideoSource, state.VideoSource);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VideoSource);
        }
    }
}
