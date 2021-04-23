using System.Collections.Generic;
using System.Linq;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using MvvmHelpers;
using MyTikTokBackup.Core.Models;
using MyTikTokBackup.Core.Services;
using MyTikTokBackup.Core.TikTok;
using MyTikTokBackup.Desktop.Services;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public abstract class FetchVideosViewModel : ObservableRecipient, IRecipient<UserChangedMessage>
    {
        private readonly INavigationService _navigationService;
        private readonly IDownloadsManager _downloadsManager;

        public IEnumerable<Header> Headers { get; set; }
        protected DownloadType type = DownloadType.Other;
        
        public FetchVideosViewModel(INavigationService navigationService, IDownloadsManager downloadsManager)
        {
            _navigationService = navigationService;
            _downloadsManager = downloadsManager;
            DownloadVideosCommand = new RelayCommand(() => AddVideosToDownloadQueue());
            IsActive = true;
        }

        private string user;
        public string User
        {
            get { return user; }
            set { SetProperty(ref user, value); }
        }
        
        public ObservableRangeCollection<ItemInfo> Videos { get; } = new ObservableRangeCollection<ItemInfo>();
        public RelayCommand DownloadVideosCommand { get; }

        public void Receive(UserChangedMessage message)
        {
            User = message.User;
            Videos.Clear();
        }

        protected IEnumerable<ItemInfo> GetItemsWithHeaders()
        {
            var headers = Headers.ToList();
            var items = Videos;
            foreach (var item in items)
            {
                item.Headers = headers;
            }
            return items;
        }

        private void AddVideosToDownloadQueue()
        {
            if (Videos.Count == 0) return;
            AddToDownloadQueue(User, type, GetItemsWithHeaders());
        }

        protected void AddToDownloadQueue(string user, DownloadType type, IEnumerable<ItemInfo> items)
        {
            _downloadsManager.QueueVideos(user, type, items);
            _navigationService.GoToNew(nameof(DownloadsViewModel));
        }
    }

    public class FetchPostedVideosViewModel : FetchVideosViewModel
    {
        public FetchPostedVideosViewModel(INavigationService navigationService, IDownloadsManager downloadsManager)
            : base(navigationService, downloadsManager)
        {
            type = DownloadType.Posted;
        }
    }

    public class FetchFavoriteVideosViewModel: FetchVideosViewModel
    {
        public FetchFavoriteVideosViewModel(INavigationService navigationService, IDownloadsManager downloadsManager)
            : base(navigationService, downloadsManager)
        {
            type = DownloadType.Favorite;
        }
    }
}
