using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyTikTokBackup.Core.Database;
using MyTikTokBackup.Core.TikTok;
using Serilog;

namespace MyTikTokBackup.Core.Services
{
    public class MetadataService
    {
        public async Task AddOrUpdateMetadataFromVideo(ItemInfo item)
        {
            using var db = new TikTokDbContext();

            var author = await AddOrUpdateAuthor(item, db);
            var hashTags = await AddOrUpdateHashtags(item,db);
            var music = await AddOrUpdateMusic(item,db);
            await AddOrUpdateVideo(item, author, music, hashTags,db);
        }

        private async Task<Database.Author> AddOrUpdateAuthor(ItemInfo item, TikTokDbContext db)
        {
            try
            {
                var author = await db.Authors.Include(x => x.Stats).FirstOrDefaultAsync(x => x.Id == item.Author.Id);
                if (author == null)
                {
                    author = new Database.Author
                    {
                        Id = item.Author.Id,
                        Nickname = item.Author.Nickname,
                        Signature = item.Author.Signature,
                        UniqueId = item.Author.UniqueId
                    };
                    author.Stats = Database.AuthorStats.Create(
                        item.AuthorStats?.DiggCount ?? 0,
                        item.AuthorStats?.FollowerCount ?? 0,
                        item.AuthorStats?.FollowingCount ?? 0,
                        item.AuthorStats?.HeartCount ?? 0,
                        item.AuthorStats?.VideoCount ?? 0
                    );
                    db.Authors.Add(author);
                }
                else
                {
                    author.Stats.DiggCount = item.AuthorStats?.DiggCount ?? 0;
                    author.Stats.FollowerCount = item.AuthorStats?.FollowerCount ?? 0;
                    author.Stats.FollowingCount = item.AuthorStats?.FollowingCount ?? 0;
                    author.Stats.HeartCount = item.AuthorStats?.HeartCount ?? 0;
                    author.Stats.VideoCount = item.AuthorStats?.VideoCount ?? 0;
                    db.Authors.Update(author);
                }

                await db.SaveChangesAsync();
                return author;
            }
            catch (Exception ex)
            {
                try
                {
                    var author = await db.Authors.FindAsync(item.Author.Id);
                    return author;
                }
                catch 
                {
                    Log.Error(ex.ToString());
                    return null;
                }
            }
        }

        private async Task<Database.Music> AddOrUpdateMusic(ItemInfo item, TikTokDbContext db)
        {
            try
            {
                var music = await db.Musics.FirstOrDefaultAsync(x => x.Id == item.Music.Id);
                if (music == null)
                {
                    music = new Database.Music
                    {
                        Album = item.Music.Album,
                        AuthorName = item.Music.AuthorName,
                        Duration = TimeSpan.FromSeconds(item.Music.Duration),
                        Id = item.Id = item.Music.Id,
                        Title = item.Music.Title
                    };
                    db.Musics.Add(music);
                    await db.SaveChangesAsync();
                }
                return music;
            }
            catch (Exception ex)
            {
                try
                {
                    var music = await db.Musics.FindAsync(item.Music.Id);
                    return music;
                }
                catch
                {
                    Log.Error(ex.ToString());
                    return null;
                }
            }
        }

        private async Task<IEnumerable<Hashtag>> AddOrUpdateHashtags(ItemInfo item, TikTokDbContext db)
        {
            if(item.TextExtra == null) return new List<Hashtag>();
            try
            {
                var hashtags = item.TextExtra
                    .Where(x => !string.IsNullOrEmpty(x.HashtagName))
                    .Select(x => new Hashtag { Name = x.HashtagName });
                var hashtagNames = hashtags.Select(x => x.Name).ToList();
                var fromDb = await db.Hashtags.Where(x => hashtagNames.Contains(x.Name)).ToListAsync();
                var toInsert = hashtags.Except(fromDb);

                db.Hashtags.AddRange(toInsert);
                await db.SaveChangesAsync();

                return fromDb.Concat(toInsert);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return new List<Hashtag>();
            }
        }

        private async Task<Database.Video> AddOrUpdateVideo(ItemInfo item, Database.Author author, Database.Music music, IEnumerable<Hashtag> hashtags, TikTokDbContext db)
        {
            try
            {

                var video = await db.Videos.FirstOrDefaultAsync(x => x.VideoId == item.Video.Id);
                if (video == null)
                {
                    video = new Database.Video
                    {
                        Description = item.Desc,
                        DuetFromId = "",//item.DuetInfo?.DuetFromId,
                        Duration = TimeSpan.FromSeconds(item.Video.Duration),
                        Hashtags = hashtags.ToList(),
                        Height = (int)item.Video.Height,
                        VideoId = item.Video.Id,
                        Ratio = item.Video.Ratio,
                        Stats = VideoStats.Create(
                           item.Stats.CommentCount,
                           item.Stats.DiggCount,
                           item.Stats.PlayCount,
                           item.Stats.ShareCount
                        ),
                        Width = (int)item.Video.Width
                    };
                    db.Videos.Add(video);
                    await db.SaveChangesAsync();

                    video.AuthorId = author.Id;
                    video.MusicId = music.Id;
                    db.Videos.Update(video);
                    await db.SaveChangesAsync();
                }
                else
                {
                    db.Remove(video.Stats);
                    video.Stats = VideoStats.Create(
                        item.Stats.CommentCount,
                        item.Stats.DiggCount,
                        item.Stats.PlayCount,
                        item.Stats.ShareCount
                    );
                    db.Videos.Update(video);
                    await db.SaveChangesAsync();
                }


                return video;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }
        }
    }
}
