// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using MyTikTokBackup.Core.Database;

Console.WriteLine("Hello, World!");


var mainDbContext = new TikTokDbContext2()
{
    DatabaseFolder = @"L:\Tiktok\MyTikTokBackup"
};
var sourceDbContext = new TikTokDbContext2() 
{
    DatabaseFolder = @"C:\Users\tomek\Downloads\MyTikTokBackup"
};





public class TikTokDbContext2 : DbContext
{
    public string DatabaseFolder { get; set; }

    public DbSet<Author> Authors { get; set; }
    public DbSet<Hashtag> Hashtags { get; set; }
    public DbSet<Music> Musics { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<ProfileVideo> ProfileVideos { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = Path.Combine(DatabaseFolder, "tiktok.db");

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