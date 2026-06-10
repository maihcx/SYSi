# SYSi

**A lightweight, fast, and modern system information utility for Windows.**

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue?logo=windows)](https://github.com/maihcx/SYSi/releases)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Custom-orange)](LICENSE)
[![Language](https://img.shields.io/badge/Language-EN%20%7C%20VI-green)](#)

---

## Overview

**SYSi** is a WPF application that displays detailed hardware and operating system information on Windows, inspired by CPU-Z and GPU-Z. Instead of relying on the slow WMI stack, SYSi queries hardware directly through Win32 APIs and P/Invoke for near-instant response times.

---

## Features

### Hardware Pages

| Page | Information |
|---|---|
| **CPU** | Name, manufacturer, architecture, socket, family, stepping, processor ID, physical cores, logical processors, base & max clock speed, L1/L2/L3 cache, virtualization status, real-time usage |
| **GPU** | Name, manufacturer, VRAM, driver version & date, video processor, architecture, memory type, resolution, refresh rate, bits per pixel, real-time usage |
| **RAM** | Total / used / available capacity, slot count, speed, memory type |
| **Storage** | Drive letter, volume label, file system, drive type, total / used / free space, device model |
| **Motherboard** | Manufacturer, model, serial number, BIOS version |
| **Network** | Adapter list, MAC address, link speed, connection status |
| **OS** | Windows name & version, build number, activation status, hostname, logged-in user, install date, last boot time, uptime, time zone, locale, system root, Windows Update status |

### Application Features

- **Home Dashboard** — at-a-glance summary of CPU, GPU, RAM, and OS with live usage cards; column count adjusts dynamically to window width
- **Auto-Update** — checks for new releases on GitHub, downloads and launches the installer in-app
- **Bilingual UI** — English and Vietnamese; switch languages without restarting
- **Single Instance** — only one window runs at a time; a second launch brings the existing window to the foreground via Named Pipe
- **Splash Screen** — logo shown on startup
- **Run at Windows Startup** — toggle in Settings
- **UI Customization** — light/dark theme, background material (Mica/Acrylic/...), corner radius, auto-hide navigation pane

---

## Tech Stack

| Component | Details |
|---|---|
| **Framework** | .NET 10, WPF |
| **UI Library** | [WPF-UI](https://github.com/lepoco/wpfui) v4.3.0 (Fluent Design) |
| **MVVM** | CommunityToolkit.Mvvm 8.4.2 |
| **DI / Hosting** | Microsoft.Extensions.Hosting 10.0 |
| **Hardware API** | Win32 API / P/Invoke — no WMI |
| **Architecture** | MVVM + Services + HostedServices, domain-split partial classes |
| **Target** | Windows x64 |

---

## Installation

### Option 1 — Installer (recommended)

1. Download `SYSi.Installer.exe` from [Releases](https://github.com/maihcx/SYSi/releases/latest)
2. Run the installer and follow the on-screen steps

### Option 2 — Build from Source

**Requirements:** .NET 10 SDK, Visual Studio 2022 (or VS Build Tools)

```bash
git clone https://github.com/maihcx/SYSi.git
cd SYSi

# Run directly
dotnet run --project SYSi/SYSi.csproj -c Release -r win-x64

# Or build the full single-file installer
build.bat
```

> The installer is output to `installer-output\SYSi.Installer.exe`

**How `build.bat` works:**
1. Publish `SYSi` → `installer-output/publish/`
2. Build `SYSi.Installer` — pass 1 with an empty placeholder payload
3. Package the published output into `payload.zip`
4. Build `SYSi.Installer` — pass 2 with the real payload embedded
5. Clean up all intermediate files

---

## Project Structure

```
SYSi/
├── Assets/                  # Application logo
├── Controls/                # Custom controls (InfoRow, ReferralCard, ...)
├── Helpers/                 # XAML value converters
├── Models/                  # Observable models (CpuInfo, GpuInfo, ...)
├── Resources/
│   └── Locales/             # String.en.resx / String.vi.resx
├── Services/
│   ├── HardwareService/     # Hardware readers (CPU, GPU, RAM, Storage, ...)
│   ├── HostServices/        # Background hosted services
│   └── UpdateService/       # GitHub release checker & downloader
├── Utils/                   # NativeMethods, NavigationHandle, ...
├── ViewModels/              # MVVM ViewModels per page
└── Views/
    ├── Pages/               # CpuPage, GpuPage, MemoryPage, ...
    ├── PagesBottom/         # SettingsPage, AboutPage
    └── Windows/             # MainWindow
```

---

## System Requirements

- **OS:** Windows 10 (1903 or later) or Windows 11
- **Architecture:** x64
- **.NET Runtime:** Bundled inside the installer — no separate installation needed

---

## License

This project is distributed under a custom license. See [LICENSE](LICENSE) (English) or [LICENSE.vi](LICENSE.vi) (Vietnamese) for details.

---

Made by **maihcx** · [GitHub](https://github.com/maihcx/SYSi)
