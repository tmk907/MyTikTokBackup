using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyTikTokBackup.Core.Helpers;
using MyTikTokBackup.Core.Services;

namespace MyTikTokBackup.Core.Repositories
{
    public interface IUserVideosRepository
    {
        Task<IEnumerable<UserVideo>> GetAll();
        Task Replace(IEnumerable<UserVideo> items);
    }

    public class UserVideosRepository : IUserVideosRepository
    {
        private List<UserVideo> _userVideos;
        private readonly IAppConfiguration _appConfiguration;

        public UserVideosRepository(IAppConfiguration appConfiguration)
        {
            _userVideos = new List<UserVideo>();
            _appConfiguration = appConfiguration;
        }

        public async Task<IEnumerable<UserVideo>> GetAll()
        {
            await LoadIfEmpty();
            return _userVideos;
        }

        private async Task LoadIfEmpty()
        {
            if (_userVideos.Count == 0)
            {
                _userVideos = await JsonHelper.DeserializeFile<List<UserVideo>>(_appConfiguration.Videos);
            }
        }

        public async Task Replace(IEnumerable<UserVideo> items)
        {
            _userVideos = items.ToList();
            await JsonHelper.SerializeFile(_appConfiguration.Videos, _userVideos);
        }
    }
}
