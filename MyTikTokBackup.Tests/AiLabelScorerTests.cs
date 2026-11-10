using System;
using System.Collections.Generic;
using System.Linq;
using MyTikTokBackup.Core.AiLabels;
using NUnit.Framework;

namespace MyTikTokBackup.Tests
{
    public class AiLabelScorerTests
    {
        private const int Dim = 64;

        private static float[] Normalize(float[] v)
        {
            VectorMath.Normalize(v);
            return v;
        }

        private static float[] RandomUnit(Random random)
        {
            return Normalize(Enumerable.Range(0, Dim).Select(_ => (float)(random.NextDouble() * 2 - 1)).ToArray());
        }

        private static float[] Noisy(float[] center, Random random, float noise)
        {
            return Normalize(center.Select(x => x + (float)(random.NextDouble() * 2 - 1) * noise).ToArray());
        }

        /// <summary>3 clusters with 10 user-assigned videos each, 30 unlabeled cluster videos and 100 noise videos.</summary>
        private static (List<AiVideoVectors> Videos, List<AiCategoryInput> Categories, Dictionary<int, int> Truth) CreateClusters(bool withImages = true, float noise = 0.15f)
        {
            var random = new Random(42);
            var centers = Enumerable.Range(0, 3).Select(_ => (Text: RandomUnit(random), Image: RandomUnit(random))).ToArray();
            var videos = new List<AiVideoVectors>();
            var positives = Enumerable.Range(0, 3).Select(_ => new List<int>()).ToArray();
            var truth = new Dictionary<int, int>();
            int id = 1;

            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 20; i++)
                {
                    videos.Add(new AiVideoVectors
                    {
                        VideoId = id,
                        Text = Noisy(centers[c].Text, random, noise),
                        Image = withImages ? Noisy(centers[c].Image, random, noise) : null,
                    });
                    if (i < 10) positives[c].Add(id); else truth[id] = c;
                    id++;
                }
            }
            for (int i = 0; i < 100; i++)
            {
                videos.Add(new AiVideoVectors { VideoId = id++, Text = RandomUnit(random), Image = withImages ? RandomUnit(random) : null });
            }

