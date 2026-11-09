namespace MyTikTokBackup.Core.AiLabels
{
    public enum AiLabelingState
    {
        Disabled,
        Idle,
        Downloading,
        EmbeddingText,
        EmbeddingImages,
        Scoring,
        Error,
    }

    public record AiLabelingStatus(AiLabelingState State, long Done, long Total, string Message)
    {
        public bool IsRunning => State is AiLabelingState.Downloading or AiLabelingState.EmbeddingText
            or AiLabelingState.EmbeddingImages or AiLabelingState.Scoring;
    }
}
