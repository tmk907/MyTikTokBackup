using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics.Tensors;
using System.Threading;

namespace MyTikTokBackup.Core.AiLabels
{
    /// <summary>
    /// Per-category logistic regression on the embeddings (a linear probe), trained on the videos the user assigned.
    /// Separate models are used for videos with text and image vectors, text only and image only,
    /// because a missing vector can't be represented by zeros without biasing the score.
    /// The threshold of each model is chosen by cross-validation on user-labeled videos to reach the target precision.
    /// </summary>
    public static class AiLinearClassifier
    {
        private enum Modality { Full, Text, Image }

        private static readonly Modality[] Modalities = { Modality.Full, Modality.Text, Modality.Image };

        /// <param name="isPositive">Videos assigned to the category.</param>
        /// <param name="isLabeled">Videos assigned to any category - negatives with a reliable label.</param>
        /// <param name="unlabeledSample">Sample of videos without categories, used as weak negatives.</param>
        /// <returns>Score minus threshold for each candidate (positive = label), NaN when the video can't be scored.</returns>
        public static float[] Margins(
            IReadOnlyList<AiVideoVectors> videos,
            bool[] isPositive,
            bool[] isLabeled,
            IReadOnlyList<int> unlabeledSample,
            AiLabelingOptions options,
            CancellationToken cancellationToken = default)
        {
            int n = videos.Count;
            var margins = new float[n];
            Array.Fill(margins, float.NaN);

            foreach (var modality in Modalities)
            {
                // Candidates scored by this model: Full when both vectors exist, otherwise the single available one.
                var candidates = Enumerable.Range(0, n).Where(i => !isPositive[i] && ModalityOf(videos[i]) == modality).ToList();
                if (candidates.Count == 0) continue;

                var labeled = Enumerable.Range(0, n).Where(i => isLabeled[i] && Has(videos[i], modality)).ToList();
                if (labeled.Count(i => isPositive[i]) < options.MinPositivesForClassifier) continue;
                var unlabeled = unlabeledSample.Where(i => Has(videos[i], modality)).ToList();

                var (threshold, models, outOfFold) = CrossValidate(videos, modality, labeled, unlabeled, isPositive, options, cancellationToken);
                if (!float.IsFinite(threshold)) continue;

                // Candidates are scored by the mean of the fold models, the same models the threshold was measured with.
                // Sampled videos were weak negatives in training, so they use the score of the model that didn't see them.
                foreach (var i in candidates)
                {
                    var score = outOfFold.TryGetValue(i, out var s) ? s : models.Average(m => m.Predict(videos[i], modality));
                    margins[i] = score - threshold;
                }
            }
            return margins;
        }

        private static Modality? ModalityOf(AiVideoVectors video)
        {
            if (video.Text != null && video.Image != null) return Modality.Full;
            if (video.Text != null) return Modality.Text;
            if (video.Image != null) return Modality.Image;
            return null;
        }

        private static bool Has(AiVideoVectors video, Modality modality) => modality switch
        {
            Modality.Full => video.Text != null && video.Image != null,
            Modality.Text => video.Text != null,
            _ => video.Image != null,
        };

        /// <summary>
        /// Lowest score at which the precision of held-out videos above it is still at least the target.
        /// Videos without categories count as weighted false positives - otherwise a user with few categories
        /// (few labeled negatives) would get almost every video labeled.
        /// Infinity when the target can't be reached - the model then gives no labels.
        /// </summary>
        private static (float Threshold, List<Model> Models, Dictionary<int, float> OutOfFold) CrossValidate(IReadOnlyList<AiVideoVectors> videos, Modality modality,
            List<int> labeled, List<int> unlabeled, bool[] isPositive, AiLabelingOptions options, CancellationToken cancellationToken)
        {
            var models = new List<Model>(options.ClassifierFolds);
            var outOfFold = new Dictionary<int, float>(unlabeled.Count);
            var heldOut = new List<(float Score, bool Positive, float Weight)>(labeled.Count + unlabeled.Count);
            for (int fold = 0; fold < options.ClassifierFolds; fold++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var train = labeled.Where(i => Fold(videos[i], options) != fold).ToList();
                var test = labeled.Where(i => Fold(videos[i], options) == fold).ToList();
                if (test.Count == 0 || !train.Any(i => isPositive[i])) continue;
                var trainUnlabeled = unlabeled.Where(i => Fold(videos[i], options) != fold).ToList();
                var testUnlabeled = unlabeled.Where(i => Fold(videos[i], options) == fold);

                var model = Train(videos, modality, train, trainUnlabeled, isPositive, options);
                models.Add(model);
                heldOut.AddRange(test.Select(i => (model.Predict(videos[i], modality), isPositive[i], 1f)));
                foreach (var i in testUnlabeled)
                {
                    var score = model.Predict(videos[i], modality);
                    outOfFold[i] = score;
                    heldOut.Add((score, false, options.UnlabeledNegativeWeight));
                }
            }
            return (PrecisionThreshold(heldOut, options.TargetPrecision, options.MinTruePositives), models, outOfFold);
        }

