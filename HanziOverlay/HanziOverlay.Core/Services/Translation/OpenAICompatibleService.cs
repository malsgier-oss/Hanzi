using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HanziOverlay.Core.Services.Translation;

public class OpenAICompatibleService
{
    private readonly HttpClient _client = new();
    private string _endpoint = "https://api.openai.com/v1/chat/completions";
    private string _apiKey = "";
    private string _model = "gpt-3.5-turbo";
    private int _timeoutSeconds = 5;

    public string Endpoint
    {
        get => _endpoint;
        set => _endpoint = value?.TrimEnd('/') ?? _endpoint;
    }

    public string ApiKey
    {
        get => _apiKey;
        set => _apiKey = value ?? "";
    }

    public string Model
    {
        get => _model;
        set => _model = value ?? "gpt-3.5-turbo";
    }

    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set => _timeoutSeconds = Math.Clamp(value, 1, 60);
    }

    public async Task<string?> TranslateAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        var payload = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = "Translate the following Chinese text to English. Reply with only the English translation, no explanation.\n\n" + text }
            }
        };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        if (!string.IsNullOrEmpty(_apiKey))
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

        try
        {
            var response = await _client.PostAsync(_endpoint, content, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var first = choices[0];
                if (first.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var contentProp))
                    return contentProp.GetString()?.Trim();
            }
        }
        catch
        {
            // return null on failure
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = null;
        }

        return null;
    }
}
