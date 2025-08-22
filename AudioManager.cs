using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MicMute
{
    public class AudioManager : IDisposable
    {
        private MMDeviceEnumerator? _deviceEnumerator;
        private MMDevice? _selectedDevice;
        private bool _disposed = false;

        public MMDevice? SelectedDevice => _selectedDevice;
        public bool IsMuted => _selectedDevice?.AudioEndpointVolume.Mute ?? false;

        public bool Initialize()
        {
            try
            {
                _deviceEnumerator = new MMDeviceEnumerator();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<MMDevice> GetAvailableDevices()
        {
            try
            {
                return _deviceEnumerator?.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList() ?? new List<MMDevice>();
            }
            catch
            {
                return new List<MMDevice>();
            }
        }

        public MMDevice? GetDefaultDevice()
        {
            try
            {
                return _deviceEnumerator?.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            }
            catch
            {
                return null;
            }
        }

        public void SetSelectedDevice(MMDevice device)
        {
            _selectedDevice = device;
        }

        public void ToggleMute()
        {
            if (_selectedDevice != null)
            {
                _selectedDevice.AudioEndpointVolume.Mute = !_selectedDevice.AudioEndpointVolume.Mute;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _selectedDevice?.Dispose();
                _deviceEnumerator?.Dispose();
                _disposed = true;
            }
        }
    }
}