using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MyTikTokBackup.Core.Services;
using Serilog;

namespace MyTikTokBackup.Core.AiLabels
{
    public class AiModelManager
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };

        public AiModelManager(IAppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;
        }

        public string ModelsFolder => Path.Combine(_appConfiguration.DownloadsFolder, "Cache", "Models");

        public string GetPath(string fileName) => Path.Combine(ModelsFolder, fileName);

        public long TotalSize => AiModelCatalog.Files.Sum(x => x.Size);

        public bool AreModelsPresent()
        {
            return AiModelCatalog.Files.All(IsPresent);
        }

        private bool IsPresent(AiModelFile file)
        {
            var info = new FileInfo(GetPath(file.FileName));
            return info.Exists && info.Length == file.Size;
        }

        /// <summary>Downloads missing model files. Progress reports (downloaded bytes, total bytes).</summary>
        public async Task EnsureModelsAsync(IProgress<(long Done, long Total)> progress, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(ModelsFolder);
            var total = TotalSize;
            long done = AiModelCatalog.Files.Where(IsPresent).Sum(x => x.Size);
            progress?.Report((done, total));

            foreach (var file in AiModelCatalog.Files.Where(x => !IsPresent(x)))
            {
                Log.Information("Downloading AI model {File} from {Url}", file.FileName, file.Url);
                var targetPath = GetPath(file.FileName);
                var tempPath = targetPath + ".download";
                var startDone = done;

                using (var response = await _client.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    using var target = File.Create(tempPath);
                    var buffer = new byte[1 << 16];
                    int read;
                    var lastReport = DateTime.MinValue;
                    while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                        done += read;
                        if (DateTime.Now - lastReport > TimeSpan.FromMilliseconds(250))
                        {
                            lastReport = DateTime.Now;
                            progress?.Report((done, total));
                        }
                    }
                }

                var hash = await ComputeSha256(tempPath, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(hash, file.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(tempPath);
                    throw new InvalidDataException($"AI model {file.FileName} has invalid checksum {hash}");
                }

                File.Move(tempPath, targetPath, overwrite: true);
                done = startDone + file.Size;
                progress?.Report((done, total));
            }
        }

        public void DeleteModels()
        {
            if (Directory.Exists(ModelsFolder))
            {
                Directory.Delete(ModelsFolder, recursive: true);
            }
        }

        private static async Task<string> ComputeSha256(string path, CancellationToken cancellationToken)
        {
            using var stream = File.OpenRead(path);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            return Convert.ToHexString(hash);
        }
    }
}
