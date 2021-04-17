using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT;

namespace MyTikTokBackup.WindowsUWP.Helpers
{
    public class FilePickerHelper
    {
        public static void Init(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
        }

        private static IntPtr _windowHandle;

        public static async Task<StorageFile> PickFile(IEnumerable<string> fileTypes)
        {
            try
            {
                var picker = new FileOpenPicker();
                foreach (var fileType in fileTypes)
                {
                    picker.FileTypeFilter.Add(fileType);
                }

                IntPtr windowHandle = _windowHandle;
                var initializeWithWindow = picker.As<IInitializeWithWindow>();
                initializeWithWindow.Initialize(windowHandle);

                return await picker.PickSingleFileAsync();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static async Task<StorageFolder> PickFolder()
        {
            try
            {
                var picker = new FolderPicker();
                picker.FileTypeFilter.Add("*");
                picker.SuggestedStartLocation = PickerLocationId.Downloads;

                IntPtr windowHandle = _windowHandle;
                var initializeWithWindow = picker.As<IInitializeWithWindow>();
                initializeWithWindow.Initialize(windowHandle);

                return await picker.PickSingleFolderAsync();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [ComImport, Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IInitializeWithWindow
        {
            void Initialize([In] IntPtr hwnd);
        }
    }
}
