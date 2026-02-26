using HanziOverlay.Core.Models;

namespace HanziOverlay.Core.Services.Capture;

public class FrameReadyEventArgs : EventArgs
{
    public CaptureFrame Frame { get; init; } = null!;
}
