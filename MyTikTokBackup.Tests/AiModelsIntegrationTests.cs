using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using MyTikTokBackup.Core.AiLabels;
using MyTikTokBackup.Core.Database;
using MyTikTokBackup.Core.Services;
using NUnit.Framework;

namespace MyTikTokBackup.Tests
{
    /// <summary>
    /// Needs downloaded models: set MYTIKTOK_AI_MODELS to a folder with the files named as in <see cref="AiModelCatalog"/>.
    /// </summary>
    [Explicit("Needs AI models")]
    public class AiModelsIntegrationTests
    {
        private string _modelsFolder;

        [OneTimeSetUp]
        public void SetUp()
        {
            _modelsFolder = Environment.GetEnvironmentVariable("MYTIKTOK_AI_MODELS");
            if (string.IsNullOrEmpty(_modelsFolder) || !Directory.Exists(_modelsFolder))
            {
                Assert.Ignore("MYTIKTOK_AI_MODELS not set");
            }
        }

        private TextEncoder CreateTextEncoder() => new TextEncoder(
            Path.Combine(_modelsFolder, AiModelCatalog.TextModel),
            Path.Combine(_modelsFolder, AiModelCatalog.Vocab),
            Path.Combine(_modelsFolder, AiModelCatalog.Dense),
            new SessionOptions());

        private static RgbImage SolidImage(byte r, byte g, byte b)
        {
            var rgba = new byte[ClipPreprocessor.PixelCount * 4];
            for (int i = 0; i < ClipPreprocessor.PixelCount; i++)
            {
                rgba[i * 4] = r;
                rgba[i * 4 + 1] = g;
                rgba[i * 4 + 2] = b;
                rgba[i * 4 + 3] = 255;
            }
            return new RgbImage(rgba, ClipPreprocessor.Size, ClipPreprocessor.Size);
        }

        [Test]
        public void TextEncoder_Tokenizes()
        {
            using var encoder = CreateTextEncoder();

            var ids = encoder.Tokenize("Przepis na szybki obiad, żółć gęś #gotowanie");
            TestContext.Out.WriteLine(string.Join(",", ids));

            Assert.That(ids[0], Is.EqualTo(101), "[CLS]");
            Assert.That(ids[^1], Is.EqualTo(102), "[SEP]");
            Assert.That(ids, Does.Not.Contain(100), "[UNK]");
            Assert.That(encoder.Tokenize(string.Join(" ", Enumerable.Repeat("słowo", 500))).Count, Is.EqualTo(128));
        }

        [Test]
        public void TextEncoder_IsMultilingualAndSemantic()
        {
            using var encoder = CreateTextEncoder();

            // CLIP text space is anisotropic (unrelated texts have cosine ~0.85), only relative order matters.
            var v = encoder.Encode(new[] { "zdjęcie psa", "a dog", "samochód", "a car", "Przepis na pyszny obiad", "cooking a recipe for dinner", "" });

            Assert.That(v.All(x => x.Length == AiModelCatalog.EmbeddingSize));
            Assert.That(MathF.Sqrt(v[0].Sum(x => x * x)), Is.EqualTo(1f).Within(1e-4));
            Assert.That(VectorMath.Dot(v[0], v[1]), Is.GreaterThan(VectorMath.Dot(v[0], v[3])));
            Assert.That(VectorMath.Dot(v[2], v[3]), Is.GreaterThan(VectorMath.Dot(v[2], v[1])));
            Assert.That(VectorMath.Dot(v[4], v[5]), Is.GreaterThan(VectorMath.Dot(v[4], v[1])));
            TestContext.Out.WriteLine($"psa/dog {VectorMath.Dot(v[0], v[1]):F3} psa/car {VectorMath.Dot(v[0], v[3]):F3} obiad/dinner {VectorMath.Dot(v[4], v[5]):F3}");
        }

        [Test]
        public void ImageEncoder_SharesSpaceWithTextEncoder()
        {
            using var textEncoder = CreateTextEncoder();
            using var imageEncoder = new ImageEncoder(Path.Combine(_modelsFolder, AiModelCatalog.VisionModel), new SessionOptions());

            var images = imageEncoder.Encode(new[] { SolidImage(220, 20, 20), SolidImage(20, 180, 40) });
            var texts = textEncoder.Encode(new[] { "czerwony kolor", "zielony kolor" });

            TestContext.Out.WriteLine($"red/red {VectorMath.Dot(images[0], texts[0]):F3} red/green {VectorMath.Dot(images[0], texts[1]):F3} green/green {VectorMath.Dot(images[1], texts[1]):F3} green/red {VectorMath.Dot(images[1], texts[0]):F3}");
            Assert.That(VectorMath.Dot(images[0], texts[0]), Is.GreaterThan(VectorMath.Dot(images[0], texts[1])));
            Assert.That(VectorMath.Dot(images[1], texts[1]), Is.GreaterThan(VectorMath.Dot(images[1], texts[0])));
        }

        private sealed class FakeLocalVideos : ILocalVideosService
        {
            public Dictionary<string, string> Paths { get; } = new();
            public IEnumerable<string> GetFileNames() => Paths.Values;
            public IEnumerable<string> GetFilePaths() => Paths.Values;
            public string GetPath(string videoId) => Paths.GetValueOrDefault(videoId, "");
            public IReadOnlyList<string> GetPaths(string videoId) => Paths.TryGetValue(videoId, out var p) ? new[] { p } : Array.Empty<string>();
            public void Refresh() { }
        }

