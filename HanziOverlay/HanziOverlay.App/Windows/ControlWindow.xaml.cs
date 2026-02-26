using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
using HanziOverlay.App.Models;
using HanziOverlay.Core.Models;
using HanziOverlay.Core.Services.Capture;
using HanziOverlay.Core.Services.Hotkeys;
using HanziOverlay.Core.Services.Ocr;
using HanziOverlay.Core.Services.Persistence;
using HanziOverlay.Core.Services.Pinyin;
using HanziOverlay.Core.Services.Stabilization;
using HanziOverlay.Core.Services.Translation;

namespace HanziOverlay.App.Windows;

public partial class ControlWindow : Window
{
    private OverlayWindow? _overlayWindow;
    private GlobalHotkeyService _hotkeyService;
    private HwndSource? _hwndSource;

    private WindowsGraphicsCaptureService? _captureService;
    private WindowsOcrService? _ocrService;
    private SubtitleStabilizer? _stabilizer;
    private NPinyinService? _pinyinService;
    private HybridTranslationService? _translationService;
    private CaptureOcrPipeline? _pipeline;
    private SavedLineStore? _savedLineStore;
    private SettingsStore? _settingsStore;
    private AppSettings? _settings;

    private bool _frozen;
    private int _pinyinRevealDelayMs = 600;
    private int _englishRevealDelayMs = 1200;
    private bool _cloudEnabled;

    private string _currentCn = "";
    private string _currentPinyin = "";
    private string _currentEnglish = "";
    private double _currentConfidence;

