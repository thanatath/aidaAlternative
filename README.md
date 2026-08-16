# System Stats Monitor

A lightweight Windows system monitor for a dedicated 960x640 display, with hardware statistics, local Claude Code usage, and system tray integration.

## Features

- **Real-Time Stats**: Monitors CPU usage, RAM usage, network download/upload speeds, GPU usage, VRAM, and temperature.
- **Custom UI**: Displays stats in a visually appealing interface with gradient backgrounds, progress bars, and fade animations.
- **Claude Usage**: Shows token activity and any rate-limit data written by local Claude Code session logs.
- **System Tray Integration**: Runs in the system tray with a context menu to show/hide the app or exit.
- **Dedicated Display Guard**: Moves other app windows and the cursor from the 960x640 screen back to the Windows primary display.

## Prerequisites

- **Windows OS**: Windows 10 or later.
- **.NET**: .NET 8 SDK or later.
- **Administrative Privileges**: Required for GPU monitoring via LibreHardwareMonitor.
- **Monitor**: A 960x640 resolution monitor is recommended for optimal display.


## Installation

1. **Clone the Repository**:
   ```bash
   git clone git@github.com:thanatath/aidaAlternative.git
   cd aidaAlternative
   ```
2. **Build**:
   ```bash
   dotnet build
   ```
3. **Run**:
   ```bash
   dotnet run
   ```

The slideshow and its gallery are disabled in code comments so they can be restored later.
