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
}
