using Microsoft.EntityFrameworkCore;

namespace MyTikTokBackup.Core.Database
{
    class TikTokDbContext : DbContext
    {
        public DbSet<Author> Authors { get; set; }
        public DbSet<Hashtag> Hashtags { get; set; }
        public DbSet<Music> Musics { get; set; }
        public DbSet<Video> Videos { get; set; }

        public TikTokDbContext()
        {
            this.Database.EnsureCreated();
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    string dbPath = Path.Combine(FileSystem.AppDataDirectory, "blogs.db3");

        //    optionsBuilder.UseSqlite($"Filename={dbPath}");
        //}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=tiktok.db");
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Video>()
                .HasIndex(x => x.VideoId);

            modelBuilder.Entity<Hashtag>()
                .HasIndex(x => x.Name);

            modelBuilder.Entity<Author>()
                .HasIndex(x => x.UniqueId);
        }
    }
}
