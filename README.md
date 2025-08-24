# MicMute

A lightweight Windows desktop application for quick and easy microphone control with interactive system tray integration.

## Use Case

MicMute is designed for users who need instant, convenient control over their microphone during:

- **Video calls and meetings** - Quickly mute/unmute without fumbling through application settings
- **Streaming and content creation** - Reliable microphone control with visual feedback
- **Gaming** - Fast mute toggle during online gaming sessions
- **General privacy** - Easy microphone management for security-conscious users

## Key Features

### 🎤 Audio Management
- Select from available microphone devices
- Mute/unmute using keyboard shortcuts
- Optional system sound effects on mute/unmute actions
- Real-time microphone status indication

### ⌨️ Global Hotkeys
- System-wide keyboard shortcuts (even when app is minimized)
- Customizable hotkey combinations with modifier keys
- Works across all applications without focus switching

### 🔔 System Tray Integration
- Runs quietly in the system tray
- Visual mute/unmute status with different tray icons
- Right-click context menu for quick actions
- Double-click to open settings

### ⚙️ Flexible Configuration
- **Start with Windows** - Automatically launch on system boot
- **Start minimized** - Begin in system tray without showing window
- **Close to tray** - Minimize to tray instead of closing
- Settings persist across application updates

### 🎨 Modern Interface
- Clean, professional dark theme
- Intuitive settings panel
- Minimal resource usage
- Single-instance application

## Installation

1. Download the latest `MicMuteSetup.exe` from releases
2. Run the installer
3. Choose installation options (shortcuts, auto-launch)
4. Run the application

## System Requirements

- **OS**: Windows 10/11
- **Framework**: .NET 8.0 Runtime
- **Dependencies**: Included in installer
- **Disk Space**: ~5MB

## Usage

### First Time Setup
1. Launch MicMute (or find it in system tray)
2. Select your microphone device from dropdown
3. Configure global hotkeys or key combinations to your liking
4. Set startup preferences
5. Minimize to tray

### Daily Use
- **Mute/Unmute**: Use configured hotkeys
- **Settings**: Double-click tray icon or right-click → Settings
- **Status Check**: Tray icon shows current mute state (red = muted, white = unmuted)

## Privacy & Security

- **Local Storage**: Settings stored in `%APPDATA%\MicMute\settings.json`
- **No Network Access**: Application works entirely offline
- **No Data Collection**: No telemetry or usage tracking
- **Open Architecture**: Clean, maintainable codebase for transparency

## Technical Details

- **Technology**: WPF (.NET 8.0) with NAudio library
- **Audio API**: Windows Audio Session API (WASAPI)
- **Hotkeys**: Windows API for global keyboard hooks
- **Architecture**: Modular design with separated concerns

## License

- None

## Support

For issues, feature requests, or questions, please visit the project repository or contact the developer.
