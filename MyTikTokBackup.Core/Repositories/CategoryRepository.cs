using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyTikTokBackup.Core.Helpers;
using MyTikTokBackup.Core.Services;

namespace MyTikTokBackup.Core.Repositories
{
    public interface ICategoryRepository
    {
        Task Delete(string name);
        Task<IEnumerable<Category>> GetAll();
        Task<bool> TryAdd(Category category);
    }

    public class CategoryRepository : ICategoryRepository
    {
        private HashSet<Category> _categories;
        private readonly IAppConfiguration _appConfiguration;

        public CategoryRepository(IAppConfiguration appConfiguration)
        {
            _categories = new HashSet<Category>();
            _appConfiguration = appConfiguration;
        }

        public async Task<IEnumerable<Category>> GetAll()
        {
            await LoadIfEmpty();
            return _categories.OrderBy(x => x.Name).ToList();
        }

        private async Task LoadIfEmpty()
        {
            if (_categories.Count == 0)
            {
                _categories = (await JsonHelper.DeserializeFile<List<Category>>(_appConfiguration.Categories)).ToHashSet();
            }
        }

        public async Task<bool> TryAdd(Category category)
        {
            await LoadIfEmpty();
            if (_categories.Contains(category))
            {
                return false;
            }
            else
            {
                _categories.Add(category);
                await JsonHelper.SerializeFile(_appConfiguration.Categories, _categories.ToList());
                return true;
            }
        }

        public async Task Delete(string name)
        {
            await LoadIfEmpty();
            var category = _categories.FirstOrDefault(x => x.Name == name);
            if (category != null)
            {
                _categories.Remove(category);
                await JsonHelper.SerializeFile(_appConfiguration.Categories, _categories.ToList());
            }
        }
    }
}
