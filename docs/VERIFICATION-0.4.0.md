# Verification - Solitude 0.4.0

Built locally on 2026-09-08. Improves active frame pacing, painting cost, card motion, drag/drop feedback, collection and saving across the 17 existing Windows/game combinations. Adds the Feel dialog and optional quick action bar. Windows 7-11 remain deferred.

## Delivered executable

- File: `dist/Solitude.exe`, version `0.4.0.0`.
- Size: **58,460,515 bytes** (58.5 MB).
- SHA-256: `5019195D6432C19CA4AC94AAFD958BB757954C7BA75B776A444ADDC5E06AAFEF`.
- Self-contained .NET 10, Windows x64, compressed single-file bundle with native dependencies, images and notices embedded.
- Distribution directory contains exactly one file.
- The packaged EXE rendered every supported profile successfully and exported all four embedded notice/license files. The build completed without compiler errors. Restore reported NU1900 because the NuGet vulnerability service was unreachable; a current online package audit was not completed.
- Previous EXE retained at `artifacts/Solitude-0.3.0.exe`; its verification record is `docs/VERIFICATION-0.3.0.md`.

## Rule and persistence verification

**44 verification groups passed, zero failed** in `tests/Solitude.Tests.csproj`.

The existing Klondike tests cover 500 initial deals, deterministic stock order, lawful sequences/foundations, manual and automatic turning, Standard/Vegas scoring, pass limits, elapsed time/undo penalties, safe collection, wins, saves and damaged-save recovery. Simulation checks exercise up to 40,000 Klondike actions.

New variant checks cover:

- The 17 available Windows/game combinations and FreeCell deal-number ranges.
- Every card in all eight columns of Microsoft FreeCell deal #1, plus repeatability and bounds.
- Four single-card free cells, immutable home cells, safe auto-home, sequence capacity profiles and grouped undo.
- Spider's 104 unique card identities across one/two/four suits; 54-card deal and five stock packets.
- Mixed-suit placement versus same-suit sequence moves, automatic turning, removal of complete King-to-Ace runs, scoring, undo and all-eight-run wins.
- Rejection of stock deals with an empty column, corrupted copies and mismatched game rules.
- Up to 60,000 additional simulated FreeCell/Spider actions with card/pile validation after every action.
- Loading the actual old save shape without new fields, preserving the deal and statistics, adopting the classic one-step undo limit, and retaining the old file as the backup when writing format 2.
- Background saves coalesce pending snapshots, flush the newest deal before exit, preserve backups and report/recover from an unwritable destination. The test's obstruction file remains preserved.

## Window and interaction verification

**356 checks passed** in `tests/Solitude.UiChecks.csproj`. These instantiate hidden production windows and call their production handlers; they do not send desktop input.

Coverage includes the eight historical frames, raster fonts, inactive captions, press/release cancellation, window regions, modal dragging and maximize/restore. Every supported Windows/game combination is checked for game selection, correct column count, available-game controls and session round trips.

The checks exercise FreeCell click-to-cell, drag-to-cell, undo and typing game #11982; Spider row dealing, undo and changing to four suits; Klondike stock drawing; closing/reopening separate FreeCell/Spider sessions with undo history; cumulative Vegas balances without converting Standard points into dollars; one-step classic undo; timed Robot animation and exclusion of that animation from XP photographic backs.

New checks verify movement and flip endpoints in early/XP/Vista renderers, interrupted undo preserving position and width, reduced motion, Feel settings, invalid drops returning from the pointer without altering game state, and valid drops settling from their actual release point. Forty-eight pixel comparisons verify that incremental drag frames exactly match full renders at 100%, 125%, 150% and 200% across classic, XP and Vista.

No personal save was used or altered. Persistent tests use uniquely named folders under `artifacts`.

## Packaged renderer and visual review

The **shipped EXE** generated **244 application views** plus **36 motion samples** in `artifacts/release-v040-renders/` and exited with code 0:

- All 17 Windows/game combinations at 100%, 125%, 150% and 200%.
- Settings, Feel, options, game-specific help/about, statistics, menus, inactive and maximized-style frames.
- Applicable card/appearance pickers, FreeCell number dialogs and classic expanded options.

Representative boards, early/XP/Vista Feel dialogs and deal-motion samples were visually inspected at full size. The motion samples show six stages of the XP/Vista deal for each game. The previous game comparison remains in `artifacts/games-comparison.png` and depicts 0.3, before the optional action bar.

All 244 packaged application views exactly match the reviewed development renders by SHA-256. The packaged Vista FreeCell board was also inspected directly. License export completed with exit code 0 and produced all four embedded notice files.

The maximized render flag checks frame treatment; handler tests separately verify actual window bounds/region changes. It is not a screenshot of a physical maximized desktop window.

## Performance and pacing

The warm full-window Vista Spider stress case dropped from 102.60 ms to 8.55 ms median CPU paint time. The offscreen partial-drag test delivered 60.0, 120.0 and 144.0 updates per second at the corresponding limits, with ordinary Windows timer messages continuing in every case. Forced full-window drawing fell below the higher limits while remaining responsive. See `docs/PERFORMANCE.md` for all six profiles, p95 results, methodology and raw artifact paths.

These results measure local production painting and frame-clock dispatch. They do not certify physical monitor FPS, VSync or end-to-end input latency. The timer probe and event-handler tests use hidden/message-only windows; no visible interactive play session was performed.

## Historical fidelity and limits

- Original preserved assets now distinguish classic Spider (faces, back, felt, About image), classic FreeCell (king portraits) and Klondike. Vista uses the four preserved decks and five backgrounds, with Seasons as the new-profile default.
- The Windows versions share games where the originals did. Spider is available in Me, XP and Vista. Windows 98's separate Plus! add-on is outside this base-Windows list.
- This is a recreation, not execution of original game binaries. Exact pixel equivalence across OS builds/themes is not certified. Title icons and some dialogs/controls remain reconstructed; the custom Settings/Feel/help/save extensions and optional action bar are intentional.
- Vista glass does not blur the live desktop. Card motion and early-back animation cadence are reconstructed. Original Vista victory effects and FreeCell negative-number easter eggs are not reproduced. Classic Klondike retains its bouncing-card win animation; the other modes now use a short confetti celebration before the win dialog.
- The classic FreeCell sequence-capacity policy follows documented Microsoft limitations, but was not compared against a running original binary for every empty-column arrangement. Me's Spider currently uses the preserved XP resource sheet; binary asset identity between Me and XP is unverified.
- Spider statistics are shared across difficulty settings within each Windows/game session. Klondike/Spider deal numbers use Solitude's shuffle; FreeCell uses Microsoft's numbered shuffle. Vista undo retains at most 200 states.
- No desktop control was performed. Hidden event-handler checks do not replace physical mouse/keyboard testing or establish taskbar/minimize integration on a visible packaged window. The earlier Escape stop remains respected.
- A clean machine without .NET was not available. Successful packaged execution and bundle configuration verify this build here; an independent clean-machine test remains outstanding.
- No upload, release publication or system installation was performed. Artwork provenance and rights limitations are in `ASSET-SOURCES.md` and the embedded notices.
