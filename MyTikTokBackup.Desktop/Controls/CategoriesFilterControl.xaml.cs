using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace MyTikTokBackup.Desktop.Controls;

public sealed record CategoryRenameRequest(CategoryItem Category, string NewName);

/// <summary>
/// Lists categories as toggle buttons used to filter videos. Allows adding, renaming and deleting categories.
/// The control only collects user intent; the owner performs the actual changes.
/// </summary>
public sealed partial class CategoriesFilterControl : UserControl
{
    public event EventHandler FilterChanged;
    public event EventHandler<string> AddRequested;
    public event EventHandler<CategoryRenameRequest> RenameRequested;
    public event EventHandler<CategoryItem> DeleteRequested;

    public CategoriesFilterControl()
    {
        InitializeComponent();
    }

    public IList<CategoryItem> Categories
    {
        get => (IList<CategoryItem>)GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    public static readonly DependencyProperty CategoriesProperty =
        DependencyProperty.Register(nameof(Categories), typeof(IList<CategoryItem>), typeof(CategoriesFilterControl), new PropertyMetadata(null));

    private bool IsNameAvailable(string name, CategoryItem except = null) =>
        Categories?.Any(x => x != except && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)) != true;

    private void Category_Click(object sender, RoutedEventArgs e) => FilterChanged?.Invoke(this, EventArgs.Empty);

    private void AddCategoryFlyout_Opened(object sender, object e)
    {
        NewCategoryTextBox.Text = string.Empty;
        NewCategoryTextBox.Focus(FocusState.Programmatic);
    }

    private void NewCategoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var name = NewCategoryTextBox.Text.Trim();
        var isAvailable = IsNameAvailable(name);
        NewCategoryError.Visibility = isAvailable ? Visibility.Collapsed : Visibility.Visible;
        ConfirmAddCategoryButton.IsEnabled = name.Length > 0 && isAvailable;
    }

    private void NewCategoryTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && ConfirmAddCategoryButton.IsEnabled)
        {
            e.Handled = true;
            AddCategory();
        }
    }

    private void ConfirmAddCategory_Click(object sender, RoutedEventArgs e) => AddCategory();

    private void AddCategory()
    {
        var name = NewCategoryTextBox.Text.Trim();
        if (name.Length == 0 || !IsNameAvailable(name)) return;
        AddCategoryFlyout.Hide();
        AddRequested?.Invoke(this, name);
    }

    private async void Rename_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not CategoryItem category) return;

        var textBox = new TextBox
        {
            Header = "Name",
            Text = category.Name,
            SelectionStart = 0,
            SelectionLength = category.Name.Length,
        };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(textBox, "RenameCategoryTextBox");
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = $"Rename category \"{category.Name}\"",
            Content = textBox,
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        textBox.TextChanged += (_, _) =>
        {
            var name = textBox.Text.Trim();
            dialog.IsPrimaryButtonEnabled = name.Length > 0 && IsNameAvailable(name, category);
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var newName = textBox.Text.Trim();
        if (newName == category.Name) return;
        RenameRequested?.Invoke(this, new CategoryRenameRequest(category, newName));
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not CategoryItem category) return;

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Delete category?",
            Content = $"Category \"{category.Name}\" will be deleted and removed from all videos. Videos themselves are not deleted.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        DeleteRequested?.Invoke(this, category);
    }
}
