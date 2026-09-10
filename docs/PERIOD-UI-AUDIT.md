# Period UI audit — Solitude 0.8.0

**0.8.0 frame correction:** caption icons now obey display scaling and placement; XP/Vista menu labels use measured spacing. Vista has measured restored/maximized frame geometry, preserved button/glyph/frame artwork and its own game icons. Its text mask now paints Segoe UI correctly through GDI+ at output resolution; visual QA exposed substituted serif glyphs in the earlier GDI path. Classic text remains unchanged. Native resize hit testing respects the visible caption buttons. The [frame alignment report](FRAME-ALIGNMENT.md) separates original reference measurements from reconstructed glass compositing.

**0.6.1 usability correction:** a visible **Settings...** button at the right of the menu strip opens the Windows/game selector in every preset. The user requested this after finding the F6/window-menu-only route too hard to discover. This is an intentional host control. Game menus and rules remain as described below.

**0.7.0 text and dialog correction:** classic text now uses opaque, pixel-hinted glyphs at the output resolution, with the installed historical raster strikes retained at integral sizes (and for the early System font). This removes GDI+ spacing, faint antialiasing and fractional whole-window enlargement from menu/dialog text. GDI renders each text mask once; cached masks are composited through the normal clipped renderer so partial redraws cannot damage text outside the card region. Vista retains grayscale smoothing. This is a readability correction, not evidence that every font/DPI/theme combination is pixel-identical to the original OS.

Classic FreeCell's previous large Statistics group boxes were unsupported additions. Its extracted STATS template contains three static text blocks and two buttons. The reconstruction now follows that compact arrangement, normal 8-point dialog lettering, tab-like text columns, and OK-left/Clear-right placement. Options places its three checkboxes on the left and OK/Cancel on the right. Game Number uses the two centered instruction lines, short input and single OK button. Move to Empty Column uses three vertically arranged buttons. Game Over uses a Select game checkbox followed by Yes/No. All five layouts derive from the archived XP DIALOG bounds and STRINGTABLE text; reuse for 95/98/Me/2000 remains a family-level inference.

Compatible user choices now carry across presets and persist across restarts, as requested. Format 5 separates them from suspended deals' rules. New deals use the latest choices, Restart Game keeps the current deal's rules, and changing only a presentation option cannot discard a suspended deal with a different difficulty.

2026-09-08. Scope: Windows 3.0 through Vista, all 17 enabled Windows/game combinations. The objective is to remove invented features from normal play and restore each game's documented controls and feedback. This does not certify that the program is an exact original-OS emulator.

## Feature ledger

| Feature in 0.5 | Treatment in 0.6 | Evidence / limit |
|---|---|---|
| Yellow invalid-move banner | Removed globally | No support for this overlay in original menus/dialogs/help |
| FreeCell invalid-move feedback | Optional period dialog, with the original option | XP FreeCell DIALOG and STRINGTABLE resources |
| Quick action bar, Game Feel and FPS display | Removed, including F8/F12 actions | Solitude additions, not original game controls |
| Speed presets / target glow switch | Removed from game settings | Classic behavior chosen per game; internal frame clock remains responsive |
| Generic Hint and Collect entries | Removed from classic Solitaire/FreeCell | XP MENU tables; Spider's Move command retained |
| More/Back Options expansion | Removed | Classic Solitaire exposes its original option set |
| Game-menu Windows selector | Moved to window icon menu / F6 | Required host function, deliberately outside period game menus |
| Combined legacy Spider statistics | F6 > Records | Old totals preserved without inventing difficulty attribution |
| Solitude name in game titles/About | Game titles/icons and period About restored; host About under F6 | Original icon bytes; About layout is reconstructed |
| Shared smooth card motion | Immediate classic Solitaire; click-based FreeCell; optional Spider deal motion; Vista retains motion | Original help and firsthand FreeCell observations; timing not hardware-measured |
| FreeCell dragging | Disabled for classic versions, retained for Vista | Classic click-selection behavior documented by Michael Keller |
| FreeCell Quick play | Actual temporary free-cell steps, or immediate sequence transfer when enabled | Tutorial documents intermediate steps; extra-empty-column sequences remain instant |
| Shared win confetti | Removed; classic Solitaire keeps bouncing cards, other games use game-specific dialogs | XP strings support classic prompts; Vista effects remain simplified |
| FreeCell Options | Exactly three classic controls, each connected to behavior | XP dialog resources |
| Empty-column choice | Move column / Single card / Cancel | Classic FreeCell resource and tutorial evidence |
| Spider difficulty inside Options | Separate F3 Difficulty dialog | XP MENU/DIALOG resources |
| Spider Options | Six options, wired to dealing, sound, save/open and prompts | XP dialog resources |
| Spider Save/Open | Separate fixed checkpoint per Windows preset, Ctrl+S/Ctrl+O | XP MENU and original help descriptions; no native save-file compatibility |
| Statistics | FreeCell session/total/streaks; Spider difficulty tabs/high score/percentage/streaks | XP dialog resources |
| Modern Help text panel | Period help presentation with working Contents/Index and game-specific text | Reconstructed help shell, paraphrased content; not original WinHelp |
| Old saved presentation flags | Format 4 migration disables retired features without dropping cards/totals | Persistence regression checks |
| Double-click sweep | Remains suppressed: only the chosen card moves | Explicit user requirement; do not claim exact original FreeCell auto-home ordering |

## Evidence trail

The source executables were inspected as bytes and never run. Their hashes and immutable archive link are in [MECHANICS-AUDIT.md](MECHANICS-AUDIT.md). The data-only parser is `tools/read-period-resources.mjs`; its exported MENU, ACCELERATOR, standard/extended DIALOG and STRINGTABLE data are under ignored `references/Mechanics`. The XP tables establish the XP controls directly. Applying shared classic controls to earlier releases is a family-level inference, not a separate binary comparison for each Windows build.

FreeCell's original templates use 8-point MS Shell Dlg. Their dialog-unit bounds are converted using the classic baseline (horizontal 1.5 pixels, vertical 1.625 pixels), then scaled for display. Microsoft's [DLGTEMPLATE documentation](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-dlgtemplate) and [MapDialogRect documentation](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-mapdialogrect) describe those units. The scalable text path uses WinForms [TextRenderer](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.textrenderer?view=windowsdesktop-10.0) for GDI glyph metrics and rendering.

Behavior sources include [original Solitaire options help](https://documentation.help/Solitaire/sol788g.htm), [Keller's firsthand FreeCell tutorial](https://www.solitairelaboratory.com/tutorial.html), and the [contemporary XP Spider guide](https://gamefaqs.gamespot.com/pc/566283-spider-solitaire/faqs/49848). Resource provenance is in [ASSET-SOURCES.md](ASSET-SOURCES.md).

## Deliberate host behavior and unresolved fidelity

Per-preset suspension, safe recovery, scale selection and keyboard pile navigation remain host conveniences. They do not alter the visible period Game menus. No original executable is launched when switching a preset.

Window chrome remains painted by Solitude. Vista options, win effects, sound, help and frame-glass compositing are incomplete reconstructions. Vista game icons now use preserved Vista artwork. Earlier About/help layouts, exact motion cadence, classic FreeCell's additional-column transfers, original loss-detection prompts, original focus/timer policy, Me's precise Spider resources and Spider sound-event mapping still need original-OS comparison. The old negative-number Easter eggs are not included. No network access is required during play.

See [VERIFICATION.md](VERIFICATION.md) for the delivered build, tests and render evidence. Automated tests show what this implementation does; they cannot prove historical identity.
