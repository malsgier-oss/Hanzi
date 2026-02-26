using System.Runtime.InteropServices;
using HanziOverlay.Core.Models;

namespace HanziOverlay.Core.Services.Capture;

public class WindowsGraphicsCaptureService : ICaptureService
{
    #region P/Invoke

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, int uStartScan, int cScanLines, byte[] lpvBits, ref BITMAPINFO bmi, int uUsage);

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
    }

    private const int SRCCOPY = 0x00CC0020;
    private const int DIB_RGB_COLORS = 0;

    #endregion

    private CaptureRegion? _region;
    private CancellationTokenSource? _cts;
    private Task? _captureTask;
    private int _fps = 6;
    private bool _paused;

    public event EventHandler<FrameReadyEventArgs>? FrameReady;

    public bool IsCapturing => _captureTask != null && !_captureTask.IsCompleted;

    public int FPS
    {
        get => _fps;
        set => _fps = Math.Clamp(value, 1, 30);
    }

    public bool IsPaused
    {
        get => _paused;
        set => _paused = value;
    }

    public void Start(CaptureRegion region)
    {
        Stop();
        _region = region;
        _cts = new CancellationTokenSource();
        _captureTask = Task.Run(() => CaptureLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _captureTask = null;
    }

    private async Task CaptureLoop(CancellationToken ct)
    {
        int delayMs = 1000 / Math.Max(1, _fps);
        while (!ct.IsCancellationRequested)
        {
            if (!_paused && _region != null)
            {
                try
                {
                    var frame = CaptureRegion(_region);
                    if (frame != null)
                        FrameReady?.Invoke(this, new FrameReadyEventArgs { Frame = frame });
                }
                catch (Exception)
                {
                    // ignore capture errors
                }
            }
            await Task.Delay(delayMs, ct).ConfigureAwait(false);
        }
    }

    private static CaptureFrame? CaptureRegion(CaptureRegion region)
    {
        if (region.Width <= 0 || region.Height <= 0) return null;

        IntPtr hdcScreen = GetDC(IntPtr.Zero);
        if (hdcScreen == IntPtr.Zero) return null;

        try
        {
            IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
            if (hdcMem == IntPtr.Zero) return null;

            try
            {
                IntPtr hbm = CreateCompatibleBitmap(hdcScreen, region.Width, region.Height);
                if (hbm == IntPtr.Zero) return null;

                try
                {
                    SelectObject(hdcMem, hbm);
                    if (!BitBlt(hdcMem, 0, 0, region.Width, region.Height, hdcScreen, region.X, region.Y, SRCCOPY))
                        return null;

                    var bmi = new BITMAPINFO
                    {
                        bmiHeader = new BITMAPINFOHEADER
                        {
                            biSize = Marshal.SizeOf<BITMAPINFOHEADER>(),
                            biWidth = region.Width,
                            biHeight = -region.Height,
                            biPlanes = 1,
                            biBitCount = 32,
                            biCompression = 0,
                            biSizeImage = region.Width * region.Height * 4
                        }
                    };

                    int stride = region.Width * 4;
                    byte[] pixels = new byte[region.Width * region.Height * 4];
                    int lines = GetDIBits(hdcMem, hbm, 0, region.Height, pixels, ref bmi, DIB_RGB_COLORS);
                    if (lines <= 0) return null;

                    return new CaptureFrame
                    {
                        BgraPixels = pixels,
                        Width = region.Width,
                        Height = region.Height
                    };
                }
                finally
                {
                    DeleteObject(hbm);
                }
            }
            finally
            {
                DeleteDC(hdcMem);
            }
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdcScreen);
        }
    }
}
