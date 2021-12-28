using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using MvvmHelpers;
using MyTikTokBackup.Core.Services;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class UserFolder
    {
        public string UniqueId { get; set; }
        public string FolderPath { get; set; }
    }

    public class FoldersViewModel : ObservableObject
    {
        public ObservableRangeCollection<UserFolder> Folders { get; } = new ObservableRangeCollection<UserFolder>();

        public async Task LoadAsync()
        {
            var downloadsFolder = Ioc.Default.GetService<IAppConfiguration>().DownloadsFolder;
            var folders = Directory.EnumerateDirectories(downloadsFolder)
                .Select(x => new UserFolder
                {
                    FolderPath = x,
                    UniqueId = Path.GetFileName(x)
                })
                .Where(x => x.UniqueId.StartsWith('@'))
                .OrderBy(x => x.UniqueId);
            Folders.ReplaceRange(folders);
        }
    }
}
