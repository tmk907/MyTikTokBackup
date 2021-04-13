using System.IO;
using Windows.Storage;

namespace MyTikTokBackup.WindowsUWP.Helpers
{
    public static class SettingsFiles
    {
        public static string Categories => Path.Combine(ApplicationData.Current.LocalFolder.Path, "categories.json");
        public static string Videos => Path.Combine(ApplicationData.Current.LocalFolder.Path, "local.json");
        public static string Metadata => Path.Combine(ApplicationData.Current.LocalFolder.Path, "db.json");

        public static string LogsFile => Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs.txt");
    }
}
