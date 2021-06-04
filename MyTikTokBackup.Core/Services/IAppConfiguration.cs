namespace MyTikTokBackup.Core.Services
{
    public interface IAppConfiguration
    {
        string DownloadsFolder { get; set; }

        string Categories { get; }
        string Videos { get; }
        string Metadata { get; }
        string AppLocalFolder { get; }
    }
}
