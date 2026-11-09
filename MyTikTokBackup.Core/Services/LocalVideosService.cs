using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.Messaging;
using Serilog;
using MyTikTokBackup.Core.Messages;

namespace MyTikTokBackup.Core.Services
{
    public interface ILocalVideosService
    {
        IEnumerable<string> GetFileNames();
        IEnumerable<string> GetFilePaths();
        string GetPath(string videoId);
        /// <summary>All local files of the video (photo posts have many images).</summary>
        IReadOnlyList<string> GetPaths(string videoId);
        void Refresh();
    }

    public class LocalVideosService : ILocalVideosService
    {
        private readonly List<string> _filePaths;
        private readonly Regex _regex;
        private readonly IAppConfiguration _appConfiguration;
        private Dictionary<string, List<string>> _idToPaths;
        private readonly object _lock = new object();

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
                    r.AddPath(m.Id, m.FilePath);
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
            if (_idToPaths.TryGetValue(videoId, out var paths))
            {
                return paths[0];
            }
            return "";
        }

        public IReadOnlyList<string> GetPaths(string videoId)
        {
            lock (_lock)
            {
                if (videoId != null && _idToPaths.TryGetValue(videoId, out var paths))
                {
                    return paths.ToList();
                }
            }
            return Array.Empty<string>();
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
                _idToPaths = new Dictionary<string, List<string>>();
                foreach(var item in map)
                {
                    AddPath(item.Id, item.Path);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                _idToPaths = new Dictionary<string, List<string>>();
            }
        }

        private void AddPath(string id, string path)
        {
            lock (_lock)
            {
                if (!_idToPaths.TryGetValue(id, out var paths))
                {
                    paths = new List<string>();
                    _idToPaths[id] = paths;
                }
                if (!paths.Contains(path))
                {
                    paths.Add(path);
                }
            }
        }

        private string GetId(string path)
        {
            return _regex.Matches(path).LastOrDefault()?.Value?.TrimStart('[')?.TrimEnd(']') ?? "";
        }
    }
}
