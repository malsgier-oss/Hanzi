namespace HanziOverlay.Core.Models;

public class AppSettings
{
    public int FPS { get; set; } = 6;
    public CaptureRegion? CaptureRegion { get; set; }

    public double Opacity { get; set; } = 0.85;
    public bool ShowChinese { get; set; } = true;
    public int PinyinRevealDelayMs { get; set; } = 600;
    public int EnglishRevealDelayMs { get; set; } = 1200;

    public bool CloudTranslationEnabled { get; set; } = false;
    public string CloudProvider { get; set; } = "LibreTranslate";
    public string CloudEndpoint { get; set; } = "https://libretranslate.com/translate";
    public string CloudApiKey { get; set; } = "";
    public int CloudTimeoutSeconds { get; set; } = 5;

    public string SavedLinesPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HanziOverlay", "saved_lines.json");
}

public record CaptureRegion(int X, int Y, int Width, int Height);
