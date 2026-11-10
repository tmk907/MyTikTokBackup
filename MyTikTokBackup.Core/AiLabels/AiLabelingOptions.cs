using System;

namespace MyTikTokBackup.Core.AiLabels
{
    /// <summary>Tuning constants of <see cref="AiLabelScorer"/>.</summary>
    public record AiLabelingOptions
    {
        /// <summary>Categories with fewer user-assigned videos use only zero-shot matching to the category name.</summary>
        public int MinPositivesForFewShot { get; init; } = 3;

        /// <summary>Categories with at least this many user-assigned videos use <see cref="AiLinearClassifier"/> instead of kNN.</summary>
        public int MinPositivesForClassifier { get; init; } = 10;

        /// <summary>Number of nearest user-assigned videos averaged by the kNN signals.</summary>
        public int NearestNeighbors { get; init; } = 3;

        public int MaxLabelsPerVideo { get; init; } = 3;

        public float FewShotTextWeight { get; init; } = 0.35f;
        public float FewShotImageWeight { get; init; } = 0.35f;
        public float FewShotNameTextWeight { get; init; } = 0.15f;
        public float FewShotNameImageWeight { get; init; } = 0.15f;

        /// <summary>Threshold (standardized score) of few-shot categories = this percentile of leave-one-out scores of user-assigned videos.</summary>
        public float FewShotThresholdPercentile { get; init; } = 0.25f;
        public float MinThreshold { get; init; } = 1.5f;
        public float MaxThreshold { get; init; } = 3.0f;

        /// <summary>Threshold (standardized score) of zero-shot categories.</summary>
        public float ZeroShotThreshold { get; init; } = 3.0f;

        /// <summary>Classifier threshold: expected share of correct labels, estimated by cross-validation on user-labeled videos.</summary>
        public float TargetPrecision { get; init; } = 0.5f;
        /// <summary>Minimum correct held-out labels needed to trust a classifier threshold.</summary>
        public int MinTruePositives { get; init; } = 3;
        public int ClassifierFolds { get; init; } = 5;
        public int ClassifierEpochs { get; init; } = 150;
        public float ClassifierLearningRate { get; init; } = 2f;
        public float ClassifierL2 { get; init; } = 1e-3f;
        /// <summary>Videos assigned to other categories are negatives, but users often assign only one of several fitting categories.</summary>
        public float LabeledNegativeWeight { get; init; } = 0.5f;
        /// <summary>Videos without categories are weak negatives - most of them don't belong to the category.</summary>
        public float UnlabeledNegativeWeight { get; init; } = 0.2f;
        public int UnlabeledSamples { get; init; } = 2000;
        public int MaxParallelism { get; init; } = 2;

        public static AiLabelingOptions Default { get; } = new AiLabelingOptions();

        /// <summary>Options adjusted by the user setting: 0 = fewer, more accurate labels, 1 = more labels; 0.5 = <see cref="Default"/>.</summary>
        public static AiLabelingOptions ForSensitivity(double sensitivity)
        {
            var s = (float)Math.Clamp(sensitivity, 0, 1);
            return Default with
            {
                TargetPrecision = 0.75f - 0.5f * s,
                ZeroShotThreshold = 3.75f - 1.5f * s,
                MinThreshold = 2.0f - 1.0f * s,
                MaxThreshold = 3.5f - 1.0f * s,
            };
        }
    }
}
