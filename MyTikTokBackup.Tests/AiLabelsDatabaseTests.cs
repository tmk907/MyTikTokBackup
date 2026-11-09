using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyTikTokBackup.Core.Database;
using MyTikTokBackup.Core.Services;
using NUnit.Framework;

namespace MyTikTokBackup.Tests
{
    public class AiLabelsDatabaseTests
    {
        private readonly CategoriesService _service = new CategoriesService();

        [OneTimeSetUp]
        public void SetUp()
        {
            new DatabaseHelper().EnsureCreated();
        }

        private static int AddVideo()
        {
            using var db = new TikTokDbContext();
            var video = new Video { VideoId = Guid.NewGuid().ToString("N"), Description = "test", DuetFromId = "", Ratio = "" };
            db.Videos.Add(video);
            db.SaveChanges();
            return video.Id;
        }

        private static string NewName() => "Cat" + Guid.NewGuid().ToString("N")[..8];

        private static void AddSuggestion(int videoId, string category)
        {
            using var db = new TikTokDbContext();
            db.VideoAiLabels.Add(new VideoAiLabel { VideoId = videoId, CategoryName = category, Score = 0.8f, Status = AiLabelStatus.Suggested, CreatedAt = DateTime.Now });
            db.SaveChanges();
        }

        [Test]
        public void Migration3_CreatesTablesAndIsIdempotent()
        {
            using (var db = new TikTokDbContext())
            {
                db.Database.ExecuteSqlRaw("DROP TABLE VideoAiLabels; DROP TABLE VideoEmbeddings;");
            }

            new DatabaseHelper().EnsureCreated();
            new DatabaseHelper().EnsureCreated();

            using (var db = new TikTokDbContext())
            {
                db.VideoEmbeddings.Add(new VideoEmbedding { VideoId = 123456, TextVector = new byte[] { 1, 2 }, ModelVersion = "x", UpdatedAt = DateTime.Now });
                db.VideoAiLabels.Add(new VideoAiLabel { VideoId = 123456, CategoryName = "A", Score = 0.5f, Status = AiLabelStatus.Rejected, CreatedAt = DateTime.Now });
                db.SaveChanges();
            }
            using (var db = new TikTokDbContext())
            {
                Assert.That(db.VideoEmbeddings.Single(x => x.VideoId == 123456).TextVector, Is.EqualTo(new byte[] { 1, 2 }));
                Assert.That(db.VideoAiLabels.Single(x => x.VideoId == 123456).Status, Is.EqualTo(AiLabelStatus.Rejected));

                db.VideoAiLabels.Add(new VideoAiLabel { VideoId = 123456, CategoryName = "A", Score = 0.5f, CreatedAt = DateTime.Now });
                Assert.Throws<DbUpdateException>(() => db.SaveChanges(), "unique index (VideoId, CategoryName)");
            }
        }

        [Test]
        public async Task Delete_RemovesAiLabels()
        {
            var name = NewName();
            await _service.CreateCategory(name);
            var video = AddVideo();
            AddSuggestion(video, name);

            await _service.Delete(name);

            var labels = await _service.GetVideoAiLabels(new[] { video });
            Assert.That(labels.ContainsKey(video), Is.False);
        }

        [Test]
        public async Task Rename_RepointsAiLabels()
        {
            var name = NewName();
            var newName = NewName();
            await _service.CreateCategory(name);
            var video = AddVideo();
            AddSuggestion(video, name);

            await _service.Rename(name, newName);

            var labels = await _service.GetVideoAiLabels(new[] { video });
            Assert.That(labels[video].Keys, Is.EqualTo(new[] { newName }));
        }

        [Test]
        public async Task AddVideoToCategory_AcceptsSuggestion()
        {
            var name = NewName();
            await _service.CreateCategory(name);
            var video = AddVideo();
            AddSuggestion(video, name);

            await _service.AddVideoToCategory(name, video);

            Assert.That((await _service.GetVideoAiLabels(new[] { video })).ContainsKey(video), Is.False);
            Assert.That((await _service.GetVideoCategories())[video], Is.EqualTo(new[] { name }));
        }

        [Test]
        public async Task RemoveVideoFromCategory_RejectsCategory()
        {
            var name = NewName();
            await _service.CreateCategory(name);
            var video = AddVideo();
            await _service.AddVideoToCategory(name, video);

            await _service.RemoveVideoFromCategory(name, video);

            using var db = new TikTokDbContext();
            Assert.That(db.VideoAiLabels.Single(x => x.VideoId == video && x.CategoryName == name).Status, Is.EqualTo(AiLabelStatus.Rejected));
        }

        [Test]
        public async Task RejectAiLabel_HidesSuggestion_AndClearRejectedRemovesIt()
        {
            var name = NewName();
            await _service.CreateCategory(name);
            var video = AddVideo();
            AddSuggestion(video, name);

            await _service.RejectAiLabel(name, video);
            Assert.That((await _service.GetVideoAiLabels(new[] { video })).ContainsKey(video), Is.False);

            await _service.ClearRejectedAiLabels();
            using var db = new TikTokDbContext();
            Assert.That(db.VideoAiLabels.Any(x => x.VideoId == video), Is.False);
        }
    }
}
