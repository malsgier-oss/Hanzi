namespace HanziOverlay.Core.Services.Capture;

public class FrameReadyEventArgs : EventArgs
{
    public object Frame { get; init; } = null!;
}
