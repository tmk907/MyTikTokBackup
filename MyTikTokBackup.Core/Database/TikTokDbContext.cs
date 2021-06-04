using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using MyTikTokBackup.Core.Services;
using Serilog;

namespace MyTikTokBackup.Core.Database
{
    class TikTokDbContext : DbContext
    {
        private static Lazy<string> appFolder = new Lazy<string>(() => Ioc.Default.GetService<IAppConfiguration>().DownloadsFolder);

        public DbSet<Author> Authors { get; set; }
        public DbSet<Hashtag> Hashtags { get; set; }
        public DbSet<Music> Musics { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<ProfileVideo> ProfileVideos { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(appFolder.Value, "tiktok.db");

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            //optionsBuilder.LogTo(Log.Information);
            //optionsBuilder.EnableSensitiveDataLogging(true);
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Author>()
                .HasIndex(x => x.UniqueId);
            modelBuilder.Entity<Author>().OwnsOne(p => p.Stats);
            
            modelBuilder.Entity<Video>()
                .HasIndex(x => x.VideoId);
            modelBuilder.Entity<Video>().OwnsOne(p => p.Stats);

            modelBuilder.Entity<Hashtag>()
                .HasIndex(x => x.Name);


            modelBuilder.Entity<ProfileVideo>()
                .Property(x => x.FeedType).HasConversion<string>();

            modelBuilder.Entity<Category>()
                .HasKey(x => x.Name);
        }
    }
}
