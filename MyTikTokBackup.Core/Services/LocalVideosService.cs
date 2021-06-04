using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Toolkit.Mvvm.Messaging;
using Serilog;
using MyTikTokBackup.Core.Messages;

namespace MyTikTokBackup.Core.Services
{
    public interface ILocalVideosService
    {
        IEnumerable<string> GetFileNames();
        IEnumerable<string> GetFilePaths();
        string GetPath(string videoId);
        void Refresh();
    }

    public class LocalVideosService : ILocalVideosService
    {
        private readonly List<string> _filePaths;
        private readonly Regex _regex;
        private readonly IAppConfiguration _appConfiguration;
        private Dictionary<string, string> _idToPath;

        public LocalVideosService(IAppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;
            _regex = new Regex(@"\[(\w)+\]", RegexOptions.Compiled);
            _filePaths = new List<string>();

            Refresh();

            StrongReferenceMessenger.Default.Register<LocalVideosService, DownloadStatusChanged>(this, (r, m) =>
            {
                if (m.DownloadStatus == Models.DownloadStatus.Downloaded)
                {
                    r._filePaths.Add(m.FilePath);
                    r._idToPath.TryAdd(m.Id, m.FilePath);
                }
            });
        }

        public IEnumerable<string> GetFilePaths()
        {
            return _filePaths;
        }

        public IEnumerable<string> GetFileNames()
        {
            return _filePaths.Select(x => Path.GetFileNameWithoutExtension(x));
        }

        public string GetPath(string videoId)
        {
            if (_idToPath.ContainsKey(videoId))
            {
                return _idToPath[videoId];
            }
            return "";
        }

        public void Refresh()
        {
            _filePaths.Clear();
            var files = Directory.EnumerateFiles(_appConfiguration.DownloadsFolder, "*",
                new EnumerationOptions { RecurseSubdirectories = true });
            _filePaths.AddRange(files);
            FindIds();
        }

        private void FindIds()
        {
            try
            {
                var map = _filePaths.Select(x => new
                {
                    Id = GetId(x),
                    Path = x
                }).Where(x => x.Id != "");
                _idToPath = new Dictionary<string, string>();
                foreach(var item in map)
                {
                    _idToPath.TryAdd(item.Id, item.Path);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                _idToPath = new Dictionary<string, string>();
            }
        }

        private string GetId(string path)
        {
            return _regex.Matches(path).LastOrDefault()?.Value?.TrimStart('[')?.TrimEnd(']') ?? "";
        }
    }
}
