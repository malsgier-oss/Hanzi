using HanziOverlay.Core.Models;

namespace HanziOverlay.Core.Services.Translation;

public interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(string cnText, CancellationToken cancellationToken = default);
    event EventHandler<TranslationUpdatedEventArgs>? TranslationUpdated;
    event EventHandler<TranslationFailedEventArgs>? TranslationFailed;
}

public class TranslationUpdatedEventArgs : EventArgs
{
    public string CnText { get; init; } = "";
    public string CloudEnglish { get; init; } = "";
}

public class TranslationFailedEventArgs : EventArgs
{
    public string CnText { get; init; } = "";
}
