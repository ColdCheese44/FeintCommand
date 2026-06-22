using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace FeintCommand;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FeintAI",
            "FeintCommand");
        Directory.CreateDirectory(directory);
        string logPath = Path.Combine(directory, "crash.log");
        File.AppendAllText(logPath, $"[{DateTimeOffset.Now:O}]{Environment.NewLine}{e.Exception}{Environment.NewLine}{Environment.NewLine}");

        MessageBox.Show(
            $"FeintCommand encountered an unexpected error. Details were written to:{Environment.NewLine}{logPath}{Environment.NewLine}{Environment.NewLine}{e.Exception.Message}",
            "FeintCommand",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
