using System.Windows;
using System.Windows.Input;

namespace HanziOverlay.App.Windows;

public partial class RegionSelectorWindow : Window
{
    public RegionSelectorWindow()
    {
        InitializeComponent();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }
}
