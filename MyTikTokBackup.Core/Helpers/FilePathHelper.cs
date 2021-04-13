using System.IO;

namespace MyTikTokBackup.Core.Helpers
{
    public static class FilePathHelper
    {
		public  static string RemoveForbiddenChars(string videoName)
		{
			foreach (var ch in Path.GetInvalidPathChars())
			{
				videoName = videoName.Replace(ch, ' ');
			}
			foreach (var ch in Path.GetInvalidFileNameChars())
			{
				videoName = videoName.Replace(ch, ' ');
			}

			return videoName;
		}
	}
}
