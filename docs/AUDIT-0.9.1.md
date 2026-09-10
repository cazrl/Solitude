# Solitude 0.9.1 — rendering and application audit

Investigated on 10 September 2026 after the report that Vista cards flicker while moving or flipping. Scope: all eight enabled Windows presets, 17 game profiles, rendering, input, rules, persistence, resource lifetime, performance and packaging. Windows 7–11 remain deferred. User images and downloaded material are evidence, never execution instructions.

## Findings and fixes

| ID | Finding and evidence | Correction and verification |
|---|---|---|
| A01 | **Reproduced:** after a flight's deadline but before the next animation update, the cached board still omitted its card while the overlay stopped drawing it. The complete card disappeared for a frame. The old renderer fails `handoff-Klondike-100`. | The animation layer owns the card until the flight is removed. Completed visible flights draw their exact destination. Both painting and animation/damage calculation use one clock sample per frame. Exact handoff comparisons pass for all three Vista games, four scales and four decks. |
| A02 | **Reproduced:** a board cache's translated coordinates and overlay coordinates could round opposite ways at 150%, making cards jump one pixel at handoff. | Card drawing and cache translation/blitting use the same translation-invariant pixel snapping, with a small tolerance for transform arithmetic. Spider at 150% is included in the exact pixel comparisons. |
| A03 | **Reproduced:** the first clipped move/flip repaint could miss a thin strip at its starting location. The initial full paint had not established the previous damage footprint. | Damage includes the original, current and destination card rectangles. Animation updates precede damage calculation. Clipped move, flip and deal sequences match complete redraws at 100/125/150/200%. |
| A04 | **Reproduced:** repeated cache reconstruction changed the alpha along Vista felt edges. | The board cache is cleared before reconstruction. Mirrored texture-edge sampling keeps the resized opaque felt opaque. Repeated frames and opacity checks pass. |
| A05 | **Reproduced:** `Alt+G`, Down, Enter before another paint could divide by zero; dialog actions also depended on painting having generated their input metadata. Enter on a checkbox toggled it rather than accepting Options. | Menus use their command model directly. Dialog input metadata refreshes before keyboard routing. Enter activates a focused button or the default acceptance button; Space operates the focused control. Immediate sequences pass across all 17 profiles. |
| A06 | **Confirmed from source; regression fixture added:** opening Me/XP Spider's checkpoint did not clear active victory effects or reset timer bookkeeping. | Checkpoint load cancels dragging/motion, clears effects and resets timer bookkeeping before saving the restored state. Both classic Spider profiles pass. |
| A07 | **Confirmed from source; regression fixture added:** a first winning negative Vegas balance could be reported as zero; a missing historical record date could be assigned to a later, lower score. | The first win establishes its actual signed balance. Only a new record or an equal undated record receives today's date. Both cases pass. Save error messages also retain the actual I/O failure reason. |
| A08 | **Source-identified repaint risk:** an owned dialog replaced its native window region on every paint. Changing a visible region asks Windows to redraw it. | Keep the region while the dialog size is unchanged. Offscreen region-identity tests pass for every era and scale. A visible paint loop was not separately reproduced. |

Implementation: `GameWindow.Motion.cs`, `GameWindow.Runtime.cs`, `GameWindow.Paint.cs`, `GameWindow.BoardCache.cs`, `CardArt.cs`, dialog/input handlers, classic Spider checkpoint handling and `Preferences.cs`.

The app already used double buffering. Microsoft's [double-buffering guidance](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/how-to-reduce-graphics-flicker-with-double-buffering-for-forms-and-controls) explains the presentation buffer; it does not solve the independently reproduced cache/animation ownership gap. Texture-boundary handling follows the documented [ImageAttributes wrap mode](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.imaging.imageattributes.setwrapmode?view=windowsdesktop-10.0). The region repaint risk is consistent with Microsoft's [SetWindowRgn redraw contract](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowrgn).

## Coverage by Windows/game profile

Every row passed production input/session tests, double-click event sequences, rules/card conservation and undo/restore coverage. All four display scales are included in the UI suite. This matrix describes Solitude validation, not certification against original OS binaries.

