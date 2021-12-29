using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using MvvmHelpers;
using MyTikTokBackup.Core.Database;
using MyTikTokBackup.Core.Helpers;
using MyTikTokBackup.Core.Models;
using MyTikTokBackup.Core.Services;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class VideoUI
    {
        public Core.Database.Video Video { get; set; }
        public string FilePath { get; set; }
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
        }

        public ObservableRangeCollection<VideoUI> PostedVideos { get; } = new ObservableRangeCollection<VideoUI>();
        public ObservableRangeCollection<VideoUI> LikedVideos { get; } = new ObservableRangeCollection<VideoUI>();
        public ObservableRangeCollection<VideoUI> BookmarkedVideos { get; } = new ObservableRangeCollection<VideoUI>();

        public ObservableRangeCollection<VideoUI> Videos { get; } = new ObservableRangeCollection<VideoUI>();


        private string userUniqueId;
        public string UserUniqueId
        {
            get { return userUniqueId; }
            set { SetProperty(ref userUniqueId, value); }
        }


        public async Task LoadAllVideos()
        {
            await Task.Run(() => LoadLikedVideos());
            await Task.Run(() => LoadPostedVideos());
            await Task.Run(() => LoadBookmarkedVideos());
        }

        public DownloadType SelectedVideoType = DownloadType.Posted;

        public async Task LoadLikedVideos()
        {
            Videos.Clear();
            SelectedVideoType = DownloadType.Favorite;
            if (LikedVideos.Count == 0)
            {
                var items = await LoadProfileVideos(FeedType.Liked);
                _dispatcher.Run(() => LikedVideos.ReplaceRange(items));
            }

            _dispatcher.Run(() =>
            {
                Videos.ReplaceRange(LikedVideos);
            });
        }
        
        public async Task LoadPostedVideos()
        {
            Videos.Clear();
            SelectedVideoType = DownloadType.Posted;
            if (PostedVideos.Count == 0)
            {
                var items = await LoadProfileVideos(FeedType.Posted);
                _dispatcher.Run(() => PostedVideos.ReplaceRange(items));
            }

            _dispatcher.Run(() =>
            {
                Videos.ReplaceRange(PostedVideos);
            });
        }

        private async Task<List<VideoUI>> LoadProfileVideos(FeedType feedType)
        {
            var videos = new List<VideoUI>();

            var db = new TikTokDbContext();

            string folderPath = Path.Combine(_appConfiguration.DownloadsFolder, UserUniqueId, SelectedVideoType.ToString());
            if (!Directory.Exists(folderPath)) return videos;

            var files = Directory.EnumerateFiles(folderPath);
            var profileVideos = await db.ProfileVideos
                .Where(x => x.FeedType == feedType && x.UserUniqueId == UserUniqueId)
                .Join(db.Videos, p => p.VideoId, v => v.VideoId, (p, v) => new { Index = p.Index, Video = v })
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
                FilePath = idToPath.GetValueOrDefault(item.Video.VideoId, "")
            }).ToList();
            return videos;
        }

        public async Task LoadBookmarkedVideos()
        {
            Videos.Clear();
            SelectedVideoType = DownloadType.Bookmarks;
            if (BookmarkedVideos.Count == 0)
            {
                var db = new TikTokDbContext();
                string folderPath = Path.Combine(_appConfiguration.DownloadsFolder, UserUniqueId, SelectedVideoType.ToString());
                if (!Directory.Exists(folderPath)) return;

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

                _dispatcher.Run(() =>
                {
                    foreach (var item in videos)
                    {
                        BookmarkedVideos.Add(new VideoUI
                        {
                            Video = item,
                            FilePath = idToPath.GetValueOrDefault(item.VideoId, "")
                        });
                    }
                });
            }
            _dispatcher.Run(() =>
            {
                Videos.ReplaceRange(BookmarkedVideos);
            });
        }

        private string GetId(string path)
        {
            return _regex.Matches(path).LastOrDefault()?.Value?.TrimStart('[')?.TrimEnd(']') ?? "";
        }
    }
}
