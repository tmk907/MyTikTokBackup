using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTikTokBackup.Core.Services
{
    public interface IStorageService
    {
        Task OpenFile(string filePath);
        Task OpenFolder(string folderPath);
        Task<string> PickFile(IEnumerable<string> fileTypes);
        Task<string> PickFolder();
    }
}
