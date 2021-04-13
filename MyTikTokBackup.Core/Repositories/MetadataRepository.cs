using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyTikTokBackup.Core.Helpers;
using MyTikTokBackup.Core.Services;

namespace MyTikTokBackup.Core.Repositories
{
    public interface IMetadataRepository
    {
        Task<IEnumerable<FullMetadata>> GetAll();
        Task Replace(IEnumerable<FullMetadata> items);
    }

    public class MetadataRepository : IMetadataRepository
    {
        private List<FullMetadata> _metadata;
        private readonly IAppConfiguration _appConfiguration;

        public MetadataRepository(IAppConfiguration appConfiguration)
        {
            _metadata = new List<FullMetadata>();
            _appConfiguration = appConfiguration;
        }

        public async Task<IEnumerable<FullMetadata>> GetAll()
        {
            await LoadIfEmpty();
            return _metadata;
        }

        private async Task LoadIfEmpty()
        {
            if (_metadata.Count == 0)
            {
                _metadata = await JsonHelper.DeserializeFile<List<FullMetadata>>(_appConfiguration.Metadata);
            }
        }

        public async Task Replace(IEnumerable<FullMetadata> items)
        {
            _metadata = items.ToList();
            await JsonHelper.SerializeFile(_appConfiguration.Metadata, _metadata);
        }
    }
}
