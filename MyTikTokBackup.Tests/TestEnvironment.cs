using System;
using System.IO;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Data.Sqlite;
using MyTikTokBackup.Core.Services;
using NUnit.Framework;

namespace MyTikTokBackup.Tests
{
    public class TestAppConfiguration : IAppConfiguration
    {
        public string DownloadsFolder { get; set; }
        public bool AiLabelsEnabled { get; set; }
        public bool ShowAiLabels { get; set; } = true;
        public string Categories => "";
        public string Videos => "";
        public string Metadata => "";
        public string AppLocalFolder => DownloadsFolder;
    }

    /// <summary>
    /// TikTokDbContext resolves the database folder from Ioc, which can be configured only once per process.
    /// </summary>
    [SetUpFixture]
    public class TestEnvironment
    {
        public static TestAppConfiguration Configuration { get; } = new TestAppConfiguration
        {
            DownloadsFolder = Path.Combine(Path.GetTempPath(), "MyTikTokBackupTests", Guid.NewGuid().ToString("N")),
        };

        private sealed class ServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType) => serviceType == typeof(IAppConfiguration) ? Configuration : null;
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(Configuration.DownloadsFolder);
            Ioc.Default.ConfigureServices(new ServiceProvider());
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            try
            {
                Directory.Delete(Configuration.DownloadsFolder, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}
