using HanziOverlay.Core.Models;

namespace HanziOverlay.Core.Services.Persistence;

public class SettingsStore
{
    private readonly string _filePath;
    private static readonly System.Text.Json.JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SettingsStore()
    {
        _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HanziOverlay", "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json, Options);
                if (settings != null) return settings;
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var json = System.Text.Json.JsonSerializer.Serialize(settings, Options);
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }
}