            var categories = Enumerable.Range(0, 3).Select(c => new AiCategoryInput
            {
                Name = $"C{c}",
                NameVector = RandomUnit(random), // names unrelated to content - few-shot signals must win
                Positives = positives[c],
            }).ToList();
            return (videos, categories, truth);
        }

        [Test]
        public void FewShot_LabelsClusterMembers_AndNotNoise()
        {
            var (videos, categories, truth) = CreateClusters();

            var results = AiLabelScorer.Score(videos, categories, new HashSet<(int, string)>());

            foreach (var (videoId, category) in truth)
            {
                Assert.That(results.Any(x => x.VideoId == videoId && x.CategoryName == $"C{category}"), $"video {videoId} not labeled C{category}");
            }
            var wrong = results.Count(x => !truth.TryGetValue(x.VideoId, out var c) || x.CategoryName != $"C{c}");
            Assert.That(wrong, Is.LessThanOrEqualTo(3), "too many false labels");
            Assert.That(results.All(x => x.Confidence >= 0.5f && x.Confidence <= 1f));
        }

        [Test]
        public void FewShot_WeakClusters_PreferPrecision()
        {
            // Threshold is the 25th percentile of leave-one-out scores of user-assigned videos,
            // so roughly a quarter of weakly similar videos is expected to stay unlabeled.
            var (videos, categories, truth) = CreateClusters(noise: 0.3f);

            var results = AiLabelScorer.Score(videos, categories, new HashSet<(int, string)>());

            var correct = truth.Count(t => results.Any(x => x.VideoId == t.Key && x.CategoryName == $"C{t.Value}"));
            var wrong = results.Count(x => !truth.TryGetValue(x.VideoId, out var c) || x.CategoryName != $"C{c}");
            Assert.That(correct, Is.GreaterThanOrEqualTo(truth.Count * 6 / 10));
            Assert.That(wrong, Is.LessThanOrEqualTo(2));
        }

        [Test]
        public void FewShot_WorksWithoutImageVectors()
        {
            var (videos, categories, truth) = CreateClusters(withImages: false);

            var results = AiLabelScorer.Score(videos, categories, new HashSet<(int, string)>());

            var correct = truth.Count(t => results.Any(x => x.VideoId == t.Key && x.CategoryName == $"C{t.Value}"));
            Assert.That(correct, Is.GreaterThanOrEqualTo(truth.Count * 9 / 10));
        }

        [Test]
        public void FewShot_KnnPath_LabelsClusterMembers()
        {
            var (videos, categories, truth) = CreateClusters();

            var results = AiLabelScorer.Score(videos, categories, new HashSet<(int, string)>(),
                new AiLabelingOptions { MinPositivesForClassifier = int.MaxValue });

            var correct = truth.Count(t => results.Any(x => x.VideoId == t.Key && x.CategoryName == $"C{t.Value}"));
            var wrong = results.Count(x => !truth.TryGetValue(x.VideoId, out var c) || x.CategoryName != $"C{c}");
            Assert.That(correct, Is.GreaterThanOrEqualTo(truth.Count * 9 / 10));
            Assert.That(wrong, Is.LessThanOrEqualTo(3));
        }

        [Test]
        public void Classifier_VideosMissingTextVector_AreNotOverScored()
        {
            // Regression: a single model with zeros for a missing vector ranked such videos above real members.
            var (videos, categories, truth) = CreateClusters();
            var random = new Random(3);
            int id = videos.Max(x => x.VideoId) + 1;
            var imageOnly = Enumerable.Range(0, 30).Select(_ => new AiVideoVectors { VideoId = id++, Image = RandomUnit(random) }).ToList();
            videos.AddRange(imageOnly);

            var results = AiLabelScorer.Score(videos, categories, new HashSet<(int, string)>());

            Assert.That(results.Count(x => imageOnly.Any(v => v.VideoId == x.VideoId)), Is.LessThanOrEqualTo(2));
            var correct = truth.Count(t => results.Any(x => x.VideoId == t.Key && x.CategoryName == $"C{t.Value}"));
            Assert.That(correct, Is.GreaterThanOrEqualTo(truth.Count * 9 / 10));
        }

        [Test]
        public void Classifier_NoSeparableSignal_ProducesNoLabels()
        {
            var random = new Random(5);
            var videos = Enumerable.Range(1, 300).Select(i => new AiVideoVectors { VideoId = i, Text = RandomUnit(random), Image = RandomUnit(random) }).ToList();
            var categories = Enumerable.Range(0, 3).Select(c => new AiCategoryInput
            {
                Name = $"C{c}",
                NameVector = RandomUnit(random),
                Positives = Enumerable.Range(1 + c * 30, 30).ToList(),
            }).ToList();

            var results = AiLabelScorer.Score(videos, categories, new HashSet<(int, string)>());

            Assert.That(results.Count, Is.LessThanOrEqualTo(10));
        }

        [Test]
        public void PrecisionThreshold_IsLowestScoreReachingTargetPrecision()
        {
            var scores = new List<(float, bool, float)>
            {
                (0.9f, true, 1), (0.8f, true, 1), (0.7f, false, 1), (0.6f, true, 1), (0.5f, false, 1), (0.4f, false, 1), (0.3f, true, 1), (0.2f, false, 1),
            };

            Assert.That(AiLinearClassifier.PrecisionThreshold(scores, 0.75f, 1), Is.EqualTo(0.6f)); // 3 of 4
            Assert.That(AiLinearClassifier.PrecisionThreshold(scores, 0.55f, 1), Is.EqualTo(0.3f)); // 4 of 7
            Assert.That(AiLinearClassifier.PrecisionThreshold(scores, 0.75f, 4), Is.EqualTo(float.PositiveInfinity));

            // Weighted negatives: 4 positives and 3 negatives with weight 0.25 = 84% precision;
            // the cut is at the last positive, the negative below it would add only a false label.
            var weighted = scores.Select(x => (x.Item1, x.Item2, x.Item2 ? 1f : 0.25f)).ToList();
            Assert.That(AiLinearClassifier.PrecisionThreshold(weighted, 0.75f, 1), Is.EqualTo(0.3f));
        }

        [Test]
        public void AssignedAndRejectedPairs_AreNotLabeled()
        {
            var (videos, categories, truth) = CreateClusters();
            var rejectedVideo = truth.First(x => x.Value == 0).Key;

            var results = AiLabelScorer.Score(videos, categories, new HashSet<(int, string)> { (rejectedVideo, "C0") });

            Assert.That(results.Any(x => x.VideoId == rejectedVideo && x.CategoryName == "C0"), Is.False);
            foreach (var category in categories)
            {
                Assert.That(results.Any(x => x.CategoryName == category.Name && category.Positives.Contains(x.VideoId)), Is.False);
            }
        }

        [Test]
        public void LabelsPerVideo_AreCapped()
        {
            var random = new Random(1);
            var center = RandomUnit(random);
            var videos = Enumerable.Range(1, 40).Select(i => new AiVideoVectors { VideoId = i, Text = Noisy(center, random, 0.2f) }).ToList();
            videos.AddRange(Enumerable.Range(41, 100).Select(i => new AiVideoVectors { VideoId = i, Text = RandomUnit(random) }));
            // 5 categories with the same content
            var categories = Enumerable.Range(0, 5).Select(c => new AiCategoryInput
            {
                Name = $"C{c}",
                NameVector = RandomUnit(random),
                Positives = Enumerable.Range(1 + c * 5, 5).ToList(),
            }).ToList();

            var results = AiLabelScorer.Score(videos, categories, new HashSet<(int, string)>(), new AiLabelingOptions { MaxLabelsPerVideo = 2 });

            Assert.That(results.GroupBy(x => x.VideoId).Max(g => g.Count()), Is.LessThanOrEqualTo(2));
            Assert.That(results.Any(x => x.VideoId > 25 && x.VideoId <= 40));
        }

        [Test]
        public void ZeroShot_LabelsOnlyBestMatchingCategory()
        {
            var random = new Random(7);
            var nameA = RandomUnit(random);
            var nameB = RandomUnit(random);
            var videos = new List<AiVideoVectors>
            {
                new AiVideoVectors { VideoId = 1, Text = Noisy(nameA, random, 0.1f) },
                new AiVideoVectors { VideoId = 2, Text = Noisy(nameB, random, 0.1f) },
            };
            videos.AddRange(Enumerable.Range(3, 200).Select(i => new AiVideoVectors { VideoId = i, Text = RandomUnit(random) }));
            var categories = new List<AiCategoryInput>
            {
                new AiCategoryInput { Name = "A", NameVector = nameA, Positives = new List<int>() },
                new AiCategoryInput { Name = "B", NameVector = nameB, Positives = new List<int> { 50 } }, // fewer than 3 positives => zero-shot
            };

            var results = AiLabelScorer.Score(videos, categories, new HashSet<(int, string)>());

            Assert.That(results.Single(x => x.VideoId == 1).CategoryName, Is.EqualTo("A"));
            Assert.That(results.Single(x => x.VideoId == 2).CategoryName, Is.EqualTo("B"));
            Assert.That(results.Count(x => x.VideoId > 2), Is.LessThanOrEqualTo(2));
        }

        [Test]
        public void IdenticalVectors_ProduceNoLabels()
        {
            var vector = Normalize(Enumerable.Repeat(1f, Dim).ToArray());
            var videos = Enumerable.Range(1, 20).Select(i => new AiVideoVectors { VideoId = i, Text = vector, Image = vector }).ToList();
            var categories = new List<AiCategoryInput>
            {
                new AiCategoryInput { Name = "A", NameVector = vector, Positives = new List<int> { 1, 2, 3, 4 } },
            };

            var results = AiLabelScorer.Score(videos, categories, new HashSet<(int, string)>());

            Assert.That(results, Is.Empty);
        }

        [Test]
        public void Knn_ExcludesSelf()
        {
            var a = Normalize(new float[] { 1, 0 });
            var b = Normalize(new float[] { 0, 1 });
            var videos = new List<AiVideoVectors>
            {
                new AiVideoVectors { VideoId = 1, Text = a },
                new AiVideoVectors { VideoId = 2, Text = b },
            };

            var knn = AiLabelScorer.Knn(videos, new[] { 0, 1 }, v => v.Text, 3);

            Assert.That(knn[0], Is.EqualTo(0f).Within(1e-6));
            Assert.That(knn[1], Is.EqualTo(0f).Within(1e-6));
        }

        [Test]
        public void Standardize_UsesMedianAndMadOfCandidates()
        {
            var raw = new float[] { 1, 2, 3, 4, 5, 100 };
            var isPositive = new[] { false, false, false, false, false, true };

            var result = AiLabelScorer.Standardize(raw, isPositive);

            // median 3, MAD 1
            Assert.That(result[2], Is.EqualTo(0f).Within(1e-5));
            Assert.That(result[4], Is.EqualTo(2f / 1.4826f).Within(1e-4));
            Assert.That(result[5], Is.EqualTo(97f / 1.4826f).Within(1e-3));
        }

        [Test]
        public void Percentile_Interpolates()
        {
            Assert.That(AiLabelScorer.Percentile(new List<float> { 4, 1, 3, 2 }, 0.5f), Is.EqualTo(2.5f));
            Assert.That(AiLabelScorer.Percentile(new List<float> { 1, 2, 3, 4, 5 }, 0.25f), Is.EqualTo(2f));
        }
    }
}