        /// <param name="scores">Weight applies to negatives; positives always count as 1.</param>
        public static float PrecisionThreshold(List<(float Score, bool Positive, float Weight)> scores, float targetPrecision, int minTruePositives)
        {
            var sorted = scores.OrderByDescending(x => x.Score).ToList();
            var threshold = float.PositiveInfinity;
            int truePositives = 0;
            double falsePositives = 0;
            for (int k = 0; k < sorted.Count; k++)
            {
                if (sorted[k].Positive) truePositives++; else falsePositives += sorted[k].Weight;
                // Ties must not split - the threshold is inclusive.
                if (k + 1 < sorted.Count && sorted[k + 1].Score == sorted[k].Score) continue;
                // Cut only at a positive: going lower without gaining a positive adds only false labels.
                if (!sorted[k].Positive) continue;
                if (truePositives >= minTruePositives && truePositives >= targetPrecision * (truePositives + falsePositives))
                {
                    threshold = sorted[k].Score;
                }
            }
            return threshold;
        }

        private static int Fold(AiVideoVectors video, AiLabelingOptions options) => (int)((uint)video.VideoId * 2654435761u % (uint)options.ClassifierFolds);

        private static Model Train(IReadOnlyList<AiVideoVectors> videos, Modality modality,
            List<int> labeled, List<int> unlabeled, bool[] isPositive, AiLabelingOptions options)
        {
            var samples = new List<(int Index, float Label, float Weight)>(labeled.Count + unlabeled.Count);
            samples.AddRange(labeled.Select(i => (i, isPositive[i] ? 1f : 0f, isPositive[i] ? 1f : options.LabeledNegativeWeight)));
            samples.AddRange(unlabeled.Select(i => (i, 0f, options.UnlabeledNegativeWeight)));
            var totalWeight = samples.Sum(x => x.Weight);

            var first = videos[samples[0].Index];
            var model = new Model(
                modality != Modality.Image ? first.Text.Length : 0,
                modality != Modality.Text ? first.Image.Length : 0);
            var gradientText = new float[model.Text.Length];
            var gradientImage = new float[model.Image.Length];

            // Full-batch gradient descent: the inputs are unit vectors, so a fixed learning rate converges well.
            for (int epoch = 0; epoch < options.ClassifierEpochs; epoch++)
            {
                Array.Clear(gradientText);
                Array.Clear(gradientImage);
                double gradientBias = 0;
                foreach (var (index, label, weight) in samples)
                {
                    var video = videos[index];
                    var z = model.Predict(video, modality);
                    var error = (1f / (1f + MathF.Exp(-z)) - label) * weight / totalWeight;
                    if (gradientText.Length > 0) TensorPrimitives.MultiplyAdd(video.Text, error, gradientText, gradientText);
                    if (gradientImage.Length > 0) TensorPrimitives.MultiplyAdd(video.Image, error, gradientImage, gradientImage);
                    gradientBias += error;
                }
                model.Step(gradientText, gradientImage, (float)gradientBias, options.ClassifierLearningRate, options.ClassifierL2);
            }
            return model;
        }

        private sealed class Model
        {
            public readonly float[] Text;
            public readonly float[] Image;
            private float _bias;

            public Model(int textSize, int imageSize)
            {
                Text = new float[textSize];
                Image = new float[imageSize];
            }

            public float Predict(AiVideoVectors video, Modality modality)
            {
                var z = _bias;
                if (modality != Modality.Image) z += TensorPrimitives.Dot(Text, video.Text);
                if (modality != Modality.Text) z += TensorPrimitives.Dot(Image, video.Image);
                return z;
            }

            public void Step(float[] gradientText, float[] gradientImage, float gradientBias, float learningRate, float l2)
            {
                Update(Text, gradientText, learningRate, l2);
                Update(Image, gradientImage, learningRate, l2);
                _bias -= learningRate * gradientBias;
            }

            private static void Update(float[] weights, float[] gradient, float learningRate, float l2)
            {
                // w -= lr * (gradient + l2 * w)
                TensorPrimitives.MultiplyAdd(weights, l2, gradient, gradient);
                TensorPrimitives.MultiplyAdd(gradient, -learningRate, weights, weights);
            }
        }
    }
}
