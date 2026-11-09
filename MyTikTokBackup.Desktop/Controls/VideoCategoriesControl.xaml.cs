using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace MyTikTokBackup.Desktop.Controls;

public sealed record CategoryToggledEventArgs(CategoryItem Category, bool IsChecked);

/// <summary>
/// Lists categories as toggle buttons; checked state means the current video belongs to the category.
/// AI labels are shown with a badge; clicking one accepts it, the context menu rejects it.
/// </summary>
public sealed partial class VideoCategoriesControl : UserControl
{
    public event EventHandler<CategoryToggledEventArgs> CategoryToggled;
    public event EventHandler<CategoryItem> AiLabelRejected;

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

    private void Category_ContextRequested(UIElement sender, ContextRequestedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: CategoryItem { IsAiSuggested: true } category } element) return;

        var reject = new MenuFlyoutItem { Text = "Reject AI label", Icon = new SymbolIcon(Symbol.Cancel) };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(reject, "RejectAiLabelMenuItem");
        reject.Click += (s, a) => AiLabelRejected?.Invoke(this, category);
        var flyout = new MenuFlyout();
        flyout.Items.Add(reject);

        if (e.TryGetPosition(element, out var point))
        {
            flyout.ShowAt(element, point);
        }
        else
        {
            flyout.ShowAt(element);
        }
        e.Handled = true;
    }
}
