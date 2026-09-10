# Verification — Solitude 0.5.0

Built locally on 2026-09-08. Scope: Windows 3.0 through Vista, 17 Windows/game combinations. Windows 7–11 remain deferred.

## Delivered executable

- File: `dist/Solitude.exe`, version **0.5.0.0**.
- Size: **58,468,707 bytes**.
- SHA-256: `AC2F00478CDA8ECCA67CB09208B39C3025FF2C0AD63679F942ADA97DC5E1F208`.
- Compressed self-contained .NET 10 Windows x64 single-file bundle. Runtime, artwork and notices are embedded; native components may extract to the temporary directory.
- Previous EXE retained at `artifacts/Solitude-0.4.0.exe`; previous verification retained in [VERIFICATION-0.4.0.md](VERIFICATION-0.4.0.md).
- Build completed without compiler errors. Restore reported NU1900 because the NuGet vulnerability service was unreachable; a current online package audit was not completed.
- Local packaging only: no upload, release publication or installation.

## Corrections

Double-click shortcuts move one chosen exposed card. The second mouse-down in a double-click no longer repeats a draw, flip or placement. Stale hit rectangles are bounds-checked, and a covered FreeCell card cannot silently substitute the column's bottom card. Normal manual FreeCell moves retain safe automatic home placement; explicit Collect advances one card per animation step.

Classic Spider now stops Undo at stock deals and completed runs. Vista can undo across those boundaries. Undo retains prior move costs, removes a reversed run's reward, and cannot manufacture points by repeated completion. Difficulty statistics and streaks are separate; earlier combined totals remain in Overall.

Windows 2000/XP text is drawn at display resolution. Old bitmap fonts normalize unsupported punctuation to readable ASCII. Classic FreeCell uses negative-image selection, card-back double-click applies and closes, Keep score is disabled outside Vegas, and the menu-only layout is the default. Modern Feel controls remain available.

The previous build's existing close-error log revealed repeated disposal trying to save through an already disposed writer. Cleanup is now idempotent. The original log is preserved as `artifacts/Solitude-0.4-close-error-20260908.txt`; it was moved out of the distribution directory after inspection, not silently deleted.

## Rules and persistence

**50 verification groups passed, zero failed** in `tests/Solitude.Tests.csproj`.

Coverage includes 500 initial Klondike deals, valid sequences and foundations, stock order and draw-three remainder, hidden-card turning, Standard/Vegas/None scoring, timed penalties, safe collection, winning, undo, save/reopen and damaged-save recovery. Per-preset tests check recycle scoring and Vegas pass limits in all eight Klondike profiles.

FreeCell coverage checks Microsoft deal #1 card-for-card, deal bounds/repeatability, sequence-capacity profiles, four single-card cells, immutable home cells, safe automatic-home grouping, isolated shortcuts and one-card collection steps. Spider coverage checks all three suit counts, 104 unique identities, stock packets, empty-column restrictions, sequence movement, automatic turning, completed runs and eight-run wins. Classic/Vista history barriers are tested both during play and when restoring history. Repeated Undo and run-completion cycles check score integrity.

Simulation loops validate cards/piles after every action, up to 40,000 Klondike and 60,000 FreeCell/Spider actions (games with no further action stop early).

Format 1/2 migration tests preserve current/stored cards, totals and original backup bytes, correct Spider score/history, and adopt format 3. Difficulty snapshots are deep-cloned and survive JSON round trips. Background-save checks cover coalescing, final flush, backup preservation, write failure and recovery.

## Production UI handlers

**1,295 UI checks passed** in `tests/Solitude.UiChecks.csproj`.

The suite instantiates hidden production windows and invokes their real handlers. It sends no desktop input. Every profile is tested at four sizes, with animations on/off, using down/up/down/double-click/up. Assertions cover:

- One chosen card and unchanged unrelated eligible aces.
- One stock packet per double-click, including missing intermediate repaints.
- Hidden-card flip without an extra home move.
- No covered-card substitution, and correct Undo of the chosen move.
- Era-specific game controls, numbered deals, selection, dragging, switching and session persistence.
- Card-back double-click across all seven classic presets.
- Negative-image FreeCell selection using pixel checks in raster/Tahoma paths.
- Disabled Keep score controls and old-font punctuation equivalence.
- Spider difficulty tabs and charging an abandoned game to its old difficulty.
- Repeated disposal with a pending/late dirty flag; the saved move remains readable.

Existing frame regions, captions, press/release cancellation, modal dragging, maximize/restore, motion endpoints, flips, interrupted Undo, invalid-drop return, drag settling, reduced motion and Feel settings remain covered. Forty-eight comparisons require partial drag frames to equal full frames at 100/125/150/200% for classic, XP and Vista renderers.

All persistent tests use isolated `artifacts` folders. No personal save was inspected or altered.

## Rendering and performance

Development renders in `artifacts/v050-checked-renders/` contain 244 application views and 36 motion samples. Representative Windows 3.x menus/settings, 2000/XP text/options, classic/Vista Spider statistics, FreeCell selection and Vista dialogs were inspected as images. A visual review caught the bitmap punctuation defect before delivery.

[PERFORMANCE.md](PERFORMANCE.md) contains the current measurements and methodology. XP full-window paint medians were 1.81/1.97/3.11 ms for Solitaire/FreeCell/Spider. Partial Vista Spider drag tests delivered 60.0/120.0/144.0 updates per second; forced full redraws fell below the higher limits without starving ordinary timer messages. These are local offscreen results, not measured monitor FPS.

The **packaged EXE** rendered all **244 application views** plus **36 motion samples** into `artifacts/release-v050-renders/` and exited with code 0. All 244 application PNGs exactly match the reviewed development renders by SHA-256. The packaged XP Options dialog was also inspected directly. License export exited with code 0 and produced all four embedded notices in `artifacts/release-v050-licenses/`. The final distribution directory contains exactly one file, `Solitude.exe`.

## Historical confidence and limits

[MECHANICS-AUDIT.md](MECHANICS-AUDIT.md) maps all 17 combinations to original documentation, firsthand investigations, resource strings, implementation tests and remaining comparison gaps. Reference executables were parsed as data only and are not embedded as runnable games.

This remains a recreation. Exact pixel/behavior identity across every OS patch/theme has not been certified. Me Spider uses preserved XP artwork; its exact original binary behavior, some classic multi-column FreeCell cases, XP Spider stock-deal scoring, and full Vista scoring/auto-home/animation parity remain open comparisons. Settings/Feel, per-preset suspension, modern animation and the isolated FreeCell double-click policy are deliberate additions. Native Spider save-file compatibility, original loss prompts and all original shortcuts are not implemented.

No visible interactive play test was performed, respecting the earlier desktop-control stop. Hidden tests do not establish physical input latency, taskbar integration or behavior on a clean computer without .NET. Packaged execution verifies this bundle on the current machine.
