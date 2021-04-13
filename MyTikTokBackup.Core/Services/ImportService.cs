using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using MyTikTokBackup.Core.Dto;
using MyTikTokBackup.Core.Helpers;
using MyTikTokBackup.Core.Repositories;
using MyTikTokBackup.Core.TikTok;

namespace MyTikTokBackup.Core.Services
{
    public interface IImportService
    {
        Task<List<ItemInfo>> GetFavoriteItems(string filePath);
        Task ImportFavoriteVideos(string filePath);
        Task ImportFavoriteVideos(List<ItemInfo> favorites);
    }

    public class ImportService : IImportService
    {
        private readonly IMetadataRepository _metadataRepository;
        private readonly IUserVideosRepository _userVideosRepository;
        private readonly Mapper _mapper;

        public ImportService(IMetadataRepository metadataRepository, 
            IUserVideosRepository userVideosRepository, 
            Mapper mapper)
        {
            _metadataRepository = metadataRepository;
            _userVideosRepository = userVideosRepository;
            _mapper = mapper;
        }
        
        public Task<List<ItemInfo>> GetFavoriteItems(string filePath)
        {
            if (System.IO.Path.GetExtension(filePath) == ".json")
            {
                return JsonHelper.DeserializeFile<List<ItemInfo>>(filePath);
            }
            else if (System.IO.Path.GetExtension(filePath) == ".har")
            {
                return GetFavoriteItemsFromHar(filePath);
            }
            else return Task.FromResult(new List<ItemInfo>());
        }

        public async Task ImportFavoriteVideos(string filePath)
        {
            var favorites = await GetFavoriteItems(filePath);
            await ImportFavoriteVideos(favorites);
        }

        public async Task ImportFavoriteVideos(List<ItemInfo> favorites)
        {
            var metadata = await _metadataRepository.GetAll();
            var userVideos = await _userVideosRepository.GetAll();

            var newVideos = favorites.Select(x => new UserVideo
            {
                VideoId = x.Id,
            });
            var list = favorites.Select(x => _mapper.Map<FullMetadata>(x));
            await _metadataRepository.Replace(metadata.Union(list));
            await _userVideosRepository.Replace(userVideos.Union(newVideos));
        }

        private async Task<List<ItemInfo>> GetFavoriteItemsFromHar(string filePath)
        {
            var archive = await JsonHelper2.DeserializeFile<HarArchive>(filePath);

            if (archive == null) return null;

            var requests = archive.Log.Entries
                .Where(x => x.Request.Url.ToString().Contains("https://m.tiktok.com/api/favorite/item_list"));

            var favList = new List<ItemInfo>();

            foreach (var req in requests)
            {
                try
                {
                    string json = "";
                    if (req.Response.Content.MimeType == "application/json")
                    {
                        if (req.Response.Content.Text.Contains('{'))
                        {
                            json = req.Response.Content.Text;
                        }
                        else
                        {
                            var data = Convert.FromBase64String(req.Response.Content.Text);
                            json = System.Text.Encoding.UTF8.GetString(data);
                        }
                    }
                    else if (req.Response.Content.MimeType == "base64")
                    {
                        var data = Convert.FromBase64String(req.Response.Content.Text);
                        json = System.Text.Encoding.UTF8.GetString(data);
                    }
                    else { }
                    var favorites = JsonSerializer.Deserialize<VideosReponse>(json);
                    foreach (var fav in favorites.ItemList)
                    {
                        fav.Headers.AddRange(req.Request.Headers
                            .Where(x => !x.Name.StartsWith(":"))
                            .Select(x => new TikTok.Header { Name = x.Name, Value = x.Value }));
                        favList.Add(fav);
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex.ToString());
                }
            }

            return favList;
        }
    }
}
