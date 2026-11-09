using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MyTikTokBackup.Core.AiLabels;
using MyTikTokBackup.Core.Messages;
using MyTikTokBackup.Core.Services;
using MyTikTokBackup.Desktop.Services;
using Serilog;
using Windows.ApplicationModel;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILocalVideosService _localVideosService;
        private readonly IAiLabelingService _aiLabelingService;
        private readonly AiModelManager _aiModelManager;
        private readonly ICategoriesService _categoriesService;
        private readonly IDispatcher _dispatcher;

        public SettingsViewModel(IAppConfiguration appConfiguration,
            ILocalVideosService localVideosService,
            IAiLabelingService aiLabelingService,
            AiModelManager aiModelManager,
            ICategoriesService categoriesService,
            IDispatcher dispatcher)
        {
            _appConfiguration = appConfiguration;
            _localVideosService = localVideosService;
            _aiLabelingService = aiLabelingService;
            _aiModelManager = aiModelManager;
            _categoriesService = categoriesService;
            _dispatcher = dispatcher;
            DownloadsFolderPath = appConfiguration.DownloadsFolder;
            ChooseDownloadsFolderCommand = new AsyncRelayCommand(ChooseDownloadsFolder);
            RecomputeAiLabelsCommand = new AsyncRelayCommand(() => _aiLabelingService.RecomputeAllAsync());
            CancelAiLabelingCommand = new RelayCommand(() => _aiLabelingService.Cancel());
            ClearRejectedAiLabelsCommand = new AsyncRelayCommand(ClearRejectedAiLabels);
            DeleteAiModelsCommand = new RelayCommand(DeleteAiModels);
            UpdateAiStatus(_aiLabelingService.Status);
        }


        private string downloadsFolderPath;
        public string DownloadsFolderPath
        {
            get { return downloadsFolderPath; }
            set { SetProperty(ref downloadsFolderPath, value); }
        }


        public IAsyncRelayCommand ChooseDownloadsFolderCommand { get; }

        private async Task ChooseDownloadsFolder()
        {
            var folder = await FilePickerHelper.PickFolder();
            if (folder != null)
            {
                _appConfiguration.DownloadsFolder = folder.Path;
                DownloadsFolderPath = _appConfiguration.DownloadsFolder;
                _localVideosService.Refresh();
            }
        }

        public bool AiLabelsEnabled
        {
            get => _appConfiguration.AiLabelsEnabled;
            set
            {
                if (_appConfiguration.AiLabelsEnabled == value) return;
                if (value) _aiLabelingService.Enable(); else _aiLabelingService.Disable();
                OnPropertyChanged();
            }
        }

        public string AiModelsSize => $"{_aiModelManager.TotalSize / 1048576} MB";

        private string aiStatusText;
        public string AiStatusText
        {
            get { return aiStatusText; }
            set { SetProperty(ref aiStatusText, value); }
        }

        private bool isAiLabelingRunning;
        public bool IsAiLabelingRunning
        {
            get { return isAiLabelingRunning; }
            set
            {
                if (SetProperty(ref isAiLabelingRunning, value)) OnPropertyChanged(nameof(IsAiLabelingIdle));
            }
        }

        public bool IsAiLabelingIdle => !isAiLabelingRunning;

        private bool isAiProgressIndeterminate;
        public bool IsAiProgressIndeterminate
        {
            get { return isAiProgressIndeterminate; }
            set { SetProperty(ref isAiProgressIndeterminate, value); }
        }

        private double aiProgress;
        public double AiProgress
        {
            get { return aiProgress; }
            set { SetProperty(ref aiProgress, value); }
        }

        public IAsyncRelayCommand RecomputeAiLabelsCommand { get; }
        public IRelayCommand CancelAiLabelingCommand { get; }
        public IAsyncRelayCommand ClearRejectedAiLabelsCommand { get; }
        public IRelayCommand DeleteAiModelsCommand { get; }

        public void Activate()
        {
            UpdateAiStatus(_aiLabelingService.Status);
            if (StrongReferenceMessenger.Default.IsRegistered<AiLabelingStatusChanged>(this)) return;
            StrongReferenceMessenger.Default.Register<SettingsViewModel, AiLabelingStatusChanged>(this,
                (r, m) => r._dispatcher.Run(() => r.UpdateAiStatus(m.Status)));
        }

        public void Deactivate()
        {
            StrongReferenceMessenger.Default.UnregisterAll(this);
        }

        private void UpdateAiStatus(AiLabelingStatus status)
        {
            IsAiLabelingRunning = status.IsRunning;
            IsAiProgressIndeterminate = status.Total <= 0;
            AiProgress = status.Total > 0 ? 100.0 * status.Done / status.Total : 0;
            AiStatusText = status.State switch
            {
                AiLabelingState.Disabled => _aiModelManager.AreModelsPresent() ? "Disabled (models downloaded)" : "Disabled",
                AiLabelingState.Error => $"Error: {status.Message}",
                _ => status.Message,
            };
        }

        private async Task ClearRejectedAiLabels()
        {
            await _categoriesService.ClearRejectedAiLabels();
        }

        private void DeleteAiModels()
        {
            try
            {
                _aiModelManager.DeleteModels();
                UpdateAiStatus(_aiLabelingService.Status);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete AI models");
                AiStatusText = $"Can't delete models: {ex.Message}";
            }
        }

        public string AppName => Package.Current.DisplayName;

        public string AppVersion => GetAppVersion();

        private string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }
    }
}
