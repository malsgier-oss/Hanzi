using HanziOverlay.Core.Models;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using Windows.Security.Cryptography;

namespace HanziOverlay.Core.Services.Ocr;

public class WindowsOcrService : IOcrService
{
    private OcrEngine? _engine;
    private bool _initialized;

    public bool IsAvailable => _initialized && _engine != null;

    public WindowsOcrService()
    {
        TryInitialize();
    }

    private void TryInitialize()
    {
        try
        {
            var lang = new Windows.Globalization.Language("zh-Hans-CN");
            if (!OcrEngine.IsLanguageSupported(lang))
            {
                _initialized = false;
                return;
            }
            _engine = OcrEngine.TryCreateFromLanguage(lang);
            _initialized = _engine != null;
        }
        catch
        {
            _initialized = false;
        }
    }

    public async Task<HanziOverlay.Core.Models.OcrResult> RecognizeAsync(object frame, CancellationToken cancellationToken = default)
    {
        if (frame is not CaptureFrame cf || _engine == null)
            return new Models.OcrResult("", 0);

        try
        {
            SoftwareBitmap? bitmap = CreateSoftwareBitmapFromFrame(cf);
            if (bitmap == null) return new HanziOverlay.Core.Models.OcrResult("", 0);

            var result = await _engine.RecognizeAsync(bitmap).AsTask(cancellationToken);
            string text = result?.Text ?? "";
            return new HanziOverlay.Core.Models.OcrResult(text, 1.0);
        }
        catch
        {
            return new HanziOverlay.Core.Models.OcrResult("", 0);
        }
    }

    private static SoftwareBitmap? CreateSoftwareBitmapFromFrame(CaptureFrame frame)
    {
        if (frame.BgraPixels.Length == 0 || frame.Width <= 0 || frame.Height <= 0)
            return null;

        try
        {
            IBuffer buffer = CryptographicBuffer.CreateFromByteArray(frame.BgraPixels);
            return SoftwareBitmap.CreateCopyFromBuffer(buffer, BitmapPixelFormat.Bgra8, frame.Width, frame.Height, BitmapAlphaMode.Premultiplied);
        }
        catch
        {
            return null;
        }
    }
}
