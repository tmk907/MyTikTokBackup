using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyTikTokBackup.Core.AiLabels
{
    public sealed class AiVideoVectors
    {
        /// <summary>Database video id.</summary>
        public int VideoId { get; init; }
        /// <summary>L2-normalized text embedding or null.</summary>
        public float[] Text { get; init; }
        /// <summary>L2-normalized image embedding or null.</summary>
        public float[] Image { get; init; }
    }

    public sealed class AiCategoryInput
    {
        public string Name { get; init; }
        /// <summary>L2-normalized embedding of the category name.</summary>
        public float[] NameVector { get; init; }
        /// <summary>Database ids of videos the user assigned to the category.</summary>
        public IReadOnlyCollection<int> Positives { get; init; }
    }

    /// <param name="Confidence">sigmoid(score - threshold), in [0.5, 1).</param>
    public record AiLabelResult(int VideoId, string CategoryName, float Confidence);

    /// <summary>
    /// Scores (video, category) pairs. Categories with enough user-assigned videos use <see cref="AiLinearClassifier"/>.
    /// Smaller ones combine kNN similarity to videos the user assigned to the category (text and image) and
    /// zero-shot similarity to the category name (text and image).
    /// Each signal is standardized per category (median / MAD over candidate videos) so they can be combined,
    /// and the threshold of a category is derived from leave-one-out scores of its user-assigned videos.
    /// </summary>
    public static class AiLabelScorer
    {
        private const float MinScale = 1e-3f;

        public static List<AiLabelResult> Score(
            IReadOnlyList<AiVideoVectors> videos,
            IReadOnlyList<AiCategoryInput> categories,
            IReadOnlySet<(int VideoId, string CategoryName)> rejected,
            AiLabelingOptions options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= AiLabelingOptions.Default;
            int n = videos.Count;
            var index = new Dictionary<int, int>(n);
            for (int i = 0; i < n; i++)
            {
                index[videos[i].VideoId] = i;
            }

            var scores = new float[categories.Count][];
            var zeroShot = new float[categories.Count][];
            var thresholds = new float[categories.Count];
            var fewShot = new bool[categories.Count];

            // Videos with any user category are reliable negatives of the other categories,
            // a stable sample of the remaining videos is used as weak negatives.
            var isLabeled = new bool[n];
            foreach (var category in categories)
            {
                foreach (var id in category.Positives ?? Array.Empty<int>())
                {
                    if (index.TryGetValue(id, out var i)) isLabeled[i] = true;
                }
            }
            var unlabeledSample = Enumerable.Range(0, n)
                .Where(i => !isLabeled[i])
                .OrderBy(i => (uint)videos[i].VideoId * 2246822519u)
                .Take(options.UnlabeledSamples)
                .ToList();

            Parallel.For(0, categories.Count, new ParallelOptions { MaxDegreeOfParallelism = options.MaxParallelism, CancellationToken = cancellationToken }, c =>
            {
                var category = categories[c];
                var positives = (category.Positives ?? Array.Empty<int>())
                    .Where(index.ContainsKey).Select(x => index[x]).Distinct().ToArray();
                var isPositive = new bool[n];
                foreach (var p in positives) isPositive[p] = true;

                var textPositives = positives.Where(p => videos[p].Text != null).ToArray();
                var imagePositives = positives.Where(p => videos[p].Image != null).ToArray();
                fewShot[c] = positives.Count(p => videos[p].Text != null || videos[p].Image != null) >= options.MinPositivesForFewShot;

                var zT = Standardize(ZeroShot(videos, category.NameVector, v => v.Text), isPositive);
                var zI = Standardize(ZeroShot(videos, category.NameVector, v => v.Image), isPositive);
                zeroShot[c] = new float[n];
                for (int i = 0; i < n; i++)
                {
                    zeroShot[c][i] = Combine(zT[i], 0.5f, zI[i], 0.5f);
                }

                var combined = new float[n];
                if (positives.Length >= options.MinPositivesForClassifier)
                {
                    combined = AiLinearClassifier.Margins(videos, isPositive, isLabeled, unlabeledSample, options, cancellationToken);
                    thresholds[c] = 0;
                }
                else if (fewShot[c])
                {
                    var kT = Standardize(Knn(videos, textPositives, v => v.Text, options.NearestNeighbors), isPositive);
                    var kI = Standardize(Knn(videos, imagePositives, v => v.Image, options.NearestNeighbors), isPositive);
                    // Category names like "Favorites" say nothing about the content -
                    // use the name only as much as it separates the user-assigned videos.
                    var nameTextWeight = options.FewShotNameTextWeight * NameRelevance(zT, positives);
                    var nameImageWeight = options.FewShotNameImageWeight * NameRelevance(zI, positives);
                    for (int i = 0; i < n; i++)
                    {
                        combined[i] = Combine(
                            kT[i], options.FewShotTextWeight,
                            kI[i], options.FewShotImageWeight,
                            zT[i], nameTextWeight,
                            zI[i], nameImageWeight);
                    }

                    var looScores = positives.Select(p => combined[p]).Where(float.IsFinite).ToList();
                    thresholds[c] = looScores.Count == 0
                        ? options.MaxThreshold
                        : Math.Clamp(Percentile(looScores, options.FewShotThresholdPercentile), options.MinThreshold, options.MaxThreshold);
                }
                else
                {
                    Array.Copy(zeroShot[c], combined, n);
                    thresholds[c] = options.ZeroShotThreshold;
                }

                // Videos already assigned by the user are not candidates.
                foreach (var p in positives) combined[p] = float.NaN;
                scores[c] = combined;
            });

            var results = new List<AiLabelResult>();
            var candidates = new List<(int Category, float Margin)>();
            for (int i = 0; i < n; i++)
            {
                candidates.Clear();
                var bestZeroShot = float.NegativeInfinity;
                for (int c = 0; c < categories.Count; c++)
                {
                    if (float.IsFinite(zeroShot[c][i]) && float.IsFinite(scores[c][i])) bestZeroShot = Math.Max(bestZeroShot, zeroShot[c][i]);
                }

                for (int c = 0; c < categories.Count; c++)
                {
                    var score = scores[c][i];
                    if (!float.IsFinite(score) || score < thresholds[c]) continue;
                    // Zero-shot on names is noisy - accept only the best matching category.
                    if (!fewShot[c] && zeroShot[c][i] < bestZeroShot) continue;
                    if (rejected != null && rejected.Contains((videos[i].VideoId, categories[c].Name))) continue;
                    candidates.Add((c, score - thresholds[c]));
                }

                foreach (var candidate in candidates.OrderByDescending(x => x.Margin).Take(options.MaxLabelsPerVideo))
                {
                    var confidence = 1f / (1f + MathF.Exp(-candidate.Margin));
                    results.Add(new AiLabelResult(videos[i].VideoId, categories[candidate.Category].Name, confidence));
                }
            }
            return results;
        }

        /// <summary>Cosine similarity to the category name, NaN when the video has no such vector.</summary>
        private static float[] ZeroShot(IReadOnlyList<AiVideoVectors> videos, float[] nameVector, Func<AiVideoVectors, float[]> selector)
        {
            var result = new float[videos.Count];
            for (int i = 0; i < videos.Count; i++)
            {
                var vector = selector(videos[i]);
                result[i] = vector == null || nameVector == null ? float.NaN : VectorMath.Dot(vector, nameVector);
            }
            return result;
        }

        /// <summary>
        /// Mean of the top-k cosine similarities to the positives. A positive is never compared with itself (leave-one-out).
        /// </summary>
        public static float[] Knn(IReadOnlyList<AiVideoVectors> videos, int[] positives, Func<AiVideoVectors, float[]> selector, int k)
        {
            var result = new float[videos.Count];
            var top = new float[k];
            for (int i = 0; i < videos.Count; i++)
            {
                var vector = selector(videos[i]);
                if (vector == null)
                {
                    result[i] = float.NaN;
                    continue;
                }

                int found = 0;
                foreach (var p in positives)
                {
                    if (p == i) continue;
                    var similarity = VectorMath.Dot(vector, selector(videos[p]));
                    if (found < k)
                    {
                        top[found++] = similarity;
                    }
                    else
                    {
                        var min = 0;
                        for (int j = 1; j < k; j++) if (top[j] < top[min]) min = j;
                        if (similarity > top[min]) top[min] = similarity;
                    }
                }

                result[i] = found == 0 ? float.NaN : top.AsSpan(0, found).ToArray().Average();
            }
            return result;
        }

        /// <summary>Robust standardization (median / MAD) using statistics of candidate (non-positive) videos.</summary>
        public static float[] Standardize(float[] raw, bool[] isPositive)
        {
            var values = new List<float>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                if (!isPositive[i] && float.IsFinite(raw[i])) values.Add(raw[i]);
            }

            var result = new float[raw.Length];
            if (values.Count < 2)
            {
                Array.Fill(result, float.NaN);
                return result;
            }

            var median = Percentile(values, 0.5f);
            var mad = Percentile(values.Select(x => Math.Abs(x - median)).ToList(), 0.5f);
            var scale = Math.Max(1.4826f * mad, MinScale);
            for (int i = 0; i < raw.Length; i++)
            {
                result[i] = (raw[i] - median) / scale;
            }
            return result;
        }

        /// <summary>
        /// 0..1 - how much the user-assigned videos stand out in a standardized zero-shot signal
        /// (median of 2 or more standard deviations = fully relevant).
        /// </summary>
        private static float NameRelevance(float[] standardized, int[] positives)
        {
            var values = positives.Select(p => standardized[p]).Where(float.IsFinite).ToList();
            if (values.Count == 0) return 0;
            return Math.Clamp(Percentile(values, 0.5f) / 2f, 0f, 1f);
        }

        /// <summary>Weighted mean of the finite values; weights of missing values are dropped.</summary>
        private static float Combine(params float[] valuesAndWeights)
        {
            float sum = 0, weights = 0;
            for (int j = 0; j < valuesAndWeights.Length; j += 2)
            {
                var value = valuesAndWeights[j];
                var weight = valuesAndWeights[j + 1];
                if (!float.IsFinite(value) || weight <= 0) continue;
                sum += value * weight;
                weights += weight;
            }
            return weights == 0 ? float.NaN : sum / weights;
        }

        public static float Percentile(List<float> values, float percentile)
        {
            var sorted = values.OrderBy(x => x).ToArray();
            var position = percentile * (sorted.Length - 1);
            var lower = (int)Math.Floor(position);
            var upper = (int)Math.Ceiling(position);
            return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
        }
    }
}
