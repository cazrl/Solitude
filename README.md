# Solitude 0.9.0

Solitaire, FreeCell and Spider in one portable Windows EXE. Click **Settings...** at the top right to choose a Windows version and game. **F6** and the window icon menu remain available too. Windows 7–11 remain deferred.

| Windows version | Solitaire / Klondike | FreeCell | Spider |
|---|---|---|---|
| 3.0 | Yes | — | — |
| 3.1 / 3.11 | Yes | — | — |
| 95 | Yes | Yes | — |
| 98 | Yes | Yes | — |
| Me | Yes | Yes | Yes |
| 2000 | Yes | Yes | — |
| XP | Yes | Yes | Yes |
| Vista | Yes | Yes | Yes |

Windows 98's Spider belonged to the separate Plus! 98 add-on and is not offered as a base-Windows game.

## Run

**[Download Solitude.exe for Windows x64](https://github.com/cazrl/Solitude/releases/download/v0.9.0/Solitude.exe)** from the [0.9.0 release](https://github.com/cazrl/Solitude/releases/tag/v0.9.0).

The **0.9.0** package is [`dist/Solitude.exe`](dist/Solitude.exe), with an identical retained copy at [`artifacts/release-v090/Solitude.exe`](artifacts/release-v090/Solitude.exe). Older packages are preserved under `artifacts`.

Run the EXE on 64-bit Windows 10 or 11. It contains the runtime, artwork and notices. No installer, account, network connection or separate .NET installation is required. Native runtime components may extract into the temporary directory.

Settings selects the Windows version, game and display size. Compatible choices are shared across Windows presets and survive closing the EXE. Display size is global; Solitaire options, classic FreeCell options and Spider difficulty/options carry to versions that support them. Classic and XP card backs remain separate artwork families. Vista appearance, animation and save choices carry across its three games.

Each Windows/game combination retains its own suspended deal, undo history and records. Existing deals keep their original rules until you choose a new deal; Restart Game replays the same deal with those rules. Sharing a setting never copies Vista mechanics into a classic game. The visible Settings button is always present in the menu strip; **Records** and **About Solitude** are inside the selector.

## Period presentation

Version 0.9 addresses the complete audit: neutral foundation rearrangement scoring; unsaved in-memory sessions; classic FreeCell loss/one-move feedback, card inspection and Cards Left; dialog mnemonics and measured Options layouts; embedded early fonts; actual owned dialog windows; Vista sound/save/startup/restart/tip controls; restored completion effects; and full-session undo in the later games. Details and remaining historical limits: [FIDELITY-FIXES-0.9.0.md](docs/FIDELITY-FIXES-0.9.0.md).

Version 0.8.1 corrects the washed-out XP caption/button colours using sampled Blue Luna colour profiles and removes the broad white button overlay. See [XP-COLOUR.md](docs/XP-COLOUR.md) for the reference and measured comparison.

Version 0.8 fixes caption icons that ignored display scaling, aligns XP/Vista captions and menu labels, and uses preserved Vista button, glyph, frame and game-icon artwork. Vista's text mask now renders the requested Segoe UI face instead of substituted serif lettering. Caption painting and hit areas share one layout. The [frame alignment report](docs/FRAME-ALIGNMENT.md) records the reference measurements and remaining glass-compositing limitation. Settings stays visible.

The yellow message banner, quick-action toolbar, Game Feel dialog, FPS counter, artificial move-speed choices and generic confetti are removed. Old saves cannot re-enable them. Game titles and icons, menu commands, options and feedback vary by game and period.

**Classic Solitaire:** immediate card moves and stock draws, manual turning, one Undo, period card-back chooser and the original Draw/Scoring/Timed game/Status bar/Outline dragging/Keep score options. Illegal drops return immediately without a banner. Outline dragging inverts a legal destination. Timed early card backs and the classic bouncing-card win remain. Deal starts immediately.

**Classic FreeCell:** click a card, then a destination; no floating drag interaction. The selected bottom card is inverted. Its three Options are Display messages on illegal moves, Quick play (no animation), and Double click moves card to free cell. Illegal-move messages use an optional dialog. With Quick play off, column transfers flash through temporary free cells and spare columns; Quick play skips those steps. Moving to an empty column offers Move column, Single card and Cancel. Statistics show this session, totals and streaks.

FreeCell's Statistics, Options, game-number, empty-column and win dialogs follow the archived classic resource layouts. Statistics uses compact text columns; the win prompt has a Select game checkbox. Menus and dialogs use solid classic lettering at the selected display size. Windows 3.x/95/98/Me use embedded legacy bitmap strikes at every scale, including 125% and 150%, with stable pixel alignment during partial redraws. Windows 2000/XP use Tahoma; Vista retains smooth Segoe UI text.

**Classic Spider:** its own faces, back, tiled felt, Game/Deal!/Help menu and Score/Moves panel. Difficulty is separate from Options. Its six options control dealing animation, automatic save/open, save/open prompts and sound. Save This Game and Open Last Saved Game use a separate fixed checkpoint for that Windows preset. Statistics have Easy, Medium and Difficult tabs. Card moves are immediate; dealing can animate. Preserved Spider sounds are used when enabled.

**Vista:** scalable decks and backgrounds, automatic turning, full-session Undo, animated motion, Hint and Change Appearance. Options includes animation, sound, tips, saving at exit and continuing saved games. The close/startup questions, Restart command, completion effects and score breakdown are restored. Vista RC1 recordings support the Solitaire/Spider finish choreography; exact RTM equivalence remains unverified.

The frame clock still handles responsive dragging and active animations, and stops requesting frames on an idle board. This does not impose a simulated old-machine frame rate. Rendering and save work remain cached or off the input thread where possible.

## Mechanics and controls

Klondike supports draw one/three and Standard, Vegas or no scoring. Pre-Vista has manual turning and one Undo; Vista turns exposed cards automatically and retains the complete current-session Undo history. Keep score carries a Vegas balance across deals.

FreeCell uses the Microsoft numbered-deal shuffle. F3 accepts 1–32,000 through Windows 2000 and 1–1,000,000 in XP/Vista. Classic and Vista differ in sequence capacity and Undo. **Double-click moves only its chosen exposed card**, as requested; it does not sweep unrelated cards home. Ordinary manual FreeCell moves retain safe automatic home placement.

Spider uses 104 card identities, ten columns, five stock packets and eight completed same-suit King-to-Ace sequences. Empty columns block dealing. Actions and Undo cost points; completed runs add 100. Me/XP Undo stops at a deal or completed run; Vista can cross those boundaries. Difficulty records stay separate.

| Action | Classic Solitaire | Classic FreeCell | Classic Spider | Vista |
|---|---|---|---|---|
| New deal | F2 | F2 | F2 | F2 |
| Undo | Game menu; Ctrl+Z fallback | F10; Ctrl+Z fallback | Ctrl+Z | Ctrl+Z |
| F3 | — | Select Game | Difficulty | FreeCell Select Game |
| F4 | — | Statistics | Statistics | Statistics |
| F5 | — | Options | Options | Options |
| Hint | — | — | M; score panel | H |
| Save/open checkpoint | — | — | Ctrl+S / Ctrl+O | — |
| Card appearance | Game > Deck | — | — | F7 |
| Windows selector | Settings / F6 | Settings / F6 | Settings / F6 | Settings / F6 |

F1 opens game-specific help with a working keyword index from Windows 95 onward. Windows 3.x uses Help > Index and preserves the original inactive F1 shortcut. Escape cancels the current selection/menu/dialog. Restart is in the relevant Game menu. Spider also deals with D or Space. Klondike right-click performs safe foundation collection; its exact original scan order is not reproduced. Arrow/Tab/Enter/Space pile navigation remains as a keyboard fallback. FreeCell's number field supports digits, Backspace, Ctrl+A and Ctrl+V. Classic presets also accept the original secret deals -1 and -2.

## Saves

Data lives in `%LOCALAPPDATA%\Solitude\solitude.json`. Format 6 stores shared choices separately from each suspended deal's active rules. Formats 1–5 migrate automatically while retaining cards, records and supported undo history. On migration, the active preset's compatible settings take precedence; other saved presets supply the remaining game-specific choices. Format 4's separate Spider checkpoints and presentation cleanup remain. Format 1/2 Spider scores and obsolete Undo history receive the previously documented corrections. The old save is retained in the atomic replacement's `.bak` file. Unreadable files are preserved and recovery tries the backup. Save failures are reported.

Previous combined Spider totals remain available under **F6 > Records** because old saves cannot reconstruct their difficulty attribution. The game’s Statistics window displays the three difficulty tabs.

Moves queue a background save after a quiet period; dragging postpones writing until release. Closing flushes the newest snapshot. The host's per-preset suspension is separate from Spider's explicit save slot, so changing versions does not discard progress. An unsaved session remains available while switching presets; disabling save-on-exit only excludes it from disk persistence. Vista can ask to save once at exit, independently of its Always save preference, and can ask to resume at startup. Test runs use isolated folders and leave personal saves untouched.

Timers pause while menus/dialogs are open, minimized or unfocused. FreeCell numbers reproduce the Microsoft shuffle; Klondike and Spider use repeatable Solitude deals.

## Evidence and limits

The [period UI audit](docs/PERIOD-UI-AUDIT.md) records removed additions, restored controls and remaining differences. The [mechanics audit](docs/MECHANICS-AUDIT.md) maps all 17 combinations to sources and regression coverage. Neighboring Windows releases sometimes shipped the same game; unsupported differences are not invented.

This is a recreation, not original Windows running inside the EXE. Exact pixel identity, every dialog, loss prompt, sound event, animation timing and Vista behavior are not certified. The help viewer is reconstructed, and its text is a concise paraphrase. Me Spider uses preserved XP resources. Vista negative-number Easter eggs and native Spider save-file compatibility are not implemented. Vista win references are RC1 captures; exact RTM choreography, sound mixing and timings remain unverified.

## Build and diagnostics

With the .NET 10 SDK on Windows, run `./build.ps1`. It tests and packages the single EXE. Included assets do not require network downloads; missing SDK/runtime packages do.

```powershell
# Offscreen renders, with no game window shown.
.\dist\Solitude.exe --render .\artifacts\renders

# An isolated game session for manual testing.
.\dist\Solitude.exe --era WindowsXP --game FreeCell --seed 1 --scale 100 --data-dir .\artifacts\manual-state

# Export embedded notices.
.\dist\Solitude.exe --licenses .\artifacts\licenses

# Offscreen renderer and Windows message-queue benchmark.
dotnet run --project tests/Solitude.Benchmarks.csproj -c Release -p:OutputPath=../artifacts/benchmark-build/ -- artifacts/benchmark.json --pacing
```

Builds do not upload, install or publish a release. Asset sources and notices are in `docs/ASSET-SOURCES.md` and `THIRD_PARTY_NOTICES.md`.

## Repository contents and attribution

The repository includes the application source, embedded resources, tests, build tools and reports. Local saves, downloaded reference binaries/videos, working captures and build outputs are excluded. Paths under `artifacts/` and `references/` in historical reports refer to local evidence, not files distributed with this repository. The packaged EXE is provided through GitHub Releases.

Solitude is an independent recreation and is not affiliated with Microsoft. Third-party assets retain their original ownership; publication does not grant an open-source license to Microsoft artwork, audio or fonts. Redistribution rights for those assets have not been independently established. See [third-party notices](THIRD_PARTY_NOTICES.md) and [asset provenance](docs/ASSET-SOURCES.md). No repository-wide open-source license is asserted.
