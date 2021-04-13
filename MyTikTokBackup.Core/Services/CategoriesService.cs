using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyTikTokBackup.Core.Repositories;

namespace MyTikTokBackup.Core.Services
{
    public interface ICategoriesService
    {
        Task<Category> CreateCategory(string name);
        Task Delete(string name);
        Task<IEnumerable<Category>> GetAll();
    }

    public class CategoriesService : ICategoriesService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoriesService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Category> CreateCategory(string name)
        {
            var color = await GetAvailableColor();
            var category = new Category() { Color = color, Name = name };
            await _categoryRepository.TryAdd(category);
            return category;
        }

        public async Task Delete(string name)
        {
            await _categoryRepository.Delete(name);
        }

        public Task<IEnumerable<Category>> GetAll()
        {
            return _categoryRepository.GetAll();
        }

        private async Task<string> GetAvailableColor()
        {
            var categories = await _categoryRepository.GetAll();
            return accentColors.FirstOrDefault(x => !categories.Select(x => x.Color).Contains(x));
        }

        private readonly string[] accentColors = new string[] {
            "#FFFFB900", "#FFE74856", "#FF0078D7",  "#FF7A7574",
            "#FFFF8C00",  "#FF0063B1", "#FF9A0089","#FF00CC6A",
             "#FF8E8CD8", "#FF00B7C3",
             "#FF038387",
             "#FFE3008C", "#FF8764B8", "#FF00B294", "#FF567C73", "#FF647C64",
            "#FFEF6950", "#FFBF0077", "#FF744DA9", "#FF018574", "#FF486860", "#FF525E54",
            "#FFD13438", "#FFC239B3", "#FFB146C2",  "#FF498205", "#FF847545",
            "#FFFF4343",  "#FF881798", "#FF10893E", "#FF107C10", "#FF7E735F"
        };
    }
}
