using System.Collections.Generic;
using MyTikTokBackup.Core.AiLabels;

namespace MyTikTokBackup.Core.Messages
{
    /// <summary>User categories or video-category assignments changed.</summary>
    public record CategoriesChanged();

    /// <summary>AI labels changed for the given database video ids.</summary>
    public record AiLabelsChanged(IReadOnlyCollection<int> VideoIds);

    public record AiLabelingStatusChanged(AiLabelingStatus Status);
}
