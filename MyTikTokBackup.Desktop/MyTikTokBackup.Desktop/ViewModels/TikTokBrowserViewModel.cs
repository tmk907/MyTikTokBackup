using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using MyTikTokBackup.Core.Services;
using MyTikTokBackup.Desktop.Services;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class TikTokBrowserViewModel : ObservableObject
    {
        public TikTokBrowserViewModel(INavigationService navigationService, IDownloadsManager downloadsManager)
        {
            BookmarksVM = new BookmarksViewModel();
            FetchFavoriteVideosVM = new FetchFavoriteVideosViewModel(navigationService, downloadsManager);
            FetchPostedVideosVM = new FetchPostedVideosViewModel(navigationService, downloadsManager);
            FindFollowingVM = new FindFollowingViewModel();
        }

        public BookmarksViewModel BookmarksVM { get; }
        public FetchPostedVideosViewModel FetchPostedVideosVM { get; }
        public FetchFavoriteVideosViewModel FetchFavoriteVideosVM { get; }
        public FindFollowingViewModel FindFollowingVM { get; }

        private bool useMobileVersion = false;
        public bool UseMobileVersion
        {
            get { return useMobileVersion; }
            set { SetProperty(ref useMobileVersion, value); }
        }

        public string StartAddress { get; set; } = "https://www.tiktok.com/";

        public void AddAllFollowingToBookmarks()
        {
            BookmarksVM.AddUsersToBookmarks(FindFollowingVM.Users);
        }
    }
}
