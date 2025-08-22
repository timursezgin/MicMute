using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace MicMute
{
    public class SystemTrayManager : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private Icon? _muteIcon;
        private Icon? _unmuteIcon;
        private bool _disposed = false;

        public event EventHandler? ShowSettingsRequested;
        public event EventHandler? ExitRequested;

        public bool Initialize()
        {
            try
            {
                LoadIcons();
                CreateNotifyIcon();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LoadIcons()
        {
            var muteStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/mute.ico"));
            if (muteStream != null) _muteIcon = new Icon(muteStream.Stream);

            var unmuteStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/unmute.ico"));
            if (unmuteStream != null) _unmuteIcon = new Icon(unmuteStream.Stream);
        }

        private void CreateNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = _unmuteIcon ?? _muteIcon,
                Text = "MicMute",
                Visible = true
            };

            _notifyIcon.MouseDoubleClick += (s, e) => ShowSettingsRequested?.Invoke(this, EventArgs.Empty);

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Settings", null, (s, e) => ShowSettingsRequested?.Invoke(this, EventArgs.Empty));
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty));

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        public void UpdateIcon(bool isMuted)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Icon = isMuted ? _muteIcon : _unmuteIcon;
                _notifyIcon.Text = $"Microphone: {(isMuted ? "Muted" : "Unmuted")}";
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                _muteIcon?.Dispose();
                _unmuteIcon?.Dispose();
                _disposed = true;
            }
        }
    }
}