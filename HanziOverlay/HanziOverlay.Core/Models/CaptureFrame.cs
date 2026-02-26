namespace HanziOverlay.Core.Models;

/// <summary>
/// Raw BGRA pixel data from a screen region. OCR service converts to SoftwareBitmap as needed.
/// </summary>
public class CaptureFrame
{
    public byte[] BgraPixels { get; init; } = Array.Empty<byte>();
    public int Width { get; init; }
    public int Height { get; init; }
}
