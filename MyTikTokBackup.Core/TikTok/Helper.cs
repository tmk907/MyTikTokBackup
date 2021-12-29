using MyTikTokBackup.Core.TikTok.Website;
using System.Linq;

namespace MyTikTokBackup.Core.TikTok
{
    public class Helper
    {
        public static ItemInfo ToItemInfo(PostedVideo x)
        {
            return new ItemInfo
            {
                Author = new Author
                {
                    Id = x.AuthorId,
                    Nickname = x.Nickname,
                    SecUid = x.AuthorSecId,
                    UniqueId = x.Author,
                    Signature = ""
                },
                AuthorStats = new Core.TikTok.AuthorStats
                {
                    DiggCount = x.AuthorStats.DiggCount,
                    FollowerCount = x.AuthorStats.FollowerCount,
                    FollowingCount = x.AuthorStats.FollowingCount,
                    Heart = x.AuthorStats.Heart,
                    HeartCount = x.AuthorStats.HeartCount,
                    VideoCount = x.AuthorStats.VideoCount
                },
                Desc = x.Desc,
                Id = x.Id,
                Music = new Core.TikTok.Music
                {
                    Album = x.Music.Album,
                    AuthorName = x.Music.AuthorName,
                    CoverLarge = x.Music.CoverLarge,
                    CoverMedium = x.Music.CoverMedium,
                    CoverThumb = x.Music.CoverThumb,
                    Duration = x.Music.Duration,
                    Id = x.Music.Id,
                    Original = x.Music.Original,
                    PlayUrl = x.Music.PlayUrl,
                    Title = x.Music.Title
                },
                Stats = new Core.TikTok.Stats
                {
                    CommentCount = x.Stats.CommentCount,
                    DiggCount = x.Stats.DiggCount,
                    PlayCount = x.Stats.PlayCount,
                    ShareCount = x.Stats.ShareCount
                },
                TextExtra = x.TextExtra.Select(x => new Core.TikTok.TextExtra
                {
                    AwemeId = x.AwemeId,
                    End = x.End,
                    HashtagId = x.HashtagId,
                    HashtagName = x.HashtagName,
                    UserId = x.UserId,
                    UserUniqueId = x.UserUniqueId
                }).ToList(),
                Video = new Core.TikTok.Video
                {
                    Cover = x.Video.Cover,
                    DownloadAddr = x.Video.DownloadAddr,
                    Duration = x.Video.Duration,
                    DynamicCover = x.Video.DynamicCover,
                    Height = x.Video.Height,
                    Id = x.Video.Id,
                    OriginCover = x.Video.OriginCover,
                    PlayAddr = x.Video.PlayAddr,
                    Ratio = x.Video.Ratio,
                    ReflowCover = x.Video.ReflowCover,
                    ShareCover = x.Video.ShareCover,
                    Width = x.Video.Width
                }
            };
        }
    }
}