        private sealed class FakeFrameExtractor : IMediaFrameExtractor
        {
            public Task<IReadOnlyList<RgbImage>> ExtractVideoFramesAsync(string path, int count, CancellationToken cancellationToken)
            {
                var red = path.Contains("red");
                IReadOnlyList<RgbImage> frames = Enumerable.Range(0, count).Select(_ => red ? SolidImage(220, 20, 20) : SolidImage(20, 180, 40)).ToList();
                return Task.FromResult(frames);
            }

            public Task<RgbImage> LoadImageAsync(string path, CancellationToken cancellationToken) => throw new NotSupportedException();
        }

        [Test]
        public async Task Service_LabelsVideosEndToEnd()
        {
            var modelsTarget = Path.Combine(TestEnvironment.Configuration.DownloadsFolder, "Cache", "Models");
            Directory.CreateDirectory(modelsTarget);
            foreach (var file in AiModelCatalog.Files)
            {
                File.Copy(Path.Combine(_modelsFolder, file.FileName), Path.Combine(modelsTarget, file.FileName), overwrite: true);
            }

            new DatabaseHelper().EnsureCreated();
            var categories = new CategoriesService();
            var cooking = "Gotowanie " + Guid.NewGuid().ToString("N")[..4];
            var dogs = "Psy " + Guid.NewGuid().ToString("N")[..4];
            await categories.CreateCategory(cooking);
            await categories.CreateCategory(dogs);

            var localVideos = new FakeLocalVideos();
            int Add(string description, string media = null)
            {
                using var db = new TikTokDbContext();
                var video = new Video { VideoId = Guid.NewGuid().ToString("N"), Description = description, DuetFromId = "", Ratio = "" };
                db.Videos.Add(video);
                db.SaveChanges();
                if (media != null)
                {
                    var path = Path.Combine(TestEnvironment.Configuration.DownloadsFolder, $"{media}-{video.VideoId}.mp4");
                    File.WriteAllText(path, "x");
                    localVideos.Paths[video.VideoId] = path;
                }
                return video.Id;
            }

            foreach (var d in new[] { "Przepis na makaron z sosem pomidorowym #gotowanie", "Szybki obiad w 15 minut, przepis", "Pieczenie ciasta czekoladowego krok po kroku", "Zupa pomidorowa jak u babci" })
            {
                await categories.AddVideoToCategory(cooking, Add(d, "red"));
            }
            foreach (var d in new[] { "Mój pies uczy się nowej sztuczki", "Szczeniak golden retriever bawi się piłką", "Spacer z psem w parku", "Pies szczeka na listonosza 😂" })
            {
                await categories.AddVideoToCategory(dogs, Add(d, "green"));
            }
            var cookingCandidate = Add("Jak zrobić domową pizzę - przepis na ciasto", "red");
            var dogCandidate = Add("Labrador i jego pierwsza kąpiel w jeziorze", "green");
            var noise = new[] { "Nowy samochód sportowy na torze", "Tutorial makijażu na wieczór", "Trening na siłowni - nogi", "Wakacje w Grecji, plaża i morze",
                "Gram w nową grę komputerową", "Recenzja telefonu", "Taniec do nowej piosenki", "Remont łazienki", "Matematyka - zadanie z geometrii", "Koncert rockowy" }
                .Select(d => Add(d)).ToList();

            TestEnvironment.Configuration.AiLabelsEnabled = true;
            var service = new AiLabelingService(TestEnvironment.Configuration, new AiModelManager(TestEnvironment.Configuration), localVideos, new FakeFrameExtractor());
            service.Start();
            service.RequestRun(AiRunKind.Incremental, TimeSpan.Zero);

            var stopwatch = Stopwatch.StartNew();
            await Task.Delay(500);
            while (service.Status.State != AiLabelingState.Idle && service.Status.State != AiLabelingState.Error && stopwatch.Elapsed < TimeSpan.FromMinutes(3))
            {
                await Task.Delay(200);
            }
            TestContext.Out.WriteLine($"{service.Status} in {stopwatch.Elapsed}");
            Assert.That(service.Status.State, Is.EqualTo(AiLabelingState.Idle), service.Status.Message);

            var labels = await categories.GetVideoAiLabels();
            foreach (var (videoId, videoLabels) in labels)
            {
                TestContext.Out.WriteLine($"{videoId}: {string.Join(", ", videoLabels.Select(x => $"{x.Key} {x.Value:P0}"))}");
            }

            Assert.That(labels.GetValueOrDefault(cookingCandidate)?.Keys, Does.Contain(cooking));
            Assert.That(labels.GetValueOrDefault(dogCandidate)?.Keys, Does.Contain(dogs));
            Assert.That(labels.GetValueOrDefault(cookingCandidate)?.Keys, Does.Not.Contain(dogs));
            Assert.That(labels.GetValueOrDefault(dogCandidate)?.Keys, Does.Not.Contain(cooking));

            using var db2 = new TikTokDbContext();
            Assert.That(db2.VideoEmbeddings.Count(x => x.ImageVector != null), Is.GreaterThanOrEqualTo(10));
        }
    }
}
