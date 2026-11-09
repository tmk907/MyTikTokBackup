namespace MyTikTokBackup.Core.Services
{
    public interface IAppConfiguration
    {
        string DownloadsFolder { get; set; }
        bool AiLabelsEnabled { get; set; }
        bool ShowAiLabels { get; set; }

        string Categories { get; }
        string Videos { get; }
        string Metadata { get; }
        string AppLocalFolder { get; }
    }
}
