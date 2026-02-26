using HanziOverlay.Core.Models;

namespace HanziOverlay.Core.Services.Stabilization;

public interface ISubtitleStabilizer
{
    event EventHandler<StableSubtitle>? StableSubtitleChanged;
    void PushOcrResult(OcrResult result);
    void Reset();
}
