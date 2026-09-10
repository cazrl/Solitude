# Verification — Solitude 0.6.1

Built locally on 2026-09-08. This release restores discoverable Windows/game selection after the 0.6 interface cleanup.

## Delivered executable

- `dist/Solitude.exe`, file version **0.6.1.0**.
- Size: **58,790,755 bytes**.
- SHA-256: `E8979F5B27B6A6976616F9B3695AF2B90D860F98480AD3A7CF03BF0E2C2CB945`.
- Exactly one portable, self-contained Windows x64 EXE in `dist`.
- Previous EXE retained at `artifacts/Solitude-0.6.0.exe`; previous report: [VERIFICATION-0.6.0.md](VERIFICATION-0.6.0.md).
- Local packaging only; no installation, upload, visible game launch or desktop input.
- Build passed. NU1900 still reports that the online NuGet vulnerability-data endpoint could not be reached.

## Correction

An always-visible **Settings...** button at the top right of the menu strip opens the Windows version, game and display-size selector. It uses the selected period's button style and works even when the Game menu is open. The dialog is titled Settings. F6 and the window-menu entry remain available.

Game rules, saved data format and per-preset sessions are unchanged. Personal saves were not touched. README instructions now lead with the visible button.

## Validation

- **1,569 production UI checks passed**: `artifacts/ui-v061.log`.
- Existing Windows-selection tests now open Settings using the visible button. Added checks exercise that button across all 17 profiles, opening it over an active Game menu, and using only mouse clicks to change Windows 3.1 Solitaire to Windows XP FreeCell.
- Existing game/session, complete double-click, animation and partial-repaint regressions passed in the same suite.
- Development and packaged EXE each rendered **215 application views** plus **36 motion samples**. All 215 packaged static PNG hashes match the development output.
- Sampled visual inspection: Windows 3.0 at 100%, XP at 150%, and the XP Settings selector. The button and Windows/game choices are readable.
- Packaged offscreen render exited 0. Artifacts: `artifacts/renders-v061-dev`, `artifacts/renders-v061-package`.
- No engine or persistence code changed; the previous release's 50 rules/persistence groups and performance measurements were not unnecessarily repeated.

Historical evidence and remaining reconstruction limits are unchanged: [PERIOD-UI-AUDIT.md](PERIOD-UI-AUDIT.md), [MECHANICS-AUDIT.md](MECHANICS-AUDIT.md). The visible Settings control is an intentional host convenience requested by the user.
