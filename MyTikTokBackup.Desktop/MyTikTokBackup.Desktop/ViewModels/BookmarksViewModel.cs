using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using MvvmHelpers;
using Newtonsoft.Json;
using MyTikTokBackup.Core.Services;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public record Bookmark(string User, string Url);
    public record UserChangedMessage(string User);
    public record AddressChangedMessage(string Url);

    public class BookmarksViewModel : ObservableRecipient, 
        IRecipient<UserChangedMessage>, IRecipient<AddressChangedMessage>
    {
        public BookmarksViewModel()
        {
            Bookmarks.AddRange(RestoreBookmarks());
            IsActive = true;
        }

        public bool IsBookmarked => Bookmarks.Any(x => x.User == currentUser);

        public Bookmark SelectedBookmark { get; set; }

        private string currentUser;
        public string CurrentUser
        {
            get { return currentUser; }
            set 
            { 
                SetProperty(ref currentUser, value);
                OnPropertyChanged(nameof(IsBookmarked));
            }
        }

        private string currentUrl;
        public string CurrentUrl
        {
            get { return currentUrl; }
            set 
            { 
                SetProperty(ref currentUrl, value);
            }
        }

        public ObservableRangeCollection<Bookmark> Bookmarks { get; } = new ObservableRangeCollection<Bookmark>();

        public void AddToBookmarks()
        {
            var url = CurrentUrl;
            var user = GetUserFromUrl(url);
            AddUsersToBookmarks(new[] { user });
        }

        public void RemoveFromBookmarks()
        {
            Bookmark bookmark = SelectedBookmark;
            if (bookmark == null)
            {
                var url = CurrentUrl;
                var user = GetUserFromUrl(url);
                SerilogHelper.LogInfo(url);
                if (string.IsNullOrEmpty(user)) return;
                bookmark = Bookmarks.FirstOrDefault(x => x.User == user);
            }
            if (bookmark != null)
            {
                Bookmarks.Remove(bookmark);
                SaveBookmarks(Bookmarks);
                OnPropertyChanged(nameof(IsBookmarked));
            }
        }

        public void AddUsersToBookmarks(IEnumerable<string> users)
        {
            var toAdd = users
                .Select(user => user.StartsWith('@') ? user : "@" + user)
                .Where(user => !Bookmarks.Any(b => b.User == user))
                .Select(user => new Bookmark(user, $"https://tiktok.com/{user}"));
            Bookmarks.AddRange(toAdd);
            SaveBookmarks(Bookmarks);
            OnPropertyChanged(nameof(IsBookmarked));
        }


        private void SaveBookmarks(IEnumerable<Bookmark> favs)
        {
            Xamarin.Essentials.Preferences.Set("bookmarks", JsonConvert.SerializeObject(Bookmarks));
        }

        private IEnumerable<Bookmark> RestoreBookmarks()
        {
            var serialized = Xamarin.Essentials.Preferences.Get("bookmarks", "[]");
            return JsonConvert.DeserializeObject<List<Bookmark>>(serialized);
        }

        public static string GetUserFromUrl(string url)
        {
            var segment = Url.Parse(url).PathSegments.FirstOrDefault() ?? "";
            return segment.StartsWith('@') ? segment : "";
        }

        public void Receive(UserChangedMessage message)
        {
            CurrentUser = message.User;
        }

        public void Receive(AddressChangedMessage message)
        {
            CurrentUrl = message.Url;
        }
    }
}
