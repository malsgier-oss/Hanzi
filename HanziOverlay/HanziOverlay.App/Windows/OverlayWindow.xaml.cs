using System.Windows;
using System.Windows.Input;

namespace HanziOverlay.App.Windows;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
        MouseLeftButtonDown += (_, _) => DragMove();
    }
}
