using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace HanziOverlay.App.Windows;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed && e.ClickCount == 1)
        {
            try { DragMove(); } catch { /* ignore when button state not suitable */ }
        }
    }

    private void Line_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is TextBlock tb && !string.IsNullOrEmpty(tb.Text))
        {
            try { Clipboard.SetText(tb.Text); } catch { }
        }
    }

    private void CopyAll_Click(object sender, RoutedEventArgs e)
    {
        string cn = ChineseText.Text ?? "";
        string py = PinyinText.Text ?? "";
        string en = EnglishText.Text ?? "";
        string all = string.Join("\n", new[] { cn, py, en }.Where(s => !string.IsNullOrEmpty(s)));
        if (!string.IsNullOrEmpty(all))
        {
            try { Clipboard.SetText(all); } catch { }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SaveRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ToggleChinese_Click(object sender, RoutedEventArgs e)
    {
        ChineseText.Visibility = ChineseText.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        ToggleChineseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Opacity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is double opacity)
        {
            Opacity = Math.Clamp(opacity, 0.2, 1.0);
            OpacityChanged?.Invoke(this, opacity);
        }
    }

    public event EventHandler? SaveRequested;
    public event EventHandler? ToggleChineseRequested;
    public event EventHandler<double>? OpacityChanged;

    public void SetOpacity(double value) => Opacity = Math.Clamp(value, 0.2, 1.0);
    public void SetChineseVisible(bool visible) => ChineseText.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

    public void SetPinyin(string text) => PinyinText.Text = text ?? "";
    public void SetEnglish(string text) => EnglishText.Text = text ?? "";
    public void SetChinese(string text) => ChineseText.Text = text ?? "";
}
