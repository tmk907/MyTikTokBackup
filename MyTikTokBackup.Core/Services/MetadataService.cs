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
            var author = await AddOrUpdateAuthor(item);
            var hashTags = await AddOrUpdateHashtags(item);
            var music = await AddOrUpdateMusic(item);
            await AddOrUpdateVideo(item, author, music, hashTags);
        }

        private async Task<Database.Author> AddOrUpdateAuthor(ItemInfo item)
        {
            try
            {
                using var db = new TikTokDbContext();
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
                        item.AuthorStats.DiggCount,
                        item.AuthorStats.FollowerCount,
                        item.AuthorStats.FollowingCount,
                        item.AuthorStats.HeartCount,
                        item.AuthorStats.VideoCount
                    );
                    db.Authors.Add(author);
                }
                else
                {
                    db.Remove(author.Stats);
                    author.Stats = Database.AuthorStats.Create(
                        item.AuthorStats.DiggCount,
                        item.AuthorStats.FollowerCount,
                        item.AuthorStats.FollowingCount,
                        item.AuthorStats.HeartCount,
                        item.AuthorStats.VideoCount
                    );
                    db.Authors.Update(author);
                }

                //AddOrUpdate(db, author);
                await db.SaveChangesAsync();
                return author;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }
        }

        private async Task<Database.Music> AddOrUpdateMusic(ItemInfo item)
        {
            try
            {
                using var db = new TikTokDbContext();
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
                Log.Error(ex.ToString());
                return null;
            }
        }

        private async Task<IEnumerable<Hashtag>> AddOrUpdateHashtags(ItemInfo item)
        {
            if(item.TextExtra == null) return new List<Hashtag>();
            try
            {
                using var db = new TikTokDbContext();

                var hashtags = item.TextExtra
                    .Where(x => !string.IsNullOrEmpty(x.HashtagId) && !string.IsNullOrEmpty(x.HashtagName))
                    .Select(x => new Hashtag { Id = x.HashtagId, Name = x.HashtagName });
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

        private async Task<Database.Video> AddOrUpdateVideo(ItemInfo item, Database.Author author, Database.Music music, IEnumerable<Hashtag> hashtags)
        {
            try
            {
                using var db = new TikTokDbContext();

                var video = await db.Videos.FirstOrDefaultAsync(x => x.VideoId == item.Video.Id);
                if (video == null)
                {
                    video = new Database.Video
                    {
                        Author = author,
                        Description = item.Desc,
                        DuetFromId = item.DuetInfo?.DuetFromId,
                        Duration = TimeSpan.FromSeconds(item.Video.Duration),
                        Hashtags = hashtags.ToList(),
                        Height = (int)item.Video.Height,
                        VideoId = item.Video.Id,
                        Music = music,
                        Ratio = item.Video.Ratio,
                        Stats = VideoStats.Create(
                           item.Stats.CommentCount,
                           item.Stats.DiggCount,
                           item.Stats.PlayCount,
                           item.Stats.ShareCount
                        ),
                        Width = (int)item.Video.Width
                    };
                    db.Videos.Update(video);
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
                }

                await db.SaveChangesAsync();

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
