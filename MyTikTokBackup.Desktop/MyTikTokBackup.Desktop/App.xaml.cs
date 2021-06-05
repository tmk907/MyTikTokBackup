using System;
using System.Runtime.InteropServices;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using MyTikTokBackup.Core.Services;
using MyTikTokBackup.Core.Helpers;
using MyTikTokBackup.Core.Repositories;
using MyTikTokBackup.Desktop.Services;
using MyTikTokBackup.Desktop.ViewModels;
using MyTikTokBackup.Desktop.Views;
using Windows.ApplicationModel;
using WinRT;
using MyTikTokBackup.WindowsUWP.Services;
using MyTikTokBackup.WindowsUWP.Helpers;
using Serilog;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyTikTokBackup.Desktop
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            Ioc.Default.ConfigureServices(ConfigureServices());
            var nav = Ioc.Default.GetService<INavigationService>();
            nav.Register<MainPage, MainViewModel>();
            nav.Register<DownloadsPage, DownloadsViewModel>();
            nav.Register<SettingsPage, SettingsViewModel>();
            nav.Register<TikTokBrowserPage, TikTokBrowserViewModel>();
            nav.Register<FoldersPage, FoldersViewModel>();
            nav.Register<ProfileVideosPage, ProfileVideosViewModel>();
         
            var helper = new Core.Database.DatabaseHelper();
            helper.Ensure();

            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File(SettingsFiles.LogsFile, rollingInterval: RollingInterval.Day)
               .WriteTo.Debug()
               .CreateLogger();
        }

        static private bool _init = false;

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            if (!_init)
            {
                _init = true;
                m_window = new MainWindow();
                var windowNative = m_window.As<IWindowNative>();
                m_windowHandle = windowNative.WindowHandle;
                FilePickerHelper.Init(m_windowHandle);
                m_window.Activate();
            }
        }

        private System.IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddTransient<ICategoryRepository, CategoryRepository>();
            services.AddTransient<IImportService, ImportService>();
            services.AddSingleton<IDownloadsManager, DownloadsManager>();
            services.AddSingleton<ICategoriesService, CategoriesService>();
            services.AddSingleton<ILocalVideosService, LocalVideosService>();
            services.AddSingleton<IAppConfiguration, AppConfiguration>();

            // Services
            services.AddTransient<MainViewModel>();
            services.AddTransient<DownloadsViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddSingleton<TikTokBrowserViewModel>();
            services.AddTransient<FoldersViewModel>();
            services.AddTransient<ProfileVideosViewModel>();
            services.AddTransient<IDispatcher, DispatcherHelper>();
            services.AddTransient<IStorageService, StorageService>();

            services.AddSingleton<INavigationService, NavigationService2>();
            services.AddSingleton(ConfigureMapper());

            return services.BuildServiceProvider();
        }

        private Mapper ConfigureMapper()
        {
            var config = new MapperConfiguration(cfg => {
                //cfg.CreateMap<Core.TikTok.Video, Video>()
                //    .ForMember(x => x.Duration, opt => opt.MapFrom(src => TimeSpan.FromSeconds(src.Duration)));
                //cfg.CreateMap<Core.TikTok.ItemInfo, FullMetadata>()
                //    .ForMember(x => x.CreateTime,
                //        opt => opt.MapFrom(src => DateTimeExtensions.UnixTimeStampToDateTime(src.CreateTime)))
                //    .ForMember(x => x.Description, opt => opt.MapFrom(src => src.Desc));
            });
            return new Mapper(config);
        }

        private Window m_window;
        private IntPtr m_windowHandle;
        public IntPtr WindowHandle { get { return m_windowHandle; } }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }
    }
}
