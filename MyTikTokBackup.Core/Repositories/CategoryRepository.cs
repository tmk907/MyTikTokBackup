using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyTikTokBackup.Core.Database;

namespace MyTikTokBackup.Core.Repositories
{
    public interface ICategoryRepository
    {
        Task Delete(string name);
        Task<IEnumerable<Category>> GetAll();
        Task AddOrUpdate(Category category);
    }

    public class CategoryRepository : ICategoryRepository
    {
        public async Task<IEnumerable<Category>> GetAll()
        {
            using var dbContext = new TikTokDbContext();
            var categories = await dbContext.Categories.OrderBy(x => x.Name).AsNoTracking().ToListAsync();
            return categories;
        }


        public async Task AddOrUpdate(Category category)
        {
            using var dbContext = new TikTokDbContext();
            dbContext.Categories.Update(category);
            await dbContext.SaveChangesAsync();
        }

        public async Task Delete(string name)
        {
            using var dbContext = new TikTokDbContext();
            var category = await dbContext.Categories.FirstOrDefaultAsync(x => x.Name == name);
            dbContext.Categories.Remove(category);
            await dbContext.SaveChangesAsync();
        }
    }
}
