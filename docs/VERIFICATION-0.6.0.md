# Verification — Solitude 0.6.0

Built locally on 2026-09-08. Scope: Windows 3.0 through Vista, 17 Windows/game combinations. Windows 7–11 remain deferred.

## Delivered executable

- File: `dist/Solitude.exe`, version **0.6.0.0**.
- Size: **58,790,243 bytes**.
- SHA-256: `0D4C9A2E135A013B8C7EE67DAE2E82012C25C25EF186C1BD956D38D1C9745835`.
- Compressed self-contained .NET 10 Windows x64 single-file bundle. Runtime, artwork and notices are embedded; native components may extract to the temporary directory.
- Exactly one file in `dist`. Previous executable: `artifacts/Solitude-0.5.0.exe`. Previous source snapshot: `artifacts/source-v050`.
- Previous reports: [VERIFICATION-0.5.0.md](VERIFICATION-0.5.0.md), [MECHANICS-AUDIT-0.5.0.md](MECHANICS-AUDIT-0.5.0.md).
- Build completed without compiler errors. NU1900 reports that the NuGet vulnerability service was unreachable; a current online package audit was not completed.
- Local packaging only. No upload, release publication, installation, visible game launch or desktop input.

## Changes verified

Removed the yellow status banner, quick toolbar, Game Feel/FPS controls, Options expansion and generic confetti. Replaced common controls with game-specific menus, options, statistics and feedback. Windows/game/scale selection moved to the window menu and F6.

Restored classic FreeCell click-to-select, its three options, optional illegal-move dialog, Cancel on the empty-column choice and temporary free-cell transfer animation. Restored classic Spider's separate Difficulty dialog, six options and independent save/open checkpoint with prompts. Classic Solitaire moves instantly; Vista keeps animated motion. Original data-only game icons and Spider sounds are embedded.

The [period UI ledger](PERIOD-UI-AUDIT.md) records evidence and limits. The [mechanics audit](MECHANICS-AUDIT.md) documents all 17 profiles and the retained double-click isolation.

## Rules and persistence

`artifacts/rules-v060.log`: **50 verification groups passed, zero failed**.

Coverage includes 500 initial deals, up to 40,000 Klondike actions and 60,000 variant actions; card conservation; stock passes/scoring; legal movement; Microsoft FreeCell deal 1; classic/Vista sequence capacity; normal auto-home versus chosen-card shortcuts; Spider deal/completion Undo barriers and repeated Undo costs; win recognition; malformed saves; format migration; backup recovery; background save ordering and error recovery.

## Production UI handlers

`artifacts/ui-v060.log`: **1,534 checks passed** using hidden production forms. No windows were shown and no desktop input was sent.

Coverage includes all 17 profiles; every full double-click event sequence at 100/125/150/200%, with and without animation; no extra draws or unrelated ace movement; no flip-then-home action; negative selection pixels; period menus/options; disabled controls; game-number input; session switching/persistence; interrupted Vista animation; dragging and clipped-frame equivalence; frame/title/font distinctions; modal drag and maximize/restore.

The new period suite verifies exact classic menu order/labels/accelerators against the XP reference contract, retired-overlay pixel equivalence even with old flags set, legacy preference migration without dropping cards or records, working help-index filtering, FreeCell option behavior, temporary cell transfers, Spider deal-animation toggling, save overwrite/cancel/restore, automatic save on close, independent per-Windows checkpoints and invalid checkpoint recovery.

After the final illegal-move dialog geometry adjustment, `artifacts/ui-period-v060.log` repeats **215 focused period checks**, all passing. This count is a focused repeat, not additional unique coverage.

## Render and package checks

- Development renders: `artifacts/renders-v060-dev`.
- Packaged EXE renders: `artifacts/renders-v060-package`.
- **215 application views**: every supported game/preset at four sizes, plus available settings/options/help/about/statistics/difficulty/appearance/number dialogs, game menus and inactive/maximized frames.
- **36 motion sample frames** across XP/Vista. Classic Solitaire/FreeCell initial deal samples are intentionally static.
- All **215 packaged static PNGs exactly match the development PNG hashes**.
- Sampled visual inspection covered early Solitaire options, XP Solitaire/Spider menus, FreeCell options/statistics/illegal-move dialog, Spider options/difficulty/statistics, classic Help and Vista options. It caught clipped Spider labels; those were corrected and re-inspected.
- Final FreeCell feedback capture: `artifacts/period-details/freecell-illegal.png`.
- Packaged `--render` and `--licenses` both exited 0.
- All four embedded notice files exported. Packaged `THIRD_PARTY_NOTICES.md` hash matches the source.

## Performance

`artifacts/benchmark-v060.json` exercises the production renderer and a message-only window. All six pacing cases pass the timer-starvation guard. At the normal 120-update target, the partial Vista Spider workload delivered approximately **119 updates/second**, p95 interval **8.74 ms**. These are offscreen message/render measurements, not display FPS or input latency. Detailed medians, tails and limits: [PERFORMANCE.md](PERFORMANCE.md).

## Remaining authenticity limits

Checks establish this implementation's behavior, not exact identity with every original Windows release. No downloaded original executable was run. Chrome is painted, Help is reconstructed, Vista remains simplified, and original loss prompts, complex classic FreeCell animation paths, per-release sound/icon details, Spider native save compatibility and several timing/scoring comparisons remain gaps. See the two audits for the complete ledger.

Personal saves were not used in tests or rewritten. The next ordinary launch migrates the existing save through the validated loader and retains atomic backup/recovery behavior.
