using HanziOverlay.Core.Models;

namespace HanziOverlay.Core.Services.Ocr;

public interface IOcrService
{
    Task<OcrResult> RecognizeAsync(object frame, CancellationToken cancellationToken = default);
    bool IsAvailable { get; }
}
