# Program audit fixes — Solitude 0.11.8

This update addresses A01–A06 in the [0.11.7 program audit](PROGRAM-AUDIT-0.11.7.md). The historical editions remain Windows 3.0 through Vista, with the existing FreeCell/Spider availability and fictional ORBIT edition.

| Finding | Result | Regression evidence |
|---|---|---|
| A01: competing windows overwrite saves | Normal launches acquire one file lease per data folder; another launch activates the owner. Store writes also check the loaded file revision under a file lock and reject stale snapshots. Newer moves, records and preferences are preserved. | Separate-process lease acquisition/release, directory aliases, hidden-window activation, two independent stores, repeated stale writes, original writer continuing, and explicit reload. |
| A02: historical Options show another deal's rules | Options and Difficulty start from the active deal. Unchanged Apply preserves that deal and shared next-deal defaults. Explicit rule changes start a new deal with the chosen rules. Cancel preserves both. | Eight historical Klondike profiles and all three Spider profiles; actual control clicks, draws, switching away/back and difficulty changes. |
| A03: Vista hides settled foundation cards | Klondike and FreeCell paint the highest settled card under an incoming animation. ORBIT uses the same pile-selection helper. | Two onto Ace and Five onto Four, four scales, Vista Klondike/FreeCell and ORBIT, pixel comparisons before/during flight and Undo. |
| A04: large historical windows extend off-screen | Every edition fits its minimum layout to the monitor work area and centers its final size. The chosen display size is retained and can recover on a larger monitor. Cached table images include the effective drawing scale. | All 18 profiles at four preferred scales; 1366×728, 1920×1040 with negative screen origin, and 800×540 work areas; centering, fitted dialog clicks and return to 3840×2160. |
| A05: historical accessibility omits the board | All editions expose named controls, piles and visible cards with actions, selection and screen bounds. Numbered-deal fields can be edited. Help paragraphs and ORBIT results expose readable text. | Every edition's accessible tree, visible-card identity multiset, selection, FreeCell transfers, game number 42, Settings/Cancel, reverse Tab and Help text. Hidden card identities remain absent. |
| A06: shortcuts bypass victory state | While the celebration runs, gameplay/settings shortcuts are suppressed. Click, Escape, Enter or Space can proceed to results. Disabling motion ends an active victory. | F2/F5/F6/Ctrl+Z during victory, reduced motion during the effect, unchanged won state and all 52 foundation cards. |

## Performance and maintenance

- ORBIT caches the comparison keys of immutable undo snapshots. Repeated hints retain the same ordering and loop avoidance without rebuilding a string for every old board. Draw, Undo, restored sessions and the complete history remain supported.
- Hovering the corner logo damages only its corner when no other animation is active. It no longer requests a full-window repaint for that animation alone.
- The 7.6-second victory choreography is retained. The frame pump stops at the still final display after 6.3 seconds; the ordinary timer opens results when the duration ends. This avoids roughly 78 redundant frame requests at a 60 Hz target.
- Obsolete per-column scrolling fields, handlers and empty drawing methods are removed. Long columns still fit in full.
- Shared settled-pile selection and active-rule handling replace duplicated special cases.
- [The verification script](../tools/verify-program.ps1) runs rules, UI and native-message checks, publishes an isolated single EXE and compares source/package images. The [Windows workflow](../.github/workflows/verify.yml) uses pinned action revisions. Hosted results are available separately in [GitHub Actions](https://github.com/cazrl/Solitude/actions); validation below records the local gate.

No new rendering dependency is shipped. Two rendering experiments were discarded because they did not demonstrate a reliable gain. The active 52-card effect at large scales can still exceed a 60 FPS CPU paint budget; these changes do not establish physical monitor FPS.

## Validation and delivery

Final results: **53 engine groups, 25,979 UI checks, 384 native checks and 290 matching source/package PNGs**. Package identity and performance are recorded in [VERIFICATION.md](VERIFICATION.md). The repeatable regressions are in [ProgramFixChecks.cs](../tests/ProgramFixChecks.cs); [independent observations](audits/program-0.11.8/observations.json) and full local evidence under `artifacts/fixes-v0118/` record the diagnostic results. Tests use isolated data and hidden windows.

The delivered file is `dist/Solitude.exe`, with a retained identical copy under `artifacts/release-v0118/`. Older EXEs and the user's normal save are preserved. [All 18 profiles](images/program-fixes-v0118/all-profiles.png).

## Boundaries of this update

The six confirmed software defects are distinct from the audit's unverified comparisons and distribution recommendations. This update does not certify exact original-Windows behavior, diagnose the earlier 0.11.5 crash without its missing managed stack, establish historical asset redistribution rights, or supply a signing identity. The verified package is distributed through the [0.11.8 release](https://github.com/cazrl/Solitude/releases/tag/v0.11.8). Physical mixed-DPI monitor moves, Narrator use, audio latency, extended ordinary play and clean-machine launch still need live verification. Existing crash diagnostics and endgame regression coverage remain in place.
