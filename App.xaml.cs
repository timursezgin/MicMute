using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Newtonsoft.Json;

namespace MicMute
{
    public partial class App : Application
    {
        // A unique name for the event that instances will use to communicate
        private const string UNIQUE_EVENT_NAME = "{C4A3C2F3-2F4B-4E9E-A2B3-D4C5E6F7G8H9}";
        private EventWaitHandle? _eventWaitHandle;
        private MainWindow? _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UNIQUE_EVENT_NAME, out bool createdNew);

                if (!createdNew)
                {
                    // Only signal the existing instance to show if --show parameter is present
                    bool showRequested = e.Args.Contains("--show");
                    if (showRequested)
                    {
                        _eventWaitHandle.Set();
                    }
                    Current.Shutdown();
                    return;
                }

                base.OnStartup(e);

                _mainWindow = new MainWindow();
                
                // Check if we should start minimized by loading settings
                bool shouldStartMinimized = ShouldStartMinimized();
                
                // Always initialize the window to ensure hotkeys work, but control visibility
                bool forceShow = e.Args.Contains("--show");
                if (!shouldStartMinimized || forceShow)
                {
                    _mainWindow.Show();
                }
                else
                {
                    // Initialize window without showing it to enable hotkey registration
                    _mainWindow.WindowState = WindowState.Minimized;
                    _mainWindow.ShowInTaskbar = false;
                    _mainWindow.Show();
                    _mainWindow.Hide();
                }

                // Handle unhandled exceptions
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application: {ex.Message}", "MicMute Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // Properly dispose of the main window
                _mainWindow?.Dispose();
                _eventWaitHandle?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during application exit: {ex.Message}");
            }
            finally
            {
                base.OnExit(e);
            }
        }

        private bool ShouldStartMinimized()
        {
            try
            {
                string settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MicMute", "settings.json");
                
                if (File.Exists(settingsFilePath))
                {
                    string json = File.ReadAllText(settingsFilePath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    return settings?.StartMinimized ?? false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading startup settings: {ex.Message}");
            }
            
            return false; // Default to showing window if settings can't be loaded
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                string message = ex?.Message ?? "Unknown error occurred";

                MessageBox.Show($"A fatal error occurred: {message}", "MicMute Fatal Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                // Try to save any important data before shutting down
                _mainWindow?.Dispose();
            }
            catch
            {
                // If we can't even show a message box, just fail silently
            }
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                MessageBox.Show($"An error occurred: {e.Exception.Message}", "MicMute Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                // Mark the exception as handled so the application doesn't crash
                e.Handled = true;
            }
            catch
            {
                // If we can't handle the exception, let it crash
                e.Handled = false;
            }
        }
    }
}