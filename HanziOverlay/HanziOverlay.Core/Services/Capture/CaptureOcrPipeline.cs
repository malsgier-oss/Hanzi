using HanziOverlay.Core.Models;
using HanziOverlay.Core.Services.Ocr;
using HanziOverlay.Core.Services.Stabilization;

namespace HanziOverlay.Core.Services.Capture;

public class CaptureOcrPipeline
{
    private readonly ICaptureService _capture;
    private readonly IOcrService _ocr;
    private readonly ISubtitleStabilizer _stabilizer;
    private CancellationTokenSource? _ocrCts;
    private bool _paused;

    public CaptureOcrPipeline(ICaptureService capture, IOcrService ocr, ISubtitleStabilizer stabilizer)
    {
        _capture = capture;
        _ocr = ocr;
        _stabilizer = stabilizer;
        _capture.FrameReady += OnFrameReady;
    }

    public event EventHandler<StableSubtitle>? StableSubtitleChanged
    {
        add => _stabilizer.StableSubtitleChanged += value;
        remove => _stabilizer.StableSubtitleChanged -= value;
    }

    public bool IsRunning => _capture.IsCapturing;
    public bool IsPaused { get => _paused; set => _paused = value; }

    public void Start(CaptureRegion region)
    {
        _stabilizer.Reset();
        _capture.Start(region);
    }

    public void Stop()
    {
        _capture.Stop();
        _ocrCts?.Cancel();
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        if (_capture is WindowsGraphicsCaptureService wgc)
            wgc.IsPaused = paused;
    }

    private async void OnFrameReady(object? sender, FrameReadyEventArgs e)
    {
        if (_paused) return;
        _ocrCts?.Cancel();
        _ocrCts = new CancellationTokenSource();
        var ct = _ocrCts.Token;
        try
        {
            var result = await _ocr.RecognizeAsync(e.Frame, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested) return;
            _stabilizer.PushOcrResult(result);
        }
        catch
        {
            // ignore
        }
    }
}
