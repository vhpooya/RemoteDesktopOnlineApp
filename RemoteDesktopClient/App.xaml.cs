using System;
using System.Windows;
using System.IO;

namespace RemoteDesktopClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set up exception handling
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            // Create temp directories if they don't exist
            CreateTempDirectories();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            LogException(ex);

            MessageBox.Show($"An unhandled exception occurred: {ex?.Message}\n\nThe application will shut down.",
                            "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Cannot prevent termination when this event is triggered
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);

            MessageBox.Show($"An unhandled exception occurred: {e.Exception.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Mark as handled to prevent application termination
            e.Handled = true;
        }

        private void LogException(Exception ex)
        {
            try
            {
                string logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                               "RemoteDesktopClient", "Logs");

                // Create log directory if it doesn't exist
                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                string logFile = Path.Combine(logFolder, $"error_{DateTime.Now:yyyyMMdd}.log");

                // Append to log file
                using (StreamWriter writer = File.AppendText(logFile))
                {
                    writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Exception:");
                    writer.WriteLine($"Message: {ex?.Message}");
                    writer.WriteLine($"Source: {ex?.Source}");
                    writer.WriteLine($"StackTrace: {ex?.StackTrace}");
                    writer.WriteLine(new string('-', 80));
                }
            }
            catch
            {
                // Ignore errors in the error logger
            }
        }

        private void CreateTempDirectories()
        {
            try
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), "RemoteDesktopClient");

                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }
            }
            catch
            {
                // Ignore directory creation errors
            }
        }
    }
}