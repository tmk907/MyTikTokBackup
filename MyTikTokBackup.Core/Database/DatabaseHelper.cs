using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MyTikTokBackup.Core.Database
{
    public class DatabaseHelper
    {
        public void EnsureCreated()
        {
            var db = new TikTokDbContext();
            Directory.CreateDirectory(db.DatabaseFolder);
            db.Database.EnsureCreated();

            Migration1(db);
            Migration1b(db);
            Migration2(db);
        }

        private void Migration1(TikTokDbContext db)
        {
            var rowCount = EFCoreHelper.RawSqlQuery<int>(
                """
                select count(*) from pragma_table_info('Videos') where name = 'MyProperty'
                """, dbReader => dbReader.GetFieldValue<int>(0));

            if (rowCount.FirstOrDefault() == 1)
            {
                try
                {
                    var sql =
                    """
                    ALTER TABLE Videos DROP COLUMN MyProperty
                    """;

                    db.Database.ExecuteSqlRaw(sql);
                    db.SaveChanges();
                    Log.Information($"Database {db.DatabaseFolder} migrated to v1");
                }
                catch (Exception ex)
                {
                    Log.Error("Error while migrating database to v1");
                    Log.Error(ex.ToString());
                    throw;
                }
            }
        }

        private void Migration1b(TikTokDbContext db)
        {
            var rowCount = EFCoreHelper.RawSqlQuery<int>(
                """
                select count(*) from pragma_table_info('VideoCategories')
                """, dbReader => dbReader.GetFieldValue<int>(0));

            if (rowCount.Count == 0)
            {
                try
                {
                    var sql =
                    """
                    CREATE TABLE "VideoCategories" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_VideoCategories" PRIMARY KEY AUTOINCREMENT,
                        "CategoryName" TEXT NOT NULL,
                        "VideoId" INTEGER NOT NULL,
                        CONSTRAINT "FK_VideoCategories_Categories_Name" FOREIGN KEY ("CategoryName") REFERENCES "Categories" ("Name") ON DELETE NO ACTION,
                        CONSTRAINT "FK_VideoCategories_Videos_Id" FOREIGN KEY ("VideoId") REFERENCES "Videos" ("Id") ON DELETE NO ACTION
                    )
                    """;

                    db.Database.ExecuteSqlRaw(sql);
                    db.SaveChanges();
                    Log.Information($"Database {db.DatabaseFolder} migrated to v1b");
                }
                catch (Exception ex)
                {
                    Log.Error("Error while migrating database to v1b");
                    Log.Error(ex.ToString());
                    throw;
                }
            }
        }

        private void Migration2(TikTokDbContext db)
        {
            var rowCount = EFCoreHelper.RawSqlQuery<int>(
                """
                select count(*) from pragma_table_info('Hashtags') where name = 'Id' and type = 'TEXT'
                """, dbReader => dbReader.GetFieldValue<int>(0));

            if (rowCount.FirstOrDefault() == 1)
            {
                try
                {
                    var sql =
                    """
                    BEGIN TRANSACTION;
                    CREATE TABLE Hashtags_Copy( 
                        Id INTEGER NOT NULL CONSTRAINT "PK_Hashtags" PRIMARY KEY AUTOINCREMENT, 
                        Name TEXT NULL,
                        OldId TEXT NULL
                    );
                    DROP INDEX "IX_Hashtags_Name";
                    CREATE INDEX "IX_Hashtags_Name" ON "Hashtags_Copy" ("Name");
                    INSERT INTO Hashtags_Copy (Name, OldId) SELECT Name, Id FROM Hashtags;

                    CREATE TABLE HashtagVideo_Copy (
                        HashtagsId TEXT NOT NULL,
                        VideosId INTEGER NOT NULL
                    );
                    INSERT INTO HashtagVideo_Copy (HashtagsId, VideosId) 
                    SELECT hc.Id, hv.VideosId 
                    FROM HashtagVideo hv
                    JOIN Hashtags_Copy hc ON hc.OldId = hv.HashtagsId order by videosid;

                    DROP TABLE Hashtags;
                    ALTER TABLE Hashtags_Copy RENAME TO Hashtags;

                    DROP TABLE HashtagVideo;
                    CREATE TABLE HashtagVideo (
                        HashtagsId INTEGER NOT NULL,
                        VideosId INTEGER NOT NULL,
                        CONSTRAINT "PK_HashtagVideo" PRIMARY KEY ("HashtagsId", "VideosId"),
                        CONSTRAINT "FK_HashtagVideo_Hashtags_HashtagsId" FOREIGN KEY ("HashtagsId") REFERENCES "Hashtags" ("Id") ON DELETE CASCADE,
                        CONSTRAINT "FK_HashtagVideo_Videos_VideosId" FOREIGN KEY ("VideosId") REFERENCES "Videos" ("Id") ON DELETE CASCADE
                    );

                    INSERT INTO HashtagVideo (HashtagsId, VideosId) SELECT HashtagsId, VideosId FROM HashtagVideo_Copy;
                    DROP TABLE HashtagVideo_Copy;
                    COMMIT;
                    """;

                    db.Database.ExecuteSqlRaw(sql);
                    db.SaveChanges();
                    Log.Information($"Database {db.DatabaseFolder} migrated to v2");
                }
                catch (Exception ex)
                {
                    Log.Error("Error while migrating database to v2");
                    Log.Error(ex.ToString());
                    throw;
                }
            }
        }



        public async Task AddOrUpdateProfileVideos(string userUniqueId, FeedType feedType, IEnumerable<string> newVideos)
        {
            using (var db = new TikTokDbContext())
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
            for (int n = 0; n < newVideos.Count; n++)
            {
                if (o < oldVideos.Count && newVideos[n] == oldVideos[o])
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

    public static class EFCoreHelper
    {
        public static List<T> RawSqlQuery<T>(string query, Func<DbDataReader, T> map)
        {
            using (var context = new TikTokDbContext())
            {
                using (var command = context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandType = CommandType.Text;

                    context.Database.OpenConnection();

                    using (var result = command.ExecuteReader())
                    {
                        var entities = new List<T>();

                        while (result.Read())
                        {
                            entities.Add(map(result));
                        }

                        return entities;
                    }
                }
            }
        }
    }
}
