using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using MvvmHelpers;
using MyTikTokBackup.Core.Database;
using MyTikTokBackup.Core.Helpers;
using MyTikTokBackup.Core.Models;
using MyTikTokBackup.Core.Services;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class VideoUI
    {
        public Core.Database.Video Video { get; init; }
        public List<string> Categories { get; init; }
        public string FilePath { get; init; }
        public string Url => $"https://www.tiktok.com/@{Video.Author.UniqueId}/video/{Video.VideoId}";
    }

    public class ProfileVideosViewModel : ObservableObject
    {
        private readonly Regex _regex;
        private readonly IAppConfiguration _appConfiguration;
        private readonly IDispatcher _dispatcher;

        public ProfileVideosViewModel(IAppConfiguration appConfiguration, IDispatcher dispatcher)
        {
            _regex = new Regex(@"\[(\w)+\]", RegexOptions.Compiled);
            _appConfiguration = appConfiguration;
            _dispatcher = dispatcher;
            _selectedCollection = PostedVideos;
        }

        private ObservableRangeCollection<VideoUI> PostedVideos { get; } = new ObservableRangeCollection<VideoUI>();
        private ObservableRangeCollection<VideoUI> LikedVideos { get; } = new ObservableRangeCollection<VideoUI>();
        private ObservableRangeCollection<VideoUI> BookmarkedVideos { get; } = new ObservableRangeCollection<VideoUI>();

        private IEnumerable<VideoUI> _selectedCollection;

        public ObservableRangeCollection<VideoUI> Videos { get; } = new ObservableRangeCollection<VideoUI>();


        private VideoUI selectedVideo;
        public VideoUI SelectedVideo
        {
            get { return selectedVideo; }
            set { SetProperty(ref selectedVideo, value); }
        }


        private string userUniqueId;
        public string UserUniqueId
        {
            get { return userUniqueId; }
            set { SetProperty(ref userUniqueId, value); }
        }



        private string query;
        public string Query
        {
            get { return query; }
            set { SetProperty(ref query, value); }
        }

        public ObservableRangeCollection<string> SelectedCategories { get; } = new ObservableRangeCollection<string>();

        public DownloadType SelectedVideoType = DownloadType.Posted;
        
        public async Task ShowLikedVideos()
        {
            Videos.Clear();
            SelectedVideoType = DownloadType.Favorite;
            if (LikedVideos.Count == 0)
            {
                var items = await GetProfileVideos(FeedType.Liked);
                _dispatcher.Run(() => LikedVideos.ReplaceRange(items));
            }
            _selectedCollection = LikedVideos;
            await RefreshVideos();
        }
        
        public async Task ShowPostedVideos()
        {
            Videos.Clear();
            SelectedVideoType = DownloadType.Posted;
            if (PostedVideos.Count == 0)
            {
                var items = await GetProfileVideos(FeedType.Posted);
                _dispatcher.Run(() => PostedVideos.ReplaceRange(items));
            }
            _selectedCollection = PostedVideos;
            await RefreshVideos();
        }

        public async Task ShowBookmarkedVideos()
        {
            Videos.Clear();
            SelectedVideoType = DownloadType.Bookmarks;
            if (BookmarkedVideos.Count == 0)
            {
                var items = await GetBookmarkedVideos();
                BookmarkedVideos.ReplaceRange(items);
            }
            _selectedCollection = BookmarkedVideos;
            await RefreshVideos();
        }

        public async Task RefreshVideos()
        {
            _dispatcher.Run(() =>
            {
                try
                {
                    Videos.ReplaceRange(FilterVideos());
                }
                catch (Exception ex)
                {
                }
            });
        }


        private async Task<List<VideoUI>> GetProfileVideos(FeedType feedType)
        {
            var videos = new List<VideoUI>();

            var db = new TikTokDbContext();

            string folderPath = Path.Combine(_appConfiguration.DownloadsFolder, UserUniqueId, SelectedVideoType.ToString());
            if (!Directory.Exists(folderPath)) return videos;

            var files = Directory.EnumerateFiles(folderPath);
            var profileVideos = await db.ProfileVideos
                .Where(x => x.FeedType == feedType && x.UserUniqueId == UserUniqueId)
                .Join(db.Videos.Include(x => x.Author).Include(x=>x.Categories), p => p.VideoId, v => v.VideoId, (p, v) => new { Index = p.Index, Video = v })
                .OrderBy(x => x.Index).AsNoTracking().ToListAsync();

            var idToPath = new Dictionary<string, string>();

            foreach (var path in files)
            {
                var id = GetId(path);
                idToPath.TryAdd(id, path);
            }

            videos = profileVideos.Select(item => new VideoUI
            {
                Video = item.Video,
                FilePath = idToPath.GetValueOrDefault(item.Video.VideoId, ""),
                Categories = item.Video.Categories.Select(x=>x.Name).ToList()
            }).ToList();
            return videos;
        }

        private async Task<List<VideoUI>> GetBookmarkedVideos()
        {
            var db = new TikTokDbContext();
            string folderPath = Path.Combine(_appConfiguration.DownloadsFolder, UserUniqueId, SelectedVideoType.ToString());
            if (!Directory.Exists(folderPath)) return new List<VideoUI>();

            var files = Directory.EnumerateFiles(folderPath);

            var idToPath = new Dictionary<string, string>();
            foreach (var path in files)
            {
                var id = GetId(path);
                idToPath.TryAdd(id, path);
            }

            var videos = await db.Videos
                .Where(x => idToPath.Keys.ToList().Contains(x.VideoId))
                .AsNoTracking().ToListAsync();

            return videos.Select(x => new VideoUI
            {
                Video = x,
                FilePath = idToPath.GetValueOrDefault(x.VideoId, "")
            }).ToList();
        }

        private string GetId(string path)
        {
            return _regex.Matches(path).LastOrDefault()?.Value?.TrimStart('[')?.TrimEnd(']') ?? "";
        }

        private IEnumerable<VideoUI> FilterVideos()
        {
            var filteredByCategories = _selectedCollection;
            if (SelectedCategories.Count > 0)
            {
                filteredByCategories = _selectedCollection
                    .Where(x => x.Categories.Any(c => SelectedCategories.Contains(c)));
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return filteredByCategories;
            }
            else
            {
                return filteredByCategories.Where(x =>
                    (x.Video.Description?.ToLowerInvariant()?.Contains(query.ToLowerInvariant()) ?? false) ||
                    (x.Video.Author?.Nickname?.ToLowerInvariant()?.Contains(query.ToLowerInvariant()) ?? false) ||
                    (x.Video.Author?.Signature?.ToLowerInvariant()?.Contains(query.ToLowerInvariant()) ?? false) ||
                    (x.Video.Hashtags?.Any(h => h.Name.ToLowerInvariant().Contains(query.ToLowerInvariant())) ?? false));
            }
        }
    }
}
