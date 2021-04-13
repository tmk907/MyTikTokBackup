using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTikTokBackup.Core.Services
{
    public interface IPickerService
    {
        Task LauchFile(string filePath);
        Task LauchFolder(string folderPath);
        Task<string> PickFile(IEnumerable<string> fileTypes);
        Task<string> PickFolder();
    }
}
