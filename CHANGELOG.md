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

