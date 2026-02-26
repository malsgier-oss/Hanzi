using System.Text;
using HanziOverlay.Core.Models;

namespace HanziOverlay.Core.Services.Persistence;

public class SavedLineStore : ISavedLineStore
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private List<SavedLine> _lines = new();

    public SavedLineStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HanziOverlay", "saved_lines.json");
        Load();
    }

    public void AddLine(SavedLine line)
    {
        lock (_lock)
        {
            _lines.Add(line);
            Save();
        }
    }

    public IReadOnlyList<SavedLine> GetAll()
    {
        lock (_lock)
            return _lines.ToList();
    }

    public void ExportToCsv(string filePath)
    {
        var lines = GetAll();
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Chinese,Pinyin,English,Confidence");
        foreach (var line in lines)
        {
            sb.Append(CsvEscape(line.Timestamp.ToString("o"))).Append(',');
            sb.Append(CsvEscape(line.CN)).Append(',');
            sb.Append(CsvEscape(line.Pinyin)).Append(',');
            sb.Append(CsvEscape(line.EN)).Append(',');
            sb.AppendLine(line.Confidence.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
    }

    private static string CsvEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "\"\"";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    private void Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_filePath))
            {
                _lines = new List<SavedLine>();
                return;
            }
            try
            {
                var json = File.ReadAllText(_filePath);
                var list = System.Text.Json.JsonSerializer.Deserialize<List<SavedLine>>(json);
                _lines = list ?? new List<SavedLine>();
            }
            catch
            {
                _lines = new List<SavedLine>();
            }
        }
    }

    private void Save()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var json = System.Text.Json.JsonSerializer.Serialize(_lines, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
