using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyTikTokBackup.Core.Database;

namespace MyTikTokBackup.Core.Services
{
    public interface ICategoriesService
    {
        Task AddVideoToCategory(string categoryName, int videoId);
        Task<Category> CreateCategory(string name);
        Task Delete(string name);
        Task<List<Category>> GetAll();
        Task<Dictionary<int, List<string>>> GetVideoCategories();
        Task RemoveVideoFromCategory(string categoryName, int videoId);
        Task Rename(string oldName, string newName);
    }

    public class CategoriesService : ICategoriesService
    {
        public async Task<Category> CreateCategory(string name)
        {
            var color = await GetAvailableColor();
            var category = new Category() { Color = color, Name = name };
            using var db = new TikTokDbContext();
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return category;
        }

        public async Task Delete(string name)
        {
            using var db = new TikTokDbContext();
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.VideoCategories.Where(x => x.CategoryName == name).ExecuteDeleteAsync();
            await db.Categories.Where(x => x.Name == name).ExecuteDeleteAsync();
            await transaction.CommitAsync();
        }

        public async Task Rename(string oldName, string newName)
        {
            // Name is the primary key and EF Core can't change a key value,
            // so rename = add new category, re-point links, delete old category.
            using var db = new TikTokDbContext();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var oldCategory = await db.Categories.AsNoTracking().FirstAsync(x => x.Name == oldName);
            db.Categories.Add(new Category { Name = newName, Color = oldCategory.Color });
            await db.SaveChangesAsync();
            await db.VideoCategories
                .Where(x => x.CategoryName == oldName)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CategoryName, newName));
            await db.Categories.Where(x => x.Name == oldName).ExecuteDeleteAsync();
            await transaction.CommitAsync();
        }

        public async Task<List<Category>> GetAll()
        {
            using var db = new TikTokDbContext();
            return await db.Categories.AsNoTracking().ToListAsync();
        }

        public async Task<Dictionary<int, List<string>>> GetVideoCategories()
        {
            using var db = new TikTokDbContext();
            var links = await db.VideoCategories
                .Select(x => new { x.VideoId, x.CategoryName })
                .ToListAsync();

            return links
                .GroupBy(x => x.VideoId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.CategoryName).Distinct().ToList());
        }

        public async Task AddVideoToCategory(string categoryName, int videoId)
        {
            using var db = new TikTokDbContext();
            var exists = await db.VideoCategories.AnyAsync(x => x.CategoryName == categoryName && x.VideoId == videoId);
            if (exists) return;

            db.VideoCategories.Add(new VideoCategory { CategoryName = categoryName, VideoId = videoId });
            await db.SaveChangesAsync();
        }

        public async Task RemoveVideoFromCategory(string categoryName, int videoId)
        {
            using var db = new TikTokDbContext();
            await db.VideoCategories
                .Where(x => x.CategoryName == categoryName && x.VideoId == videoId)
                .ExecuteDeleteAsync();
        }

        private async Task<string> GetAvailableColor()
        {
            var usedColors = (await GetAll()).Select(x => x.Color).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return accentColors.FirstOrDefault(x => !usedColors.Contains(x))
                ?? accentColors[usedColors.Count % accentColors.Length];
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