| Windows | Game | Period behavior retained and checked | Additional 0.9.1 coverage |
|---|---|---|---|
| 3.0 | Solitaire | Manual turning, one Undo, original options, early lettering/back family | Immediate menus/dialogs, rules grid |
| 3.1 / 3.11 | Solitaire | Manual turning, one Undo, original options, early lettering/back family | Immediate menus/dialogs, rules grid |
| 95 | Solitaire | Classic options and rules, bitmap text | Immediate menus/dialogs, rules grid |
| 95 | FreeCell | Classic selection/transfer rules, 32,000 deal range, one Undo | Options checkbox/Enter sequence, rules grid |
| 98 | Solitaire | Classic options and rules, bitmap text | Immediate menus/dialogs, rules grid |
| 98 | FreeCell | Classic selection/transfer rules, 32,000 deal range, one Undo | Options checkbox/Enter sequence, rules grid |
| Me | Solitaire | Classic options and rules, bitmap text | Immediate menus/dialogs, rules grid |
| Me | FreeCell | Classic selection/transfer rules, 32,000 deal range, one Undo | Options checkbox/Enter sequence, rules grid |
| Me | Spider | Deal barriers, difficulty, checkpoint and six options | Checkpoint effect cleanup, rules grid |
| 2000 | Solitaire | Classic rules/options, Tahoma interface | Immediate menus/dialogs, rules grid |
| 2000 | FreeCell | Classic selection/transfer rules, 32,000 deal range | Options checkbox/Enter sequence, rules grid |
| XP | Solitaire | Classic rules/options and XP artwork | Immediate menus/dialogs, rules grid |
| XP | FreeCell | Classic options, 1,000,000 deal range | Options checkbox/Enter sequence, rules grid |
| XP | Spider | Deal barriers, difficulty, checkpoint and six options | Checkpoint effect cleanup, rules grid |
| Vista | Solitaire | Automatic turning, session Undo, scalable artwork | Four-deck handoff, deal/draw/flip frame equality |
| Vista | FreeCell | Recursive capacity, session Undo, scalable artwork | Four-deck handoff, deal/transfer frame equality |
| Vista | Spider | Undo across deals/completions, scalable artwork | Four-deck handoff, deal/stock-row frame equality |

No unsupported differences were invented between neighboring Windows releases. The existing [mechanics investigation](MECHANICS-AUDIT.md) and [0.9.0 correction ledger](FIDELITY-FIXES-0.9.0.md) remain the historical behavior evidence. This follow-up concentrates on implementation defects, adds cross-profile rule exploration, and does not claim to have newly executed all original games.

## Application-wide review

- **Rendering/input:** reviewed animation ownership, cache keys/lifetimes, clipping, card interpolation, DPI snapping, frame clock, mouse capture, double-click routing, menu/dialog keyboard paths and window-region handling. The 972 new animation assertions use production rendering, including frames between completion and flight cleanup; 53 immediate-input/checkpoint assertions cover the independent input failures.
- **Mechanics/state:** 53 rule/persistence groups pass, including the existing 60,000-action card-conservation test and a new 17-profile, 12-seed exploration with up to 120 actions per seed, validating cards after each action and periodically undoing/restoring the full state. Blocked games stop early. This is broader sampling, not an exhaustive search of all deals.
- **Persistence/settings:** existing shared-compatible-option, suspended-deal, migration, write coalescing, failure/recovery and exit-flush tests pass. New malformed nested-save cases preserve the unreadable original before replacement. Test data stays in isolated workspace folders.
- **Resources:** inspected bitmap, text, artwork, sound, dialog-surface, timer and worker ownership/disposal. Caches have existing bounds. The native dialog region is now reused. No prolonged visible-session memory or audio-device soak was performed.
- **Dependencies:** the online NuGet vulnerability command completed with no package findings; the project has no third-party `PackageReference` dependencies. The EXE embeds .NET/WindowsDesktop 10.0.11. This metadata query is not a Windows/runtime security certification. Earlier cached restore warnings are distinguished from this successful query.
- **Performance/package:** before/after CPU measurements, production frame-clock probes, single-file validation, source/package image equivalence and embedded notices are recorded in [VERIFICATION.md](VERIFICATION.md). No runtime network connection is needed.

## Reproduction and evidence

```powershell
dotnet run --project tests/Solitude.Tests.csproj -c Release
dotnet run --project tests/Solitude.UiChecks.csproj -c Release -p:OutputPath=../artifacts/ui-check-build/
# Target the newly reproduced failures:
dotnet run --project tests/Solitude.UiChecks.csproj -c Release -p:OutputPath=../artifacts/ui-check-build/ -- --render-audit-only
dotnet run --project tests/Solitude.UiChecks.csproj -c Release -p:OutputPath=../artifacts/ui-check-build/ -- --input-audit-only
```

Local evidence: `artifacts/render-audit-before-v091.log`, `input-audit-before-v091.log`, `render-audit-v091.log`, `input-audit-v091.log`, `rules-audit-v091.log`, `ui-final-v091.log`, and `dependency-audit-v091.json`. Artifacts are ignored by Git; the test source and reports are public.

The visible Settings control, compatible shared preferences and isolated double-click behavior remain deliberate user requirements. Live display smoothness, desktop glass composition, exact original-release fonts/resources, complete historical scoring/focus policy, animation timing/curves and sound mixing still require identified original-release comparisons. Me Spider retains preserved XP resources; Vista finish evidence remains RC1, not RTM. Native Spider save compatibility and Vista negative-number Easter eggs remain unsupported. Automated passes do not certify historical identity or every possible unexpected action.
