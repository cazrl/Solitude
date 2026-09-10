# Verification — Solitude 0.7.0

Built locally on 2026-09-08. This update corrects classic text rendering and FreeCell dialogs, and shares compatible settings across presets without changing suspended deals.

## Delivered executable

- `artifacts/release-v070/Solitude.exe`, file version **0.7.0.0**.
- Size: **58,806,627 bytes**.
- SHA-256: `4A9497988F5BFAE430D4766D510F044879B203FEB1C0BEDB64280CE8C3FEFD36`.
- Exactly one portable, self-contained Windows x64 EXE in that package folder.
- `dist/Solitude.exe` was running and Windows denied replacement. It remains **0.6.1.0**, with its previous hash unchanged. Close it before opening the update; both use the same personal save location.
- Previous package also retained at `artifacts/Solitude-0.6.1.exe`; previous report: [VERIFICATION-0.6.1.md](VERIFICATION-0.6.1.md).
- Local packaging only. No installation, upload, visible game launch or desktop input. Personal saves were not opened or changed by verification.
- Final publish passed. NU1900 reports that the online NuGet vulnerability-data endpoint was unavailable; this scan did not complete.

## Corrections

- Classic menu/dialog lettering uses solid, pixel-hinted glyphs. Bitmap System/MS Sans Serif strikes remain where appropriate; fractional sizes use GDI glyphs at the actual output resolution. Cached alpha masks keep clipped repaints stable. Vista retains smooth text.
- FreeCell Statistics follows the compact text-column layout instead of the invented group boxes. Options uses right-side OK/Cancel; Game Number has the short input and single OK; empty-column buttons are vertical; the win prompt has a Select game checkbox and Yes/No.
- Compatible preferences now carry across Windows presets and persist after closing the EXE. Display size is global, classic game options stay within compatible families, and Vista appearance carries across its games. Era-specific mechanics remain intact.
- Save format 5 separates shared choices from each deal's active rules. New deals use current shared choices; suspended deals and Restart Game retain their original rules. Changing only presentation options cannot accidentally apply a pending difficulty and discard progress. Formats 1–4 migrate with recovery backups.
- Fast FreeCell number entry followed by Enter reads the current text, including before a repaint. Invalid input cannot reuse an older valid number.
- The visible Settings button and isolated double-click behavior remain.

## Validation

- **50 rules/persistence verification groups passed**: `artifacts/rules-v070.log`.
- **1,625 production UI checks passed**: `artifacts/ui-v070.log`.
- Coverage includes all 17 profiles, four display sizes, menus/options, restored win-prompt controls, fast number input, shared settings across profiles and process restart, pending difficulty, unchanged suspended cards, same-rule Restart Game, save recovery, complete double-click event sequences, animation interruption, and partial/full repaint equivalence.
- Font pixel checks cover Windows 95, 2000 and XP at 100/125/150/200%, rejecting faded or color-fringed classic strokes.
- Development and packaged EXE each rendered **225 application views** plus **36 motion samples**. All **261 PNG hashes match**.
- Sampled visual inspection covered all four display sizes, early System lettering, Win95/XP FreeCell Statistics, Options, Game Number, Game Over and empty-column dialogs, Settings, Windows 2000 at 200%, and Vista Options.
- Packaged offscreen render and notice export exited 0. All four exported notices match their source files.
- Artifacts: `artifacts/renders-v070-dev`, `artifacts/renders-v070-package`, `artifacts/licenses-v070`, `artifacts/package-validation-v070.json`, `artifacts/package-v070-final.log`.

The offscreen production frame-pump benchmark delivered **120 updates/second** in the normal partial-redraw case, p95 interval **8.72 ms**. All six pacing cases passed the Windows-timer starvation guard. These are CPU/message-loop measurements, not measured monitor FPS or end-to-end input latency. Details: [PERFORMANCE.md](PERFORMANCE.md).

Original XP resource templates establish the FreeCell dialog structure. Reusing that structure for older classic releases remains a family-level inference. Full pixel/behavior identity across every original Windows build is not certified; Vista remains a less complete reconstruction. Evidence and remaining limits: [PERIOD-UI-AUDIT.md](PERIOD-UI-AUDIT.md), [MECHANICS-AUDIT.md](MECHANICS-AUDIT.md).
