using System.Windows;
using System.Windows.Interop;
using HanziOverlay.Core.Services.Hotkeys;

namespace HanziOverlay.App.Windows;

public partial class ControlWindow : Window
{
    private OverlayWindow? _overlayWindow;
    private GlobalHotkeyService _hotkeyService;
    private HwndSource? _hwndSource;

    public ControlWindow()
    {
        InitializeComponent();
        _hotkeyService = new GlobalHotkeyService();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _overlayWindow = new OverlayWindow();
        _overlayWindow.Show();
        _overlayWindow.SetOpacity(OpacitySlider.Value);
        _overlayWindow.SaveRequested += (_, _) => SaveCurrentLine();
        _overlayWindow.ToggleChineseRequested += (_, _) => { /* state tracked elsewhere */ };
        _overlayWindow.OpacityChanged += (_, opacity) => OpacitySlider.Value = opacity;

        _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        if (_hwndSource != null)
        {
            _hwndSource.AddHook(WndProc);
            _hotkeyService.Register(_hwndSource.Handle);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _hotkeyService.Unregister();
        _overlayWindow?.Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _overlayWindow?.Close();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_hotkeyService.ProcessMessage(hwnd, msg, wParam, lParam, out handled))
            return IntPtr.Zero;
        return IntPtr.Zero;
    }

    private void OnHotkeyPressed(object? sender, HotkeyAction action)
    {
        Dispatcher.Invoke(() =>
        {
            switch (action)
            {
                case HotkeyAction.SelectRegion:
                    ShowRegionSelector();
                    break;
                case HotkeyAction.PauseResume:
                    TogglePauseResume();
                    break;
                case HotkeyAction.FreezeUnfreeze:
                    ToggleFreeze();
                    break;
                case HotkeyAction.ToggleChinese:
                    ToggleChinese();
                    break;
                case HotkeyAction.SaveLine:
                    SaveCurrentLine();
                    break;
                case HotkeyAction.ExportSaved:
                    ExportSaved();
                    break;
            }
        });
    }

    private void ShowRegionSelector()
    {
        var selector = new RegionSelectorWindow();
        selector.RegionSelected += (_, region) =>
        {
            // Store region for capture service (Phase 2)
            Application.Current.Properties["CaptureRegion"] = region;
        };
        selector.Show();
    }

    private void TogglePauseResume()
    {
        // Phase 2: toggle capture
        PauseResumeButton.Content = PauseResumeButton.Content?.ToString() == "Pause" ? "Resume" : "Pause";
    }

    private void ToggleFreeze()
    {
        // Phase 4: freeze overlay updates
        FreezeButton.Content = FreezeButton.Content?.ToString() == "Freeze" ? "Unfreeze" : "Freeze";
    }

    private void ToggleChinese()
    {
        if (_overlayWindow == null) return;
        _overlayWindow.ChineseText.Visibility = _overlayWindow.ChineseText.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SaveCurrentLine()
    {
        // Phase 5: SavedLineStore
    }

    private void ExportSaved()
    {
        // Phase 5: export CSV
    }

    private void SelectRegionButton_Click(object sender, RoutedEventArgs e) => ShowRegionSelector();
    private void PauseResumeButton_Click(object sender, RoutedEventArgs e) => TogglePauseResume();
    private void FreezeButton_Click(object sender, RoutedEventArgs e) => ToggleFreeze();
    private void ToggleCnButton_Click(object sender, RoutedEventArgs e) => ToggleChinese();
    private void SaveButton_Click(object sender, RoutedEventArgs e) => SaveCurrentLine();
    private void ExportButton_Click(object sender, RoutedEventArgs e) => ExportSaved();

    private void FpsCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Phase 2: update capture FPS
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_overlayWindow != null)
            _overlayWindow.SetOpacity(e.NewValue);
    }

    private void CloudTranslationCheck_Changed(object sender, RoutedEventArgs e)
    {
        // Phase 4: enable/disable cloud translation
    }

    private void HistoryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Phase 5: re-display selected history line
    }
}
