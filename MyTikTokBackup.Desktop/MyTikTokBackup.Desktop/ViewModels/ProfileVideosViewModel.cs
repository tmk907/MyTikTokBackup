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

        public ProfileVideosViewModel(IAppConfiguration appConfiguration)
        {
            _regex = new Regex(@"\[(\w)+\]", RegexOptions.Compiled);
            _appConfiguration = appConfiguration;
        }

        public ObservableRangeCollection<VideoUI> PostedVideos { get; } = new ObservableRangeCollection<VideoUI>();
        public ObservableRangeCollection<VideoUI> LikedVideos { get; } = new ObservableRangeCollection<VideoUI>();


        private string userUniqueId;
        public string UserUniqueId
        {
            get { return userUniqueId; }
            set { SetProperty(ref userUniqueId, value); }
        }


        public async Task LoadAllVideos()
        {
            await LoadPostedVideos();
            await LoadLikedVideos();
        }

        public async Task LoadLikedVideos()
        {
            var db = new TikTokDbContext();

            string folderPath = Path.Combine(_appConfiguration.DownloadsFolder, UserUniqueId, "Favorite");
            if (!Directory.Exists(folderPath)) return;

            var likedFiles = Directory.EnumerateFiles(folderPath);
            var profileLikedVideos = await db.ProfileVideos
                .Where(x => x.FeedType == FeedType.Liked && x.UserUniqueId == UserUniqueId)
                .Join(db.Videos, p => p.VideoId, v => v.VideoId, (p, v) => new { Index = p.Index, Video = v })
                .OrderBy(x => x.Index).AsNoTracking().ToListAsync();

            var likedIdToPath = new Dictionary<string, string>();

            foreach (var path in likedFiles)
            {
                var id = GetId(path);
                likedIdToPath.TryAdd(id, path);
            }

            foreach (var item in profileLikedVideos)
            {
                LikedVideos.Add(new VideoUI
                {
                    Video = item.Video,
                    FilePath = likedIdToPath.GetValueOrDefault(item.Video.VideoId, "")
                });
            }
        }

        public async Task LoadPostedVideos()
        {
            var db = new TikTokDbContext();
            string folderPath = Path.Combine(_appConfiguration.DownloadsFolder, UserUniqueId, "Posted");
            if (!Directory.Exists(folderPath)) return;

            var postedFiles = Directory.EnumerateFiles(folderPath);

            var profilePostedVideos = await db.ProfileVideos
                .Where(x => x.FeedType == FeedType.Posted && x.UserUniqueId == UserUniqueId)
                .Join(db.Videos, p => p.VideoId, v => v.VideoId, (p, v) => new { Index = p.Index, Video = v })
                .OrderBy(x => x.Index).AsNoTracking().ToListAsync();

            var postedIdToPath = new Dictionary<string, string>();

            foreach (var path in postedFiles)
            {
                var id = GetId(path);
                postedIdToPath.TryAdd(id, path);
            }
            
            foreach (var item in profilePostedVideos)
            {
                PostedVideos.Add(new VideoUI
                {
                    Video = item.Video,
                    FilePath = postedIdToPath.GetValueOrDefault(item.Video.VideoId, "")
                });
            }
        }

        private string GetId(string path)
        {
            return _regex.Matches(path).LastOrDefault()?.Value?.TrimStart('[')?.TrimEnd(']') ?? "";
        }

    }
}
