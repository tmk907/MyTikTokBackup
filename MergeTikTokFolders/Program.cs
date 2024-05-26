// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.DependencyInjection;
using MyTikTokBackup.Core.Database;
using MyTikTokBackup.Core.Services;
using Similarity;

Console.WriteLine("Hello, World!");


var mainDbContext = new TikTokDbContext2()
{
    DatabaseFolder = @"D:\TikTok\MyTikTokBackup"
};
//var sourceDbContext = new TikTokDbContext2() 
//{
//    DatabaseFolder = @"C:\Users\tomek\Downloads\MyTikTokBackup"
//};

var categoriesFolder = @"D:\Filmy\Tiktok\kategorie";

var videos = await mainDbContext.Videos.Include(x => x.Author).Select(x => new SimpleVideo(x)).ToListAsync();
var services = new ServiceCollection();
services.AddTransient<IAppConfiguration, SimpleConfig>();
Ioc.Default.ConfigureServices(services.BuildServiceProvider());
var categoriesService = new CategoriesService();
var vidCat = new List<VideoCategory>();
var matches = new List<Match>();

mainDbContext.Database.Migrate();

foreach (var file in Directory.GetFiles(categoriesFolder))
{
    var lines = File.ReadAllLines(file);
    var categoryName = Path.GetFileNameWithoutExtension(file);
    var category = await mainDbContext.Categories.FirstOrDefaultAsync(x => x.Name == categoryName);
    if (category == null)
    {
        category = await categoriesService.CreateCategory(categoryName);
        category = await mainDbContext.Categories.FirstOrDefaultAsync(x => x.Name == categoryName);
    }

    vidCat.Clear();
    videos = await mainDbContext.Videos.Include(x => x.Author).Select(x => new SimpleVideo(x)).ToListAsync();

    foreach (var line in lines)
    {
        var splitted = line.Split(" - ");
        var author = splitted[0].Trim();
        var title = splitted[1].Trim();
        if (splitted.Length > 2)
        {
            title = String.Join("", splitted.Skip(1));
        }

        var video = videos.FirstOrDefault(x => (x.Nickname == author || x.UniqueId == author) && x.Title == title);
        if (video != null)
        {
            vidCat.Add(new VideoCategory { Category = category, Video = video.Video });
            matches.Add(new Match { FileLine = line, Video = video.Video, Type = "Type: Author and title" });
        }
        else
        {
            video = videos.FirstOrDefault(x => !string.IsNullOrWhiteSpace(title) && x.Title == title);
            if (video != null)
            {
                vidCat.Add(new VideoCategory { Category = category, Video = video.Video });
                matches.Add(new Match { FileLine = line, Video = video.Video, Type = "Type: Title" });
            }
            else
            {
                video = videos.FirstOrDefault(x => CompareTokens(x.Title, title));
                if (video != null)
                {
                    vidCat.Add(new VideoCategory { Category = category, Video = video.Video });
                    matches.Add(new Match { FileLine = line, Video = video.Video, Type = "Type: Tokens" });
                }
                else
                {
                    //Console.WriteLine(line);
                    matches.Add(new Match { FileLine = line, Video = null, Type = "Type: not matched" });
                }
            }
        }
    }
    Console.WriteLine($"{lines.Count()} videos in category {categoryName}, {vidCat.Count} matched");
    try
    {
        mainDbContext.VideoCategories.AddRange(vidCat);
        await mainDbContext.SaveChangesAsync();

        File.WriteAllLines(@$"D:\TikTok\MyTikTokBackup\matched-{categoryName}.txt",
            matches.Select(x => new[] { x.Type, x.MatchedFileLine, x.MatchedVideo }).SelectMany(x => x.ToList()));
    }
    catch (Exception ex)
    {

    }
}


bool CompareTokens(string title1, string title2)
{
    var similarity = StringSimilarity.Calculate(title1, title2);
    return ((double)similarity) > 0.9;

    //var split1 = title1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    //var split2 = title2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    //var same = 0;
    //if (split1.Length > 2)
    //{
    //    foreach(var item in split1.Where(x => !x.StartsWith("#")))
    //    {
    //        if (split2.Contains(item))
    //        {
    //            same++;
    //        }
    //    }
    //}

    //if ((double)same / split1.Count(x=>!x.StartsWith("#")) > 0.7)
    //{
    //    return true;
    //}

    //return false;
}

class SimpleVideo
{
    public string Title => Video.Description ?? "";
    public string Nickname => Video.Author?.Nickname ?? "";
    public string UniqueId => Video.Author?.UniqueId ?? "";
    public int Id => Video.Id;
    public Video Video { get; }
    public SimpleVideo(Video video)
    {
        Video = video;
    }
}

public class Match
{
    public string Type { get; set; }
    public Video Video { get; set; }
    public string FileLine { get; set; }
    public string MatchedFileLine
    {
        get
        {
            var splitted = FileLine.Split(" - ");
            return $"{splitted[1]} - {splitted[0]}"; 
        }
    }
    public string MatchedVideo => $"{Video?.Description} - {Video?.Author?.UniqueId} - {Video?.Author?.Nickname} - {Video?.VideoId}";
}

public class SimpleConfig : IAppConfiguration
{
    public string DownloadsFolder { get => @"D:\TikTok\MyTikTokBackup"; set => throw new NotImplementedException(); }

    public string Categories => throw new NotImplementedException();

    public string Videos => throw new NotImplementedException();

    public string Metadata => throw new NotImplementedException();

    public string AppLocalFolder => throw new NotImplementedException();
}

public class TikTokDbContext2 : DbContext
{
    public string DatabaseFolder { get; set; }

    public DbSet<Author> Authors { get; set; }
    public DbSet<Hashtag> Hashtags { get; set; }
    public DbSet<Music> Musics { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<ProfileVideo> ProfileVideos { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<VideoCategory> VideoCategories { get; set; }

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