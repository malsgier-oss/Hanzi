using HanziOverlay.Core.Models;

namespace HanziOverlay.Core.Services.Translation;

public class HybridTranslationService : ITranslationService
{
    private readonly TranslationCache _cache = new();
    private readonly LibreTranslateService _libre = new();
    private readonly OpenAICompatibleService _openAi = new();

    private bool _cloudEnabled;
    private string _provider = "LibreTranslate";
    private string _endpoint = "https://libretranslate.com/translate";
    private string _apiKey = "";
    private int _timeoutSeconds = 5;

    public event EventHandler<TranslationUpdatedEventArgs>? TranslationUpdated;

    public void Configure(bool cloudEnabled, string provider, string endpoint, string apiKey, int timeoutSeconds)
    {
        _cloudEnabled = cloudEnabled;
        _provider = provider ?? "LibreTranslate";
        _endpoint = endpoint ?? _endpoint;
        _apiKey = apiKey ?? "";
        _timeoutSeconds = Math.Clamp(timeoutSeconds, 1, 60);
        _libre.Endpoint = _provider == "LibreTranslate" ? _endpoint : _libre.Endpoint;
        _libre.TimeoutSeconds = _timeoutSeconds;
        _openAi.Endpoint = _provider == "OpenAI" ? _endpoint : _openAi.Endpoint;
        _openAi.ApiKey = _apiKey;
        _openAi.TimeoutSeconds = _timeoutSeconds;
    }

    public async Task<TranslationResult> TranslateAsync(string cnText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cnText))
            return new TranslationResult("", null, false);

        int key = TranslationCache.HashCnText(cnText);

        if (_cloudEnabled && _cache.TryGet(key, out string? cached))
            return new TranslationResult("", cached, true);

        string localEn = "";
        string? cloudEn = null;
        bool usedCloud = false;

        if (_cloudEnabled)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    string? result = _provider == "OpenAI"
                        ? await _openAi.TranslateAsync(cnText, cancellationToken).ConfigureAwait(false)
                        : await _libre.TranslateAsync(cnText, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(result))
                    {
                        _cache.Set(key, result);
                        TranslationUpdated?.Invoke(this, new TranslationUpdatedEventArgs { CnText = cnText, CloudEnglish = result });
                    }
                }
                catch
                {
                    // silent failure
                }
            }, cancellationToken);
        }

        return new TranslationResult(localEn, cloudEn, usedCloud);
    }
}
