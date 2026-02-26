using HanziOverlay.Core.Models;

namespace HanziOverlay.Core.Services.Capture;

public interface ICaptureService
{
    event EventHandler<FrameReadyEventArgs>? FrameReady;

    void Start(CaptureRegion region);
    void Stop();
    bool IsCapturing { get; }
}
