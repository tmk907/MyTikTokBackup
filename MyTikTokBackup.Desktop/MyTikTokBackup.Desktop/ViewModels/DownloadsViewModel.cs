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
using MyTikTokBackup.WindowsUWP.Helpers;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class DownloadsViewModel : ObservableObject
    {
        private readonly IDownloadsManager _downloadsManager;
        private readonly IImportService _importService;
        private readonly IDispatcher _dispatcher;

        public DownloadsViewModel(IDownloadsManager downloadsManager,
            IImportService importService, IDispatcher dispatcher)
        {
            _downloadsManager = downloadsManager;
            _importService = importService;
            _dispatcher = dispatcher;
            ClearCommand = new RelayCommand(Clear);
        }

        public async Task Init()
        {
            UpdateAll();

            StrongReferenceMessenger.Default.Register<DownloadStatusChanged>(this, (r, m) =>
            {
                Log.Information($"DownloadStatusChanged {m.Id} {m.DownloadStatus}");

                var item = Videos.FirstOrDefault(x => x.Id == m.Id);
                if (item != null)
                {
                    _dispatcher.Run(() =>
                    {
                        item.DownloadStatus = m.DownloadStatus;
                        Downloaded = _downloadsManager.GetQueueState().Downloaded;
                        Total = _downloadsManager.GetQueueState().Total;
                    });
                }
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

        public ObservableRangeCollection<DownloadVideo> Videos { get; } = new ObservableRangeCollection<DownloadVideo>();

        public IRelayCommand ClearCommand { get; }

        private void Clear()
        {
            _downloadsManager.Clear();
            UpdateAll();
        }

        private void UpdateAll()
        {
            Videos.ReplaceRange(_downloadsManager.ItemsToDownload.Select(x => new DownloadVideo
            {
                FilePath = x.FilePath,
                Id = x.VideoId,
                Title = System.IO.Path.GetFileNameWithoutExtension(x.FilePath),
                DownloadStatus = x.DownloadStatus,
            }));
            Downloaded = _downloadsManager.GetQueueState().Downloaded;
            Total = _downloadsManager.GetQueueState().Total;
        }

        private bool _canExecute = true;
        private void NotifyCanExecuteChanged(bool canExecute)
        {
            _canExecute = canExecute;
            //DownloadCommand.NotifyCanExecuteChanged();
            //DownloadAndImportCommand.NotifyCanExecuteChanged();
        }
    }
}
