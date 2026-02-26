using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HanziOverlay.Core.Services.Translation;

public class LibreTranslateService
{
    private readonly HttpClient _client = new();
    private string _endpoint = "https://libretranslate.com/translate";
    private int _timeoutSeconds = 5;

    public string Endpoint
    {
        get => _endpoint;
        set => _endpoint = value?.TrimEnd('/') ?? _endpoint;
    }

    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set => _timeoutSeconds = Math.Clamp(value, 1, 60);
    }

    public async Task<string?> TranslateAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        var payload = new { q = text, source = "zh", target = "en", format = "text" };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

        try
        {
            var response = await _client.PostAsync(_endpoint, content, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("translatedText", out var prop))
                return prop.GetString();
        }
        catch
        {
            // return null on failure
        }

        return null;
    }
}
