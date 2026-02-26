using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using HanziOverlay.Core.Models;

namespace HanziOverlay.App.Windows;

public partial class RegionSelectorWindow : Window
{
    private Point _startWindow;
    private bool _dragging;

    public event EventHandler<CaptureRegion>? RegionSelected;

    public RegionSelectorWindow()
    {
        InitializeComponent();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startWindow = e.GetPosition(Canvas);
        _dragging = true;
        SelectionRect.Visibility = Visibility.Visible;
        SelectionRect.Width = 0;
        SelectionRect.Height = 0;
        System.Windows.Controls.Canvas.SetLeft(SelectionRect, _startWindow.X);
        System.Windows.Controls.Canvas.SetTop(SelectionRect, _startWindow.Y);
        Mouse.Capture(this, CaptureMode.SubTree);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        Point current = e.GetPosition(Canvas);
        double x = Math.Min(_startWindow.X, current.X);
        double y = Math.Min(_startWindow.Y, current.Y);
        double w = Math.Abs(current.X - _startWindow.X);
        double h = Math.Abs(current.Y - _startWindow.Y);
        System.Windows.Controls.Canvas.SetLeft(SelectionRect, x);
        System.Windows.Controls.Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = w;
        SelectionRect.Height = h;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        Mouse.Capture(null);
        _dragging = false;

        Point endWindow = e.GetPosition(Canvas);
        double minX = Math.Min(_startWindow.X, endWindow.X);
        double minY = Math.Min(_startWindow.Y, endWindow.Y);
        double maxX = Math.Max(_startWindow.X, endWindow.X);
        double maxY = Math.Max(_startWindow.Y, endWindow.Y);
        double w = maxX - minX;
        double h = maxY - minY;

        if (w < 10 || h < 10)
        {
            Close();
            return;
        }

        Point topLeftScreen = PointToScreen(new Point(minX, minY));
        Point bottomRightScreen = PointToScreen(new Point(maxX, maxY));
        int x = (int)topLeftScreen.X;
        int y = (int)topLeftScreen.Y;
        int width = (int)(bottomRightScreen.X - topLeftScreen.X);
        int height = (int)(bottomRightScreen.Y - topLeftScreen.Y);

        RegionSelected?.Invoke(this, new CaptureRegion(x, y, width, height));
        Close();
    }
}
