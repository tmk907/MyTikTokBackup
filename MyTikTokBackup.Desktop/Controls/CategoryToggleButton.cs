using System;
using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace MyTikTokBackup.Desktop.Controls;

/// <summary>
/// ToggleButton showing a category: a colored strip when unchecked, the category color as background when checked.
/// </summary>
public sealed partial class CategoryToggleButton : ToggleButton
{
    private readonly SolidColorBrush _colorBrush = new(Colors.Gray);
    private readonly SolidColorBrush _pointerOverBrush = new(Colors.Gray);
    private readonly SolidColorBrush _pressedBrush = new(Colors.Gray);
    private readonly SolidColorBrush _foregroundBrush = new(Colors.White);
    private readonly TextBlock _textBlock = new() { VerticalAlignment = VerticalAlignment.Center };

    public CategoryToggleButton()
    {
        MinWidth = 0;
        Padding = new Thickness(8, 4, 10, 5);
        CornerRadius = new CornerRadius(8);

        // Lightweight styling: the template resolves these keys from the control's own resources.
        Resources["ToggleButtonBackgroundChecked"] = _colorBrush;
        Resources["ToggleButtonBackgroundCheckedPointerOver"] = _pointerOverBrush;
        Resources["ToggleButtonBackgroundCheckedPressed"] = _pressedBrush;
        Resources["ToggleButtonBorderBrushChecked"] = _colorBrush;
        Resources["ToggleButtonBorderBrushCheckedPointerOver"] = _pointerOverBrush;
        Resources["ToggleButtonBorderBrushCheckedPressed"] = _pressedBrush;
        Resources["ToggleButtonForegroundChecked"] = _foregroundBrush;
        Resources["ToggleButtonForegroundCheckedPointerOver"] = _foregroundBrush;
        Resources["ToggleButtonForegroundCheckedPressed"] = _foregroundBrush;

        var strip = new Border
        {
            Width = 4,
            Height = 16,
            CornerRadius = new CornerRadius(2),
            Background = _colorBrush,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        panel.Children.Add(strip);
        panel.Children.Add(_textBlock);
        Content = panel;
    }

    /// <summary>Prefix for the UI Automation id; the category name is appended.</summary>
    public string AutomationIdPrefix { get; set; } = "Category_";

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(CategoryToggleButton),
            new PropertyMetadata(null, (d, e) => ((CategoryToggleButton)d).OnTextChanged((string)e.NewValue)));

    /// <summary>Color in #AARRGGBB or #RRGGBB format.</summary>
    public string ColorHex
    {
        get => (string)GetValue(ColorHexProperty);
        set => SetValue(ColorHexProperty, value);
    }

    public static readonly DependencyProperty ColorHexProperty =
        DependencyProperty.Register(nameof(ColorHex), typeof(string), typeof(CategoryToggleButton),
            new PropertyMetadata(null, (d, e) => ((CategoryToggleButton)d).OnColorChanged((string)e.NewValue)));

    private void OnTextChanged(string text)
    {
        _textBlock.Text = text ?? string.Empty;
        AutomationProperties.SetName(this, text ?? string.Empty);
        AutomationProperties.SetAutomationId(this, AutomationIdPrefix + text);
    }

    private void OnColorChanged(string hex)
    {
        var color = ParseColor(hex);
        _colorBrush.Color = color;
        _pointerOverBrush.Color = Shade(color, 0.88);
        _pressedBrush.Color = Shade(color, 0.76);
        _foregroundBrush.Color = IsLight(color) ? Colors.Black : Colors.White;
    }

    public static Color ParseColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Colors.Gray;
        var value = hex.TrimStart('#');
        if (value.Length == 6) value = "FF" + value;
        if (value.Length != 8 || !uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
        {
            return Colors.Gray;
        }
        return Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
    }

    private static Color Shade(Color c, double factor) =>
        Color.FromArgb(c.A, (byte)(c.R * factor), (byte)(c.G * factor), (byte)(c.B * factor));

    private static bool IsLight(Color c) => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) > 150;
}