    private const int MaxHistory = 30;
    private readonly ObservableCollection<HistoryItem> _history = new();

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
        try
        {
            _overlayWindow = new OverlayWindow();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to create overlay window: {ex.Message}", "HanziOverlay", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        _overlayWindow.Show();
        _overlayWindow.SetOpacity(OpacitySlider.Value);
        _overlayWindow.SetPinyin("");
        _overlayWindow.SetEnglish("Select region (Ctrl+Alt+S) to start");
        _overlayWindow.SetChinese("");
        _overlayWindow.SaveRequested += (_, _) => SaveCurrentLine();
        _overlayWindow.ToggleChineseRequested += (_, _) => { };
        _overlayWindow.OpacityChanged += (_, opacity) => OpacitySlider.Value = opacity;

        _savedLineStore = new SavedLineStore();
        _settingsStore = new SettingsStore();
        _settings = _settingsStore.Load();
        _pinyinRevealDelayMs = _settings.PinyinRevealDelayMs;
        _englishRevealDelayMs = _settings.EnglishRevealDelayMs;
        _cloudEnabled = _settings.CloudTranslationEnabled;

        _captureService = new WindowsGraphicsCaptureService { FPS = _settings.FPS };
        _ocrService = new WindowsOcrService();
        UpdateOcrStatus();
        _stabilizer = new SubtitleStabilizer(requiredConsecutiveFrames: 2, similarityThreshold: 0.80, highConfidenceThreshold: 0.85);
        _pinyinService = new NPinyinService();
        _translationService = new HybridTranslationService();
        _translationService.Configure(_cloudEnabled, _settings.CloudProvider, _settings.CloudEndpoint, _settings.CloudApiKey, _settings.CloudTimeoutSeconds);
        _translationService.TranslationUpdated += OnTranslationUpdated;
        _pipeline = new CaptureOcrPipeline(_captureService, _ocrService, _stabilizer);
        _pipeline.StableSubtitleChanged += OnStableSubtitleChanged;

        FpsCombo.SelectedIndex = _settings.FPS switch { 3 => 0, 6 => 1, 10 => 2, _ => 1 };
        OpacitySlider.Value = _settings.Opacity;
        CloudTranslationCheck.IsChecked = _cloudEnabled;
        CloudEndpointBox.Text = _settings.CloudEndpoint ?? "";
        CloudApiKeyBox.Password = _settings.CloudApiKey ?? "";
        _overlayWindow.SetOpacity(_settings.Opacity);
        _overlayWindow.SetChineseVisible(_settings.ShowChinese);

        HistoryList.ItemsSource = _history;

        if (_settings.CaptureRegion != null)
        {
            _pipeline.Start(_settings.CaptureRegion);
            PauseResumeButton.Content = "Pause";
        }

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
        _pipeline?.Stop();
        _translationService?.Configure(false, "", "", "", 5);
        _translationService!.TranslationUpdated -= OnTranslationUpdated;
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
            Application.Current.Properties["CaptureRegion"] = region;
            _settings!.CaptureRegion = region;
            _settingsStore?.Save(_settings);
            _pipeline?.Stop();
            _pipeline?.Start(region);
            PauseResumeButton.Content = "Pause";
        };
        selector.Show();
    }

    private void OnStableSubtitleChanged(object? sender, StableSubtitle stable)
    {
        if (_overlayWindow == null || _frozen) return;

        string cn = stable.CnText;
        string pinyin = _pinyinService?.ToPinyinWithTones(cn) ?? "";
        bool showChinese = _overlayWindow.ChineseText.Visibility == Visibility.Visible;
        bool cloudOn = _cloudEnabled;
        string enPlaceholder = cloudOn ? "(translating...)" : "(translation offline)";

        _currentCn = cn;
        _currentPinyin = pinyin;
        _currentEnglish = enPlaceholder;
        _currentConfidence = stable.OcrConfidence;

        // Update overlay immediately so pinyin and translation placeholder show in real time
        Dispatcher.Invoke(() =>
        {
            if (showChinese)
                _overlayWindow.SetChinese(cn);
            _overlayWindow.SetPinyin(pinyin);
            _overlayWindow.SetEnglish(enPlaceholder);
            AddToHistory(cn, pinyin, enPlaceholder, stable.OcrConfidence);
        });

        if (cloudOn && _translationService != null)
            _ = _translationService.TranslateAsync(cn, CancellationToken.None);
    }

    private void OnTranslationUpdated(object? sender, TranslationUpdatedEventArgs e)
    {
        if (e.CnText != _currentCn) return;
        _currentEnglish = e.CloudEnglish;
        Dispatcher.Invoke(() => _overlayWindow?.SetEnglish(e.CloudEnglish));
    }

    private void AddToHistory(string cn, string pinyin, string en, double confidence)
    {
        _history.Insert(0, new HistoryItem(cn, pinyin, en, confidence));
        while (_history.Count > MaxHistory)
            _history.RemoveAt(_history.Count - 1);
    }

    private void TogglePauseResume()
    {
        bool isPaused = PauseResumeButton.Content?.ToString() == "Resume";
        _pipeline?.SetPaused(!isPaused);
        PauseResumeButton.Content = isPaused ? "Pause" : "Resume";
    }

    private void ToggleFreeze()
    {
        _frozen = !_frozen;
        FreezeButton.Content = _frozen ? "Unfreeze" : "Freeze";
    }

    private void ToggleChinese()
    {
        if (_overlayWindow == null) return;
        _overlayWindow.ChineseText.Visibility = _overlayWindow.ChineseText.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SaveCurrentLine()
    {
        if (_savedLineStore == null || string.IsNullOrWhiteSpace(_currentCn)) return;
        string en = string.IsNullOrWhiteSpace(_currentEnglish) ? "(translation offline)" : _currentEnglish;
        _savedLineStore.AddLine(new SavedLine(DateTime.UtcNow, _currentCn, _currentPinyin, en, _currentConfidence));
    }

    private void ExportSaved()
    {
        if (_savedLineStore == null) return;
        var dlg = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = "csv",
            FileName = "hanzioverlay_export.csv"
        };
        if (dlg.ShowDialog() == true)
            _savedLineStore.ExportToCsv(dlg.FileName);
    }

    private void SelectRegionButton_Click(object sender, RoutedEventArgs e) => ShowRegionSelector();
    private void PauseResumeButton_Click(object sender, RoutedEventArgs e) => TogglePauseResume();
    private void FreezeButton_Click(object sender, RoutedEventArgs e) => ToggleFreeze();
    private void ToggleCnButton_Click(object sender, RoutedEventArgs e) => ToggleChinese();
    private void SaveButton_Click(object sender, RoutedEventArgs e) => SaveCurrentLine();
    private void ExportButton_Click(object sender, RoutedEventArgs e) => ExportSaved();

    private void FpsCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_captureService != null && _settings != null && FpsCombo.SelectedItem is System.Windows.Controls.ComboBoxItem item && int.TryParse(item.Content?.ToString(), out int fps))
        {
            _captureService.FPS = fps;
            _settings.FPS = fps;
            _settingsStore?.Save(_settings);
        }
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_overlayWindow != null && _settings != null)
        {
            _overlayWindow.SetOpacity(e.NewValue);
            _settings.Opacity = e.NewValue;
            _settingsStore?.Save(_settings);
        }
    }

    private void CloudTranslationCheck_Changed(object sender, RoutedEventArgs e)
    {
        SaveCloudSettings();
        _cloudEnabled = CloudTranslationCheck.IsChecked == true;
        _settings!.CloudTranslationEnabled = _cloudEnabled;
        _settingsStore?.Save(_settings);
        ApplyCloudConfig();
    }

    private void SaveCloudSettings()
    {
        if (_settings == null) return;
        _settings.CloudEndpoint = CloudEndpointBox.Text?.Trim() ?? "";
        _settings.CloudApiKey = CloudApiKeyBox.Password ?? "";
        _settingsStore?.Save(_settings);
    }

    private void ApplyCloudConfig()
    {
        SaveCloudSettings();
        _settings = _settingsStore?.Load();
        if (_settings != null)
        {
            string provider = !string.IsNullOrWhiteSpace(_settings.CloudApiKey) ? "OpenAI" : "LibreTranslate";
            string endpoint = _settings.CloudEndpoint ?? (provider == "OpenAI" ? "https://api.openai.com/v1/chat/completions" : "https://libretranslate.com/translate");
            _translationService?.Configure(_settings.CloudTranslationEnabled, provider, endpoint, _settings.CloudApiKey ?? "", _settings.CloudTimeoutSeconds);
        }
    }

    private void CloudSettings_LostFocus(object sender, RoutedEventArgs e)
    {
        ApplyCloudConfig();
    }

    private void UpdateOcrStatus()
    {
        if (OcrStatusText != null)
            OcrStatusText.Text = _ocrService?.IsAvailable == true ? "OCR: Ready (Chinese)" : "OCR: Not available — install Chinese language pack";
    }

    private void HistoryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (HistoryList.SelectedItem is HistoryItem item && _overlayWindow != null)
        {
            _overlayWindow.SetChinese(item.CnText);
            _overlayWindow.SetPinyin(item.Pinyin);
            _overlayWindow.SetEnglish(item.English);
        }
    }
}
