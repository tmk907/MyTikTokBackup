using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyTikTokBackup.Core.Services;
using MyTikTokBackup.WindowsUWP.Helpers;
using Windows.Storage;
using Windows.System;

namespace MyTikTokBackup.WindowsUWP.Services
{
    public class PickerService : IPickerService
    {
        public async Task<string> PickFile(IEnumerable<string> fileTypes)
        {
            var file = await FilePickerHelper.PickFile(fileTypes);
            return file?.Path;
        }

        public async Task<string> PickFolder()
        {
            var folder = await FilePickerHelper.PickFolder();
            return folder?.Path;
        }

        public async Task LauchFolder(string folderPath)
        {
            await Launcher.LaunchFolderPathAsync(folderPath);
        }

        public async Task LauchFile(string filePath)
        {
            var file = await StorageFile.GetFileFromPathAsync(filePath);
            await Launcher.LaunchFileAsync(file);
        }
    }
}
