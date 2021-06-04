using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MyTikTokBackup.Core.Database
{
    public class DatabaseHelper
    {
        public void Ensure()
        {
            var db = new TikTokDbContext();
            db.Database.EnsureCreated();
        }

        public async Task AddOrUpdateProfileVideos(string userUniqueId, FeedType feedType, IEnumerable<string> newVideos)
        {
            using(var db = new TikTokDbContext())
            {
                var oldVideos = await db.ProfileVideos.Where(x => x.UserUniqueId == userUniqueId && x.FeedType == feedType).OrderBy(x => x.Index).ToListAsync();

                if (oldVideos.Count == 0)
                {
                    db.ProfileVideos.AddRange(Create(0, userUniqueId, feedType, newVideos));
                }
                else
                {
                    var merged = Merge(oldVideos.Select(x => x.VideoId).ToList(), newVideos.ToList());
                    db.ProfileVideos.RemoveRange(oldVideos);
                    db.ProfileVideos.AddRange(Create(0, userUniqueId, feedType, merged));
                }
                await db.SaveChangesAsync();
            }
        }

        private IEnumerable<ProfileVideo> Create(int startIndex, string userId, FeedType feedType, IEnumerable<string> videos)
        {
            return videos.Select((x, i) => new ProfileVideo { FeedType = feedType, Index = startIndex + i, UserUniqueId = userId, VideoId = x });
        }

        public IEnumerable<string> Merge(IList<string> oldVideos, IList<string> newVideos)
        {
            var merged = new List<string>();

            int o = 0;
            for(int n = 0; n < newVideos.Count; n++)
            {
                if(o < oldVideos.Count && newVideos[n] == oldVideos[o])
                {
                    merged.Add(newVideos[n]);
                    o++;
                }
                else
                {
                    merged.Add(newVideos[n]);
                }
            }
            var mergedAsSet = new HashSet<string>(merged);
            for (int i = o; i < oldVideos.Count; i++)
            {
                if (!mergedAsSet.Contains(oldVideos[i]))
                {
                    merged.Add(oldVideos[i]);
                }
            }

            Log.Information($"Merged new {newVideos.Count} old {oldVideos.Count} merged {merged.Count}");

            return merged;
        }
    }
}
