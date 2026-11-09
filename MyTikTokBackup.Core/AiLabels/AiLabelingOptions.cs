namespace MyTikTokBackup.Core.AiLabels
{
    /// <summary>Tuning constants of <see cref="AiLabelScorer"/>.</summary>
    public record AiLabelingOptions
    {
        /// <summary>Categories with fewer user-assigned videos use only zero-shot matching to the category name.</summary>
        public int MinPositivesForFewShot { get; init; } = 3;

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

        public static AiLabelingOptions Default { get; } = new AiLabelingOptions();
    }
}
