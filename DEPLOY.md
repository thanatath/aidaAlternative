# Deployment Guide

Deploy the built app to a target machine directory (default: `C:\aidaAlternative`) and run it from there.

## Deploy (from the repo)

```bash
# 1. Build release (if not already built)
dotnet build -c Release

# 2. Copy the build output to the deploy directory
cp -rf bin/Release/net8.0-windows/* C:/aidaAlternative/

# 3. Run from the deploy directory
cd C:/aidaAlternative
./aidaAlternative.exe
```

Notes:

- The publish directory (`bin/publish`) contains the ClickOnce installer (`setup.exe`). If you prefer a per-machine install instead of a folder copy, use `dotnet publish -c Release` and run `C:\.../bin/publish/setup.exe` — it installs under the user's `AppData` and registers a tray icon.
- `aidaAlternative.exe` is a self-contained Windows app; the .NET runtime ships inside the folder, so no separate runtime install is needed.

## Running

- Run **as Administrator** for full GPU/VRAM temperature monitoring (LibreHardwareMonitor requires elevated access for some sensors). The app still runs without admin, just with fewer sensors.
- The app targets the **960x640 display** as the stats screen and guards it against other windows; if that monitor is absent, it falls back to the primary display.
- **Web server**: serves the gallery UI at `http://localhost:5001` (default port). The `http://*:` listener prefix requires the process to run elevated, or use the non-admin `http://localhost:` prefix in `WebServer/SimpleWebServer.cs`.
- Tray icon: right-click for Show/Hide/Exit.

## Updating

1. Rebuild: `dotnet build -c Release`
2. Kill the running app (tray → Exit, or `taskkill /IM aidaAlternative.exe`)
3. Re-copy: `cp -rf bin/Release/net8.0-windows/* C:/aidaAlternative/`
4. Relaunch from `C:\aidaAlternative`

## Troubleshooting

| Symptom | Fix |
|---|---|
| `HttpListener` exception at startup | Run as Administrator (port prefix uses `http://*:`), or change to `http://localhost:` |
| No GPU/temperature data | Run as Administrator; some sensors need elevated access |
| Stats window on wrong screen | Ensure the 960x640 monitor is connected and set as the target display |
| Port 5001 already in use | Change the default port in `WebServer/SimpleWebServer.cs` (`port = 5001`) and rebuild |
