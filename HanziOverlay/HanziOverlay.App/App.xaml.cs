using System.Windows;
using System.Windows.Threading;

namespace HanziOverlay.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            MessageBox.Show($"Error: {e.Exception.Message}\n\n{e.Exception.StackTrace}", "HanziOverlay Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
    }
}
