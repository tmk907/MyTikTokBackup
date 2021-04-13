using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using MvvmHelpers;
using MyTikTokBackup.Core.Messages;
using MyTikTokBackup.Core.Models;
using MyTikTokBackup.Core.Repositories;
using MyTikTokBackup.Core.Services;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class LibraryViewModel : ObservableObject
    {
        private readonly ICategoriesService _categoriesService;
        private readonly ILocalVideosService _localVideosService;
        private readonly IUserVideosRepository _userVideosRepository;
        private readonly IMetadataRepository _metadataRepository;

        public LibraryViewModel(ICategoriesService categoriesService,
            ILocalVideosService localVideosService,
            IUserVideosRepository userVideosRepository,
            IMetadataRepository metadataRepository)
        {
            _categoriesService = categoriesService;
            _localVideosService = localVideosService;
            _userVideosRepository = userVideosRepository;
            _metadataRepository = metadataRepository;

            FilterVideosCommand = new RelayCommand(FilterVideos);
            SaveVideoCategoriesCommand = new AsyncRelayCommand(SaveVideoCategories);
            AddCategoryCommand = new AsyncRelayCommand(AddCategory);

            MediaPlayer = new MediaPlayer();
            //MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
            //MediaPlayer.AutoPlay = true;
            //MediaPlayer.IsLoopingEnabled = true;
        }

        private Dictionary<string,FullMetadata> _metadata;
        private IEnumerable<UserVideo> _userVideos;
        private List<Category> _categories;

        public async Task Init()
        {
            _metadata = (await _metadataRepository.GetAll()).ToDictionary(x => x.Id);
            _userVideos = await _userVideosRepository.GetAll();
            _categories = (await _categoriesService.GetAll()).ToList();

            allVideos = _userVideos.Select(x => new TikTokVideo
            {
                FilePath = _localVideosService.GetPath(x.VideoId),
                Id = x.VideoId,
                Title = _metadata[x.VideoId].Description,
                SelectedCategories = new ObservableRangeCollection<Category>(x.Categories),
                Metadata = _metadata[x.VideoId]
            });
            Videos.ReplaceRange(allVideos);

            UpdateCategories(_categories);

            StrongReferenceMessenger.Default.Register<LibraryViewModel, FilterByCategoryChangeMessage>(this, (r, m) =>
            {
                r.FilterVideos();
            });
        }

        public async Task UnInit()
        {
            StrongReferenceMessenger.Default.Unregister<FilterByCategoryChangeMessage>(this);
        }

        private IEnumerable<TikTokVideo> allVideos;
        public ObservableRangeCollection<TikTokVideo> Videos { get; } = new ObservableRangeCollection<TikTokVideo>();

        private TikTokVideo selectedVideo;
        public TikTokVideo SelectedVideo
        {
            get { return selectedVideo; }
            set { SetProperty(ref selectedVideo, value); }
        }

        public ObservableRangeCollection<CategoryFilter> Categories { get; } = new ObservableRangeCollection<CategoryFilter>();

        public void ChangeVideo(TikTokVideo video)
        {
            SelectedVideo = video;

            //var file = await StorageFile.GetFileFromPathAsync(video.FilePath);
            //var mediaSource = MediaSource.CreateFromStorageFile(file);
            //MediaPlayer.Source = mediaSource;
        }

        public MediaPlayer MediaPlayer { get; }

        #region Filter videos
        public IRelayCommand FilterVideosCommand { get; }

        private string query;
        public string Query
        {
            get { return query; }
            set { SetProperty(ref query, value); }
        }

        private void FilterVideos()
        {
            if (string.IsNullOrWhiteSpace(Query))
            {
                Videos.ReplaceRange(allVideos);
            }
            var query = (Query ?? "").Trim().ToLowerInvariant();
            var filtered = FilterByCategories();
            filtered = filtered
                .Where(x => x.Metadata.Description.ToLowerInvariant().Contains(query));
            
            Videos.ReplaceRange(filtered);
        }

        private IEnumerable<TikTokVideo> FilterByCategories()
        {
            var selectedCategoryNames = Categories.Where(c => c.IsSelected).Select(x => x.Name);
            if (selectedCategoryNames.Count() == 0 ||
                selectedCategoryNames.Count() == Categories.Count)
            {
                return allVideos;
            }
            else
            {
                return allVideos.Where(x => x.SelectedCategories
                    .Select(x => x.Name).Any(x => selectedCategoryNames.Contains(x))); 
            }
        }

        #endregion

        #region Select video category
        public ObservableRangeCollection<CategorySelection> SelectedVideoCategories { get; } = new ObservableRangeCollection<CategorySelection>();
        public AsyncRelayCommand SaveVideoCategoriesCommand { get; }
        private TikTokVideo _selectedVideo;

        public void PrepareVideoCategories(TikTokVideo video)
        {
            _selectedVideo = video;
            foreach(var category in SelectedVideoCategories)
            {
                category.IsSelected = video.SelectedCategories.Any(x => x.Name == category.Name);
            }
        }

        private async Task SaveVideoCategories()
        {
            var selected = SelectedVideoCategories.Where(x => x.IsSelected);
            _selectedVideo.SelectedCategories.ReplaceRange(selected.Select(x => new Category
            {
                Color = x.Color,
                Name = x.Name
            }));
            var all = await _userVideosRepository.GetAll();
            all.Single(x => x.VideoId == _selectedVideo.Id).Categories = _selectedVideo.SelectedCategories.ToList();
            await _userVideosRepository.Replace(all);
        }
        #endregion

        #region Add category
        private string newCategoryName;
        public string NewCategoryName
        {
            get { return newCategoryName; }
            set { SetProperty(ref newCategoryName, value); }
        }

        public IAsyncRelayCommand AddCategoryCommand { get; }

        private async Task AddCategory()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName)) return;
            var category = await _categoriesService.CreateCategory(newCategoryName);
            _categories.Add(category);
            UpdateCategories(_categories);
            NewCategoryName = "";
        }

        private void UpdateCategories(IEnumerable<Category> newCategories)
        {
            Categories.ReplaceRange(newCategories.OrderBy(x => x.Name)
                .Select(x => new CategoryFilter() { Color = x.Color, Name = x.Name }));
            SelectedVideoCategories.ReplaceRange(newCategories.OrderBy(x => x.Name)
                .Select(x => new CategorySelection(x.Name, x.Color)));
        }
        #endregion
    }

    public class CategorySelection: ObservableObject
    {
        public CategorySelection(string name, string color)
        {
            Name = name;
            Color = color;
            IsSelected = true;
        }

        public CategorySelection()
        {
        }

        protected bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set { SetProperty(ref isSelected, value); }
        }

        public string Name { get; set; }
        public string Color { get; set; }
    }

    public class CategoryFilter: CategorySelection
    {
        new public bool IsSelected
        {
            get { return isSelected; }
            set 
            {
                SetProperty(ref isSelected, value);
                StrongReferenceMessenger.Default.Send(new FilterByCategoryChangeMessage()); 
            }
        }
    }
}
