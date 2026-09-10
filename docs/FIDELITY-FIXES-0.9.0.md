# Fidelity corrections — Solitude 0.9.0

9 September 2026. This implements the actionable defects from the [0.8.1 audit](FIDELITY-AUDIT-2026-09-09.md). It covers the existing eight Windows presets and 17 game profiles. Windows 7–11 remain deferred. The original audit is preserved as a record of the pre-fix behavior.

The scoring, session-loss, input and missing-control defects are corrected. Historical reconstruction remains subject to the specific evidence limits below; passing regression checks does not certify pixel identity with every original Windows release.

## Audit disposition

| Finding | Change | Verification and historical boundary |
|---|---|---|
| F01 — foundation score farming | Moving a card between foundations is score-neutral. The first legal move home retains its normal reward. | Repeated 100-transfer sequences across all eight Klondike presets in Standard and Vegas. Exact original acceptance of the gesture is not independently certified. |
| F02 — lost session when switching presets | In-memory suspension always keeps the deal and Undo history. Save-on-exit only governs disk persistence. | All 17 profiles retain identical cards/history after switching away and back with saving disabled; disk snapshots exclude those games. Shared compatible preferences remain separate from active deal rules. |
| F03 — classic FreeCell loss and warning | Counts available moves, flashes the caption when one remains, records a loss once when none remain, and opens the original Game Over replay choices. | All five classic FreeCell presets exercise one/zero-move fixtures. Secret game -1 reaches the loss prompt through four legal moves. The general blocked fixture is structurally valid; its reachability from an ordinary numbered deal is not claimed. Exact warning cadence is reconstructed. |
| F04 — FreeCell inspection and counter | Holding the right mouse button reveals the full buried card until release/capture loss. Cards Left appears beside the retained Settings access. | Held/released render and input checks across the classic presets. The original feature is supported by Keller's investigation and the preserved XP string. The requested Settings control necessarily changes the available menu-strip space. |
| F05 — keyboard omissions | Space operates Solitaire selection/placement and stock; dialog access keys, Alt+Q and radio-group arrow keys work. Spider accepts D for dealing. | Production input dispatch checks, including access keys and group selection. Every historical accelerator across every release has not been independently catalogued. |
| F06 — classic Options layout | Restores the measured 3.x and 95-family dimensions, Keep score placement, spacing and access-key underlines. Embeds System/MS Sans Serif bitmap strikes for the early presets at all four scales. | 278×230 early layout; 342×216 classic layout; XP's 27-pixel caption makes its outer height 225. Reference comparison, rendered views and clipped/full repaint checks. Source font strike identity against each historical release remains unverified. |
| F07 — Vista flow | Restores sound, tips, independent save/continue controls, one-time exit saving, startup resume questions, Restart, event sounds and game-specific help. Compatible Vista options carry among its three games. | Input, save/resume and WAV-header/event-gate checks. Tiny onboarding tips and completion/results use original RC1 visual evidence. Exact RTM wording/defaults/placement and audible mixing are not certified. |
| F08 — movement and finishes | Classic FreeCell transfers can travel through spare columns as well as free cells; Quick play skips the sequence. Spider has continuing fireworks. Vista Solitaire has falling/shattering cards and a score/bonus results dialog. Vista FreeCell has win music and results. | Motion snapshots, finite/completable effects, dismissal and state-immutability checks. The Vista captures are RC1 build 5600 and contain sped-up gameplay. Effects are reconstructed; exact cadence, RTM behavior and FreeCell's original win choreography remain unverified. |
| F09 — Help and window behavior | Corrects Vista-only instructions, restores 3.x Help > Index and inactive F1, adds system Move/Size and real owned dialog forms that can leave the parent's bounds. | Offscreen owner/location/click tests for all eight eras at 100/125/150/200%. Live activation, task switching, desktop composition and multiple monitors were not exercised. The Help viewer and window chrome remain custom renderers. |

Additional corrections remove the later games' arbitrary 200-state Undo ceiling, restore classic FreeCell's negative deals -1/-2, retain a recorded time bonus for consistent results/footer display, and prevent mnemonic labels from clipping inside otherwise wide buttons. The build now replaces the normal `dist` EXE from the retained release and verifies equal hashes, preserving the previous executable first.

## Per-profile coverage

Every row received production board/menu/dialog rendering, input and shared-session coverage. The common corrections are applied by game and era policy, rather than inventing visual or mechanical differences between adjacent releases.

