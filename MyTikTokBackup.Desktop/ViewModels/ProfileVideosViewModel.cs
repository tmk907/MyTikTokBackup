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
using MyTikTokBackup.Desktop.Controls;
using Serilog;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class VideoUI
    {
        public Core.Database.Video Video { get; init; }
        /// <summary>Category names; the same set instance is shared by every VideoUI of a given video.</summary>
        public HashSet<string> Categories { get; init; }
        public string FilePath { get; init; }
        public string Url => $"https://www.tiktok.com/@{Video.Author.UniqueId}/video/{Video.VideoId}";
    }

    public class ProfileVideosViewModel : ObservableObject
    {
        private readonly Regex _regex;
        private readonly IAppConfiguration _appConfiguration;
        private readonly IDispatcher _dispatcher;
        private readonly ICategoriesService _categoriesService;
        private readonly Dictionary<int, HashSet<string>> _videoCategories = new();
        private bool _areCategoriesLoaded;

        public ProfileVideosViewModel(IAppConfiguration appConfiguration, IDispatcher dispatcher, ICategoriesService categoriesService)
        {
            _categoriesService = categoriesService;
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
            set
            {
                if (SetProperty(ref selectedVideo, value))
                {
                    OnPropertyChanged(nameof(HasSelectedVideo));
                    SyncAssignedCategories();
                }
            }
        }

        public bool HasSelectedVideo => selectedVideo != null;


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

        /// <summary>All categories, sorted by name. Used by both the filter and the video assignment controls.</summary>
        public ObservableCollection<CategoryItem> Categories { get; } = new ObservableCollection<CategoryItem>();

        public DownloadType SelectedVideoType = DownloadType.Posted;
        
        public async Task ShowLikedVideos()
        {
            await EnsureCategoriesLoadedAsync();
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
            await EnsureCategoriesLoadedAsync();
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
            await EnsureCategoriesLoadedAsync();
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


        private async Task EnsureCategoriesLoadedAsync()
        {
            if (_areCategoriesLoaded) return;
            _areCategoriesLoaded = true;
            try
            {
                var categories = await _categoriesService.GetAll();
                foreach (var category in categories.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    Categories.Add(new CategoryItem { Name = category.Name, Color = category.Color });
                }

                foreach (var (videoId, names) in await _categoriesService.GetVideoCategories())
                {
                    GetCategorySet(videoId).UnionWith(names);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load categories");
            }
        }

        private HashSet<string> GetCategorySet(int videoId)
        {
            if (!_videoCategories.TryGetValue(videoId, out var set))
            {
                set = new HashSet<string>();
                _videoCategories[videoId] = set;
            }
            return set;
        }

        private void SyncAssignedCategories()
        {
            var assigned = selectedVideo?.Categories;
            foreach (var category in Categories)
            {
                category.IsAssigned = assigned?.Contains(category.Name) == true;
            }
        }

        private void InsertSorted(CategoryItem category)
        {
            var index = Categories.TakeWhile(x => string.Compare(x.Name, category.Name, StringComparison.CurrentCultureIgnoreCase) < 0).Count();
            Categories.Insert(index, category);
        }

        public async Task AddCategoryAsync(string name)
        {
            try
            {
                var created = await _categoriesService.CreateCategory(name);
                InsertSorted(new CategoryItem { Name = created.Name, Color = created.Color });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create category {Name}", name);
            }
        }

        public async Task RenameCategoryAsync(CategoryItem category, string newName)
        {
            var oldName = category.Name;
            try
            {
                await _categoriesService.Rename(oldName, newName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to rename category {OldName} to {NewName}", oldName, newName);
                return;
            }

            foreach (var set in _videoCategories.Values.Where(x => x.Remove(oldName)))
            {
                set.Add(newName);
            }
            category.Name = newName;
            Categories.Remove(category);
            InsertSorted(category);
        }

        public async Task DeleteCategoryAsync(CategoryItem category)
        {
            try
            {
                await _categoriesService.Delete(category.Name);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete category {Name}", category.Name);
                return;
            }

            foreach (var set in _videoCategories.Values)
            {
                set.Remove(category.Name);
            }
            Categories.Remove(category);

            if (category.IsFilterSelected)
            {
                await RefreshVideos();
            }
        }

        public async Task SetVideoCategoryAsync(CategoryItem category, bool isAssigned)
        {
            var video = selectedVideo;
            if (video == null)
            {
                category.IsAssigned = false;
                return;
            }

            try
            {
                if (isAssigned)
                {
                    await _categoriesService.AddVideoToCategory(category.Name, video.Video.Id);
                    video.Categories.Add(category.Name);
                }
                else
                {
                    await _categoriesService.RemoveVideoFromCategory(category.Name, video.Video.Id);
                    video.Categories.Remove(category.Name);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update category {Name} of video {VideoId}", category.Name, video.Video.VideoId);
            }

            if (video == selectedVideo)
            {
                category.IsAssigned = video.Categories.Contains(category.Name);
            }
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
                .Join(db.Videos.Include(x => x.Author), p => p.VideoId, v => v.VideoId, (p, v) => new { Index = p.Index, Video = v })
                .OrderBy(x => x.Index).AsNoTracking().ToListAsync();

            var idToPath = new Dictionary<string, string>();

            foreach (var path in files)
            {
                var id = GetId(path);
                idToPath.TryAdd(id, path);
            }

            foreach(var item in profileVideos.Where(x=> x.Video.Author == null))
            {
                item.Video.Author = new Author
                {
                    Id = "",
                    Nickname = "",
                    Signature = "",
                    Stats = new AuthorStats(),
                    UniqueId = ""
                };
            }

            foreach (var item in profileVideos.Where(x => x.Video.Music == null))
            {
                item.Video.Music = new Music
                {
                    Album = "",
                    AuthorName = "",
                    Duration = TimeSpan.Zero,
                    Id = "",
                    Title = ""
                };
            }

            videos = profileVideos.Select(item => new VideoUI
            {
                Video = item.Video,
                FilePath = idToPath.GetValueOrDefault(item.Video.VideoId, ""),
                Categories = GetCategorySet(item.Video.Id)
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
                FilePath = idToPath.GetValueOrDefault(x.VideoId, ""),
                Categories = GetCategorySet(x.Id)
            }).ToList();
        }

        private string GetId(string path)
        {
            return _regex.Matches(path).LastOrDefault()?.Value?.TrimStart('[')?.TrimEnd(']') ?? "";
        }

        private IEnumerable<VideoUI> FilterVideos()
        {
            var filteredByCategories = _selectedCollection;
            var selectedCategories = Categories.Where(x => x.IsFilterSelected).Select(x => x.Name).ToList();
            if (selectedCategories.Count > 0)
            {
                filteredByCategories = _selectedCollection
                    .Where(x => selectedCategories.Any(x.Categories.Contains));
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
