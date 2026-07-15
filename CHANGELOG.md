## v0.10.7

## 🚀 Changelog
## 🧹 Maintenance

- Add LiquidGlass shader and AcrylicPanel refactor (#157) — @maihcx
- Bump package versions to 10.0.10 (#156) — @maihcx


---

## v0.10.6

## 🚀 Changelog
## 🐛 Bug Fixes

- Initialize window backdrop type from theme service (#147) — @maihcx

## ⚡ Performance

- Update Library and Optimize installer (#151) — @maihcx

## 🧹 Maintenance

- Add AcrylicPanel control and use it in HomePage (#150) — @maihcx
- Remove unused using statements (#149) — @maihcx
- Update app icons and logos (#148) — @maihcx


---

## v0.10.5

## 🚀 Changelog
## 🐛 Bug Fixes

- Application crashes with ES CPU (#145) — @maihcx

## ⚡ Performance

- Optimize memory after theme changes (#144) — @maihcx


---

## v0.10.4

## 🚀 Changelog
## 🐛 Bug Fixes

- Add BaseClockMHz to ES sample matching (#137) — @maihcx

## ⚡ Performance

- Rename memory optimizer to timed OptimizeAfterAsync (#142) — @maihcx

## 🧹 Maintenance

- Update Intel and AMD chipset database (#140) — @maihcx
- Reorganize and extend CPU rules database (#139) — @maihcx
- Expand CPU TDP database entries (#138) — @maihcx


---

## v0.10.3

## 🚀 Changelog
## ⚡ Performance

- Add MemoryOptimizer and use for trimming (#135) — @maihcx


---

## v0.10.2

## 🚀 Changelog
## 🐛 Bug Fixes

- Use GetSystemAppTheme instead of GetAppTheme (#133) — @maihcx
- Pass backdrop type when applying system theme (#131) — @maihcx


---

## v0.10.1

## 🚀 Changelog
## 🐛 Bug Fixes

- Fix WindowsUpdateStatus and ActivationStatus color on HightContrast mode (#127) — @maihcx

## ⚡ Performance

- Optimize PerformanceChart rendering & add scrolling (#124) — @maihcx

## 🧹 Maintenance

- Add package metadata and include README in csproj (#130) — @maihcx
- Enhance detection and labeling for Intel engineering samples (#126) — @maihcx
- Bump package versions to 10.0.9 (#125) — @maihcx


---

## v0.10.0

## 🚀 Changelog
## 🚀 Features

- Add GPU usage history and chart (#122) — @maihcx
- Add RAM usage history and chart (#120) — @maihcx
- Add CPU usage history and chart (#119) — @maihcx
- Add PerformanceChart control (#118) — @maihcx

## 🐛 Bug Fixes

- Subscribe to hardwareHostService PropertyChanged (#121) — @maihcx
- Fix refresh interval paused handling and storage for PR#116 (#117) — @maihcx

## 🧹 Maintenance

- Use TimeSpan for refresh intervals (#116) — @maihcx


---

## v0.9.2

## 🚀 Changelog
## 🐛 Bug Fixes

- Run host start/stop synchronously on app lifecycle (#114) — @maihcx


---

## v0.9.1

## 🚀 Changelog
## ⚡ Performance

- Improve scrolling smoothness (#110) — @maihcx

## 🧹 Maintenance

- Enable smooth scrolling for release notes (#112) — @maihcx


---

## v0.9.0

## 🚀 Changelog
## 🚀 Features

- Add SYSi.BugTracker crash reporter and modernize UI (#101) — @maihcx

## 🐛 Bug Fixes

- Exclude non-physical network adapters (#105) — @maihcx

## 🧹 Maintenance

- Include BugTracker in installer build (#107) — @maihcx
- Add hardware DBs and refactor lookups (#104) — @maihcx
- Add HardwareDatabase and refactor CPU logic (#103) — @maihcx
- Remove empty catches (#102) — @maihcx
- Centralize DataDir in AppInfoHelper (#100) — @maihcx
- Refactor HardwareService and ViewModels (#99) — @maihcx
- Use SymbolIcon for InfoRow copy button (#98) — @maihcx
- Adjust GPU page spacing and mark last InfoRow (#97) — @maihcx


---

## v0.8.0

## 🚀 Changelog
## 🚀 Features

- Add monitor support and normalize CPU socket (#93) — @maihcx

## 🐛 Bug Fixes

- Add monitor support and normalize CPU socket (#93) — @maihcx
- Gracefully stop pipe thread with CancellationToken (#91) — @maihcx
- Fix deviceInfoSet check and motherboard mappings (#89) — @maihcx

## ⚡ Performance

- Cache CPU base MHz and make OS info load async (#92) — @maihcx
- Use Dispatcher.InvokeAsync for property notifications (#90) — @maihcx

## 🧹 Maintenance

- Add monitor names and responsive GPU display UI (#95) — @maihcx


---

## v0.7.0

## 🚀 Changelog
## 🚀 Features

- Enhance RAM info, UI and converters (#85) — @maihcx
- Display CPU code name, TDP, instructions & boost (#83) — @maihcx
- Support chipset/southbridge detection and display (#81) — @maihcx
- Add BIOS microcode support (#80) — @maihcx

## 🐛 Bug Fixes

- Wrap TimerStop in try/catch (#82) — @maihcx

## ⚡ Performance

- Use virtualizing ListBox for RAM slots & Storage disk (#86) — @maihcx
- Invoke PropertyChanged per hardware task (#84) — @maihcx

## 🧹 Maintenance

- Remove AutoHideNavPanelChanged delegate/event (#79) — @maihcx
- Add NavigationPanelHostService and nav model (#78) — @maihcx


---

## v0.6.0

## 🚀 Changelog
## 🚀 Features

- Add configurable refresh interval (#69) — @maihcx

## 🐛 Bug Fixes

- Remove extra space in filename (#66) — @maihcx

## ⚡ Performance

- Set refresh interval on startup; update ViewModel (#76) — @maihcx
- Set application theme on startup; remove Watch (#74) — @maihcx
- Use invariant culture for parsing width (#70) — @maihcx
- Add update type enum and localize OS status (#68) — @maihcx
- Parallelize hardware snapshot and optimize IO (#67) — @maihcx

## 🧹 Maintenance

- Remove _isInitialized and eagerly initialize viewmodels (#73) — @maihcx
- Adjust virtualization offset; null-safe InfoRow updates (#72) — @maihcx
- Add .editorconfig and apply code cleanup (#71) — @maihcx
- Remove unused _navigationWindow field (#65) — @maihcx


---

## v0.5.1

## 🚀 Changelog
## 🐛 Bug Fixes

- Use WMI for CPU base speed and remove SMBIOS (#63) — @maihcx

## 🧹 Maintenance

- Remove INavigationWindow methods and OnClosed (#62) — @maihcx


---

## v0.5.0

## 🚀 Changelog
## 🚀 Features

- Persist navigation pane open state (#60) — @maihcx
- Add Network page, viewmodel and localization (#52) — @maihcx

## 🐛 Bug Fixes

- Add CPU virtualization detection (#55) — @maihcx

## 🧹 Maintenance

- Introduce IWindow and refactor window hosting (#59) — @maihcx
- Refactor XAML layout and spacing in pages (#57) — @maihcx
- Add CPU ShortName and bind to UI (#56) — @maihcx
- Update WPF UI library binaries (#51) — @maihcx


---

## v0.4.2

## 🚀 Changelog
## 🧹 Maintenance

- Use CardAction and make GPU layout responsive (#49) — @maihcx
- Add PDH CPU clock, refresh and UI updates (#47) — @maihcx
- Use dynamic ControlCornerRadius for list items (#45) — @maihcx
- Remove unused timer and loadingText fields (#44) — @maihcx


---

## v0.4.1

## 🚀 Changelog
## 🧹 Maintenance

- Localize OS activation and update statuses (#42) — @maihcx
- Refactor GPU service and add DXGI LUID map (#41) — @maihcx


---

## v0.4.0

## 🚀 Changelog
- Updated version (#40) — @maihcx

## 🚀 Features

- Add OS page and OS info support (#38) — @maihcx
- Add ValueContent and content-based visibility (#37) — @maihcx
- Map GPU PDH counters and add GPU usage UI (#34) — @maihcx

## 🐛 Bug Fixes

- Map RDNA4 device IDs to GDDR6 (#32) — @maihcx

## 🧹 Maintenance

- Add icons to page headers and resize about logo (#39) — @maihcx
- Convert WidthToColumnsConverter to IMultiValueConverter (#36) — @maihcx
- Add OsHostService/OsInfo and use in HomeView (#35) — @maihcx
- Add global usings and relocate HostServices (#33) — @maihcx


---