| Windows | Game | Corrections exercised | Remaining release-specific comparison |
|---|---|---|---|
| 3.0 | Solitaire | F01, F02, F05, F06, F09; embedded System font | Full original game/dialog/runtime baseline |
| 3.1 / 3.11 | Solitaire | F01, F02, F05, F06, F09; measured Options and System font | Independent 3.1 versus 3.11 baseline |
| 95 | Solitaire | F01, F02, F05, F06, F09; measured Options and MS Sans Serif | Original runtime transitions |
| 95 | FreeCell | F02–F05, F08, F09; negative deals | Exact 95 resources and timings |
| 98 | Solitaire | F01, F02, F05, F06, F09; embedded MS Sans Serif | Exact 98 controls and transitions |
| 98 | FreeCell | F02–F05, F08, F09; negative deals | Exact 98 resources and timings |
| Me | Solitaire | F01, F02, F05, F06, F09; embedded MS Sans Serif | Exact Me controls and transitions |
| Me | FreeCell | F02–F05, F08, F09; negative deals | Exact Me resources and timings |
| Me | Spider | F02, F05, F08, F09; full history between original Undo barriers | Me resource identity; current preserved assets are XP |
| 2000 | Solitaire | F01, F02, F05, F06, F09 | Historical Tahoma bytes and original timing |
| 2000 | FreeCell | F02–F05, F08, F09; negative deals | Exact 2000 resources/font/timing |
| XP | Solitaire | F01, F02, F05, F06, F09; sampled Luna palette retained | Full frame states and historical font identity |
| XP | FreeCell | F02–F05, F08, F09; original loss/negative-deal resources | Exact transfer and warning cadence |
| XP | Spider | F02, F05, F08, F09; full history between original Undo barriers | Precise firework/audio/scoring corner cases |
| Vista | Solitaire | F01, F02, F05, F07–F09; full Undo, win score breakdown | RTM motion/scoring/glass and complete original dialogs |
| Vista | FreeCell | F02, F05, F07–F09; full Undo and win sound | Original win choreography, negative deals, RTM flow |
| Vista | Spider | F02, F05, F07–F09; full Undo and fireworks | RTM audio/effect/scoring and native file format |

## Evidence used

- Microsoft archived Solitaire [playing keys](https://documentation.help/Solitaire/sol9k6x.htm), [scoring rules](https://documentation.help/Solitaire/sol28bz.htm), and [3.x Help behavior](https://jeffpar.github.io/kbarchive/kb/058/Q58384/).
- Michael Keller's firsthand [FreeCell FAQ](https://www.solitairelaboratory.com/fcfaq.html) and [tutorial](https://www.solitairelaboratory.com/tutorial.html): inspection, one/no-move feedback, automatic home moves and intermediate transfers.
- Preserved XP resource menus/dialogs/strings, and the explicit -1/-2 data construction branches. No original game binary was executed. Locations and hashes: [asset provenance](ASSET-SOURCES.md) and [resource manifest](FIDELITY-RESOURCE-HASHES-0.9.0.json).
- Original-size Windows 3.1/95 Options screenshots retained under `references/WindowsUI`, with measurements documented in the audit.
- Long Zheng's [4 September 2006 Vista screencasts](https://istartedsomething.com/20060904/windows-vista-screencast-bundled-games/), recovered from the Internet Archive: RC1 Solitaire shattering cards/results/tips and Spider fireworks. They are reference recordings, not distributed assets.
- Preserved [Vista/7 sound resource archive](https://sounds.spriters-resource.com/pc_computer/solitairewindowsvista7/asset/442489/). Twenty WAVs are embedded; event mapping is independently implemented.

## Deliberate requirements and limits

The visible Settings button/F6, shared compatible settings, independent suspended sessions and isolated double-click behavior are retained user requirements. Ordinary FreeCell moves retain safe automatic home placement. Double-click does not sweep unrelated cards home.

Original Windows is not emulated. The program uses custom frame/control painting and modern Windows window management. Vista's glass does not blur the desktop. Windows 2000/XP/Vista scalable fonts come from the host; the frozen earlier bitmap strikes came from the local legacy font files, not independently acquired original OS images.

Unknowns remain for exact original focus/timer policy, right-click collection order, some scoring corner cases, animation curves, audio overlap, small-icon variants and complete per-release accelerator/dialog coverage. Vista negative-number Easter eggs and native Spider save-file compatibility are unsupported. These items require identified original release baselines; they are not passed by the local tests and are not described as exact reproductions.

The updated source and package validation, counts, render checks, hash and execution limits are recorded in [VERIFICATION.md](VERIFICATION.md). Previous executables and the original audit remain available.
