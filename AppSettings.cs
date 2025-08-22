using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace MicMute
{
    public class AppSettings : INotifyPropertyChanged
    {
        private Key _hotKeyKey = Key.None;
        private ModifierKeys _hotKeyModifiers = ModifierKeys.None;
        private string? _muteSoundPath;
        private string? _unmuteSoundPath;
        private bool _startMinimized = false;
        private bool _closeToTray = true;

        public Key HotKeyKey
        {
            get => _hotKeyKey;
            set
            {
                if (_hotKeyKey != value)
                {
                    _hotKeyKey = value;
                    OnPropertyChanged(nameof(HotKeyKey));
                }
            }
        }

        public ModifierKeys HotKeyModifiers
        {
            get => _hotKeyModifiers;
            set
            {
                if (_hotKeyModifiers != value)
                {
                    _hotKeyModifiers = value;
                    OnPropertyChanged(nameof(HotKeyModifiers));
                }
            }
        }

        public string? MuteSoundPath
        {
            get => _muteSoundPath;
            set
            {
                if (!string.IsNullOrEmpty(value) && !File.Exists(value))
                {
                    return; // Don't set invalid paths
                }

                if (_muteSoundPath != value)
                {
                    _muteSoundPath = value;
                    OnPropertyChanged(nameof(MuteSoundPath));
                }
            }
        }

        public string? UnmuteSoundPath
        {
            get => _unmuteSoundPath;
            set
            {
                if (!string.IsNullOrEmpty(value) && !File.Exists(value))
                {
                    return; // Don't set invalid paths
                }

                if (_unmuteSoundPath != value)
                {
                    _unmuteSoundPath = value;
                    OnPropertyChanged(nameof(UnmuteSoundPath));
                }
            }
        }

        public bool StartMinimized
        {
            get => _startMinimized;
            set
            {
                if (_startMinimized != value)
                {
                    _startMinimized = value;
                    OnPropertyChanged(nameof(StartMinimized));
                }
            }
        }

        public bool CloseToTray
        {
            get => _closeToTray;
            set
            {
                if (_closeToTray != value)
                {
                    _closeToTray = value;
                    OnPropertyChanged(nameof(CloseToTray));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SoundInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object? obj)
        {
            if (obj is SoundInfo other)
            {
                return FilePath == other.FilePath;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return FilePath?.GetHashCode() ?? 0;
        }
    }
}