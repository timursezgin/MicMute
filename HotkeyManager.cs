using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace MicMute
{
    public class HotkeyManager : IDisposable
    {
        private const int HOTKEY_ID = 9000;
        private IntPtr _windowHandle;
        private bool _isRegistered = false;
        private bool _disposed = false;

        public event EventHandler? HotkeyPressed;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public void SetWindowHandle(IntPtr handle)
        {
            _windowHandle = handle;
        }

        public bool RegisterHotkey(ModifierKeys modifiers, Key key)
        {
            if (_windowHandle == IntPtr.Zero || key == Key.None) return false;

            UnregisterHotkey();

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            bool success = RegisterHotKey(_windowHandle, HOTKEY_ID, (uint)modifiers, vk);
            _isRegistered = success;
            return success;
        }

        public void UnregisterHotkey()
        {
            if (_isRegistered && _windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
                _isRegistered = false;
            }
        }

        public bool HandleWindowMessage(int msg, IntPtr wParam)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterHotkey();
                _disposed = true;
            }
        }
    }
}