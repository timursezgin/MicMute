using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace MicMute
{
    public partial class MainWindow : Window, IDisposable
    {
        private bool _disposed = false;
        private DateTime _startupTime = DateTime.Now;
        
        private AudioManager _audioManager = new AudioManager();
        private SystemTrayManager _trayManager = new SystemTrayManager();
        private HotkeyManager _hotkeyManager = new HotkeyManager();
        
        private AppSettings _settings = new AppSettings();
        private readonly string _settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "MicMute", "settings.json");

        private EventWaitHandle? _eventWaitHandle;
        private Thread? _eventListenerThread;

        private readonly BitmapImage _unmuteIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/unmute.ico"));
        private readonly BitmapImage _muteIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/mute.ico"));

        // Windows API for dark titlebar
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            InitializeServices();
            LoadApplicationSettings();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!this.IsVisible)
            {
                this.ShowInTaskbar = false;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (hwndSource != null)
            {
                hwndSource.AddHook(HwndHook);
                _hotkeyManager.SetWindowHandle(hwndSource.Handle);
                
                // Apply dark titlebar
                ApplyDarkTitleBar(hwndSource.Handle);
                
                if (_settings.HotKeyKey != Key.None)
                {
                    _hotkeyManager.RegisterHotkey(_settings.HotKeyModifiers, _settings.HotKeyKey);
                    hotkeyTextBox.Text = $"{_settings.HotKeyModifiers} + {_settings.HotKeyKey}";
                }
                
                StartListeningForShowSignal();
            }
        }

        private void ApplyDarkTitleBar(IntPtr handle)
        {
            try
            {
                int darkMode = 1;
                DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            }
            catch
            {
                // Ignore if dark mode is not supported
            }
        }

        private void InitializeServices()
        {
            // Initialize audio
            _audioManager.Initialize();
            var devices = _audioManager.GetAvailableDevices();
            micComboBox.ItemsSource = devices;
            micComboBox.DisplayMemberPath = "FriendlyName";
            
            var defaultDevice = _audioManager.GetDefaultDevice();
            if (defaultDevice != null)
            {
                micComboBox.SelectedItem = devices.FirstOrDefault(d => d.ID == defaultDevice.ID);
            }
            else if (devices.Any())
            {
                micComboBox.SelectedIndex = 0;
            }

            // Initialize system tray
            _trayManager.Initialize();
            _trayManager.ShowSettingsRequested += (s, e) => ShowWindow();
            _trayManager.ExitRequested += (s, e) => Application.Current.Shutdown();

            // Initialize hotkey manager
            _hotkeyManager.HotkeyPressed += (s, e) => _audioManager.ToggleMute();

            // Load system sounds
            LoadSystemSounds();
            UpdateUI();
        }

        private void LoadSystemSounds()
        {
            var soundList = new List<SoundInfo> { new SoundInfo { Name = "None", FilePath = string.Empty } };

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps\.Default");
                if (key != null)
                {
                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        using var subKey = key.OpenSubKey(subKeyName);
                        var filePathObject = subKey?.OpenSubKey(".Current")?.GetValue(string.Empty);
                        if (filePathObject is string filePath && !string.IsNullOrEmpty(filePath))
                        {
                            soundList.Add(new SoundInfo { Name = subKeyName, FilePath = filePath });
                        }
                    }
                }
            }
            catch { /* Ignore sound loading errors */ }

            var sortedSounds = soundList.OrderBy(s => s.Name).ToList();
            
            muteSoundComboBox.ItemsSource = sortedSounds;
            muteSoundComboBox.DisplayMemberPath = "Name";
            unmuteSoundComboBox.ItemsSource = sortedSounds;
            unmuteSoundComboBox.DisplayMemberPath = "Name";

            // Set saved selections
            muteSoundComboBox.SelectedItem = sortedSounds.FirstOrDefault(s => s.FilePath == _settings.MuteSoundPath) ?? sortedSounds[0];
            unmuteSoundComboBox.SelectedItem = sortedSounds.FirstOrDefault(s => s.FilePath == _settings.UnmuteSoundPath) ?? sortedSounds[0];
        }

        private void UpdateUI()
        {
            if (_audioManager.SelectedDevice == null) return;
            
            bool isMuted = _audioManager.IsMuted;
            this.Icon = isMuted ? _muteIcon : _unmuteIcon;
            _trayManager.UpdateIcon(isMuted);

            // Play sound
            string? soundPath = isMuted ? _settings.MuteSoundPath : _settings.UnmuteSoundPath;
            if (!string.IsNullOrEmpty(soundPath))
            {
                try { new SoundPlayer(soundPath).Play(); } catch { }
            }
        }

        private void StartListeningForShowSignal()
        {
            try
            {
                string eventName = "{C4A3C2F3-2F4B-4E9E-A2B3-D4C5E6F7G8H9}";
                try
                {
                    _eventWaitHandle = EventWaitHandle.OpenExisting(eventName);
                }
                catch
                {
                    _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
                }

                _eventListenerThread = new Thread(() =>
                {
                    while (!_disposed)
                    {
                        try
                        {
                            if (_eventWaitHandle.WaitOne(5000) && !_disposed)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    if (_settings.StartMinimized && (DateTime.Now - _startupTime).TotalSeconds < 30)
                                        return; // Ignore during startup period
                                    ShowWindow();
                                });
                            }
                        }
                        catch { break; }
                    }
                }) { IsBackground = true };
                
                _eventListenerThread.Start();
            }
            catch { }
        }

        private void ShowWindow()
        {
            this.ShowInTaskbar = true;
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
        }

        // Event Handlers
        private void micComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (micComboBox.SelectedItem is NAudio.CoreAudioApi.MMDevice device)
            {
                _audioManager.SetSelectedDevice(device);
                UpdateUI();
            }
        }

        private void hotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            ModifierKeys modifiers = Keyboard.Modifiers;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key == Key.LeftShift || key == Key.RightShift || key == Key.LeftCtrl || 
                key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || 
                key == Key.LWin || key == Key.RWin) return;

            _settings.HotKeyKey = key;
            _settings.HotKeyModifiers = modifiers;
            hotkeyTextBox.Text = $"{modifiers} + {key}";
            
            _hotkeyManager.RegisterHotkey(modifiers, key);
            SaveSettings();
        }

        private void clearHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            _hotkeyManager.UnregisterHotkey();
            _settings.HotKeyKey = Key.None;
            _settings.HotKeyModifiers = ModifierKeys.None;
            hotkeyTextBox.Text = "Click here and press a key combination";
            SaveSettings();
        }

        private void muteSoundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (muteSoundComboBox.SelectedItem is SoundInfo sound)
            {
                _settings.MuteSoundPath = sound.FilePath;
                SaveSettings();
                if (!string.IsNullOrEmpty(sound.FilePath))
                {
                    try { new SoundPlayer(sound.FilePath).Play(); } catch { }
                }
            }
        }

        private void unmuteSoundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (unmuteSoundComboBox.SelectedItem is SoundInfo sound)
            {
                _settings.UnmuteSoundPath = sound.FilePath;
                SaveSettings();
                if (!string.IsNullOrEmpty(sound.FilePath))
                {
                    try { new SoundPlayer(sound.FilePath).Play(); } catch { }
                }
            }
        }

        // Settings Management
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                _settings = new AppSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
                string json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch { }
        }

        private void LoadApplicationSettings()
        {
            startWithWindowsCheckBox.IsChecked = IsStartWithWindowsEnabled();
            startMinimizedCheckBox.IsChecked = _settings.StartMinimized;
            closeToTrayCheckBox.IsChecked = _settings.CloseToTray;
        }

        // Auto-startup management
        private bool IsStartWithWindowsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                var value = key?.GetValue("MicMute");
                if (value != null)
                {
                    string currentPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    return value.ToString()?.Contains(Path.GetFileName(currentPath)) ?? false;
                }
                return false;
            }
            catch { return false; }
        }

        private void SetAutoStart(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (enabled)
                    {
                        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                        key.SetValue("MicMute", $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue("MicMute", false);
                    }
                }
            }
            catch { }
        }

        // Checkbox event handlers
        private void startWithWindowsCheckBox_Checked(object sender, RoutedEventArgs e) => SetAutoStart(true);
        private void startWithWindowsCheckBox_Unchecked(object sender, RoutedEventArgs e) => SetAutoStart(false);
        private void startMinimizedCheckBox_Checked(object sender, RoutedEventArgs e) { _settings.StartMinimized = true; SaveSettings(); }
        private void startMinimizedCheckBox_Unchecked(object sender, RoutedEventArgs e) { _settings.StartMinimized = false; SaveSettings(); }
        private void closeToTrayCheckBox_Checked(object sender, RoutedEventArgs e) { _settings.CloseToTray = true; SaveSettings(); }
        private void closeToTrayCheckBox_Unchecked(object sender, RoutedEventArgs e) { _settings.CloseToTray = false; SaveSettings(); }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_settings.CloseToTray)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnClosing(e);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = _hotkeyManager.HandleWindowMessage(msg, wParam);
            if (handled) UpdateUI();
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _audioManager?.Dispose();
                _trayManager?.Dispose();
                _hotkeyManager?.Dispose();
                _eventWaitHandle?.Dispose();
            }
        }
    }
}