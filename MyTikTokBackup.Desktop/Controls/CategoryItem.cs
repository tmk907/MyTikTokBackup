using CommunityToolkit.Mvvm.ComponentModel;

namespace MyTikTokBackup.Desktop.Controls;

/// <summary>
/// Category shown on the profile videos page. One instance backs both the filter list and the
/// "assign to current video" list, so renames/color changes show up in both places.
/// </summary>
public partial class CategoryItem : ObservableObject
{
    private string _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string _color;
    public string Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }

    private bool _isFilterSelected;
    /// <summary>Category is selected in the videos filter.</summary>
    public bool IsFilterSelected
    {
        get => _isFilterSelected;
        set => SetProperty(ref _isFilterSelected, value);
    }

    private bool _isAssigned;
    /// <summary>Currently selected video belongs to this category.</summary>
    public bool IsAssigned
    {
        get => _isAssigned;
        set => SetProperty(ref _isAssigned, value);
    }

    private bool _isAiSuggested;
    /// <summary>AI suggests this category for the currently selected video.</summary>
    public bool IsAiSuggested
    {
        get => _isAiSuggested;
        set
        {
            if (SetProperty(ref _isAiSuggested, value)) OnPropertyChanged(nameof(ToolTip));
        }
    }

    private float _aiConfidence;
    public float AiConfidence
    {
        get => _aiConfidence;
        set
        {
            if (SetProperty(ref _aiConfidence, value)) OnPropertyChanged(nameof(ToolTip));
        }
    }

    public string ToolTip => IsAiSuggested
        ? $"AI label ({AiConfidence:P0} confidence). Click to add the video to this category, right-click to reject."
        : "Add or remove the selected video from this category";
}
