using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace MyTikTokBackup.Desktop.Controls;

public sealed record CategoryToggledEventArgs(CategoryItem Category, bool IsChecked);

/// <summary>
/// Lists categories as toggle buttons; checked state means the current video belongs to the category.
/// </summary>
public sealed partial class VideoCategoriesControl : UserControl
{
    public event EventHandler<CategoryToggledEventArgs> CategoryToggled;

    public VideoCategoriesControl()
    {
        InitializeComponent();
    }

    public IList<CategoryItem> Categories
    {
        get => (IList<CategoryItem>)GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    public static readonly DependencyProperty CategoriesProperty =
        DependencyProperty.Register(nameof(Categories), typeof(IList<CategoryItem>), typeof(VideoCategoriesControl), new PropertyMetadata(null));

    private void Category_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton { Tag: CategoryItem category } button)
        {
            CategoryToggled?.Invoke(this, new CategoryToggledEventArgs(category, button.IsChecked == true));
        }
    }
}
