# Solitude historical fidelity audit — 9 September 2026

**Verdict: the current program does not yet meet the requested one-to-one historical experience. None of the 17 profiles can be signed off as an exact reproduction.** The core card games are substantially implemented, and several genuine era differences exist. However, this audit reproduced scoring, persistence and keyboard defects and confirmed missing original FreeCell features. Vista remains the least complete recreation of the original application flow.

This is an audit of **0.8.1**, covering the eight existing Windows presets, all 17 enabled game profiles, their source, current executable renders, regression checks and available historical evidence. Windows 7–11 remain deferred. No production source or game executable was changed during this audit.

## Scope and evidence

“Exact” must be judged against a specified original release, language, theme, font configuration, display scale and application state. The current project generally targets English default-era appearances, with XP Blue Luna and Vista Aero. Theme differences in reference screenshots are not automatically defects.

Evidence labels used below:

- **Reproduced:** an isolated probe demonstrates the behavior in current Solitude.
- **Confirmed from source/resource/visual comparison:** the implementation or displayed difference is directly observable, without claiming a live original-OS test.
- **Historical inference:** contemporary firsthand accounts or related-release evidence support the expectation, but the exact target binary was not tested.
- **Unverified:** sufficient original evidence or live comparison is absent. This is not a pass and is not automatically a defect.

The preserved XP binaries were inspected as resource data only. No original Windows game binary was executed. No visible game window was launched and no desktop input was sent. Probe forms were ephemeral; packaged rendering used a separate audit data directory. Personal saves were not used as fixtures.

The following are deliberate user requirements, not historical-fidelity bugs: the visible Settings button/F6 selector, sharing compatible preferences, retaining separate preset sessions, and making double-click affect only the selected card. Reinstating an unwanted double-click cascade would violate the requested behavior.

## Findings that need correction

### F01 — P1: repeated foundation moves manufacture Solitaire points

**Reproduced in all eight Klondike presets, in both Standard and Vegas scoring.** In Solitude deal seed 4, move the exposed Ace to an empty foundation, then move it repeatedly between that foundation and another empty foundation. Every transfer is accepted and rewarded again. After 100 transfers, Standard gains **1,000 points** and Vegas gains **500 dollars**, although the foundations still contain just one card. Classic Standard went from 10 to 1,010; Vegas went from −47 to +453. Vista Standard went from 15 to 1,015 because its initial move also exposed a card.

`CanMove` accepts the transfer and `Move` awards the foundation score based on the destination before considering the origin: [Game.cs](../src/Game.cs#L178), [score branch](../src/Game.cs#L196). The probe uses a normally generated, playable deal, not an invented card arrangement. The ordinary drag handler uses these same move rules.

This contradicts the bounded scoring model described in Microsoft's [Windows 3.x scoring article](https://www.betaarchive.com/wiki/index.php/Microsoft_KB_Archive/101766) and undermines Vegas accounting. Exact handling of a foundation-to-foundation gesture still needs checking against each original; that uncertainty does not make the observed score growth acceptable evidence of fidelity. Correct the accounting or reject that gesture according to the verified original behavior, and add a regression for repeated transfers.

### F02 — P1: switching presets can discard an unfinished game

**Reproduced.** In Vista Solitaire, disable “Always save game on exit,” draw cards, switch to XP, then return to Vista. The current deal and its history are discarded even though the application never exits. The control case with saving enabled retained the exact state; the disabled case returned a new random seed and zero moves.

[StashSession](../src/GameWindow.Variants.cs#L11) uses `SaveOnExit` to decide whether to retain the in-memory session. Separate temporary suspension from persistence on application exit. This is a defect in Solitude's requested multi-preset behavior, not a feature the original standalone applications needed.

### F03 — P2: classic FreeCell does not detect and present a lost game

**Reproduced in Windows 95, 98, Me, 2000 and XP presets.** A structurally valid 52-card state with every free cell occupied, no empty columns and zero legal moves remains on the board without a loss dialog. `Hint()` is null and no loss is recorded at that point. The fixture passes `Game.Validate`; its reachability from an original Microsoft-numbered deal was not established. The source independently confirms that the successful-action path handles wins but has no corresponding loss branch.

XP's preserved dialog resources contain the original **Game Over** loss dialog and its replay choices: [resource evidence](../references/Mechanics/freecell-xp-controls.json#L446). Solitude's [Changed handler](../src/GameWindow.cs#L151) only checks for a win. The original classic game's one-available-move caption warning is also absent; that behavior is described in [Keller's firsthand investigation](https://www.solitairelaboratory.com/fcfaq.html). Its exact cadence and every pre-XP revision remain unverified.

Add immediate legal-move exhaustion handling, the correct replay choices and single-count statistics updates. Ordinary resignation currently records a loss later; that does not reproduce the original end-of-game feedback.

### F04 — P2: two recognizable classic FreeCell board features are missing

**Right-click inspection is absent in all five classic presets.** Holding the right button over a buried card produced identical before/held images in every probe. The [right-button branch](../src/GameWindow.cs#L208) returns without doing anything for classic FreeCell. The original momentary full-card reveal is documented by [Keller](https://www.solitairelaboratory.com/fcfaq.html). This matters particularly in tightly packed columns.

**The Cards Left counter is absent.** The original XP resource string survives at [line 758](../references/Mechanics/freecell-xp-controls.json#L758). The current [menu/board painter](../src/GameWindow.Paint.cs#L42) has no corresponding counter, and it is absent from all five rendered boards. Keep the requested Settings access while restoring the original information. Exact pre-XP counter placement should be checked against each release rather than copied blindly from XP.

### F05 — P2: original keyboard operations are missing

**Reproduced:** Space does nothing at the initial stock keyboard position in all seven pre-Vista Solitaire presets. The same dispatcher also prevents Space from acting as a general card-selection/placement key in these presets. Microsoft's archived [playing instructions](https://documentation.help/Solitaire/sol9k6x.htm) explicitly allow Enter or Space for these operations. See [ProcessCmdKey](../src/GameWindow.cs#L398).

**Reproduced:** Alt+Q does not toggle Quick Play in any classic FreeCell Options dialog. XP's original dialog marks Q as its access key: [original control](../references/Mechanics/freecell-xp-controls.json#L687). Solitude's [dialog key handler](../src/GameWindow.cs#L350) consumes unmatched keys. This is a shared dialog limitation: access-key dispatch and radio-group arrow navigation are not implemented. Other original access keys need explicit per-dialog coverage; this audit directly probed Alt+Q, not every key.

### F06 — P2: the classic Solitaire Options dialogs still have the wrong geometry

**Confirmed visually against the existing original captures.** At 100%, the Windows 3.1 reconstruction is 310×270 pixels, compared with an approximately 278×230-pixel original dialog crop. In the original, Keep score sits to the right of Status bar; in Solitude it is moved onto a new row below Outline dragging. Original access-key underlines are missing, and group/button spacing differs. The original Windows 95 reference dialog is approximately 342×216; Solitude uses 350×252, with excessive empty space above the buttons.

These are visible structural differences, not a judgment based on two differently colored Windows themes. See the [native-size comparison image](../artifacts/audit-2026-09-09/options-reference-comparison.png), [3.1 original capture](https://cdn.mobygames.com/29baa550-ac03-11ed-b57a-02420a000130.webp), and [95 original capture](https://cdn.mobygames.com/b4c2873a-c1d4-11ed-ab6b-02420a000194.webp). Crop dimensions are approximate screenshot measurements, not recovered dialog-unit specifications.

The source fixes these sizes and positions in [PaintDialog](../src/GameWindow.Dialogs.cs#L16) and [PaintClassicOptions](../src/GameWindow.Dialogs.cs#L163). The 95 layout is reused through XP; those releases need separate resource/layout comparisons before certification. Their similarity alone is not evidence that they should differ.

### F07 — P2: Vista's options, sound and saved-game flow are incomplete

**Confirmed implementation gap; historical confidence varies by detail.** Vista Options contains game-specific draw/scoring/difficulty controls plus only two general checkboxes: animation and save on exit. There is no sound option, separate continue-saved-game option, or Vista save-on-close question. `PeriodClosing` implements the close question only for classic Spider. All Vista sound calls return immediately because `PlayPeriodSound` only supports classic Spider.

A [contemporary Vista user report from February 2008](https://www.techtalkz.com/threads/even-solitaire.421706/) explicitly describes Solitaire's animation and sound options and its saved-game startup question. Another [Vista-era report](https://www.vistax64.com/threads/solitaire-save-issues.228741/) describes separate save/load choices and close/startup prompts. These are firsthand reports, not resource-level specifications; precise wording, defaults, control placement and differences among the three Vista games still need original captures/resources. Do not treat Windows 7 screenshots as proof of Vista behavior.

Affected source: [Vista options](../src/GameWindow.PeriodDialogs.cs#L95), [sound gate](../src/GameWindow.Period.cs#L26), [closing flow](../src/GameWindow.Period.cs#L36). The Vista game menu also has no Restart entry. Its exact original availability and route remain a comparison item, not a certified per-game mismatch in this audit.

### F08 — P2: animation and completion behavior are reconstructed or absent

The source has distinct motion policies, but not a verified reproduction of every original game. Classic Solitaire moves immediately and has a bouncing-card finish. Classic FreeCell simulates some transfers through free cells; sequences needing spare columns skip that reconstruction when the available cells are insufficient. The [original tutorial](https://www.solitairelaboratory.com/tutorial.html) demonstrates the characteristic intermediate transfers; the current shortcut is visible in [BeginFreeCellSequence](../src/GameWindow.Motion.cs#L90).

For Vista and both Spider presets, [StartVictory](../src/GameWindow.cs#L455) goes directly to a generic win dialog after card motion. There is no separately implemented Vista/Spider celebration sequence to compare with the originals. Classic Spider loads preserved sounds, but only generic success and win events are mapped; event-specific audio equivalence has not been established. Vista uses a common 210 ms easing policy. Original movement speeds, flip curves, hint sequencing, celebration effects and audio timing remain unverified.

The recommendation is to capture original transition sequences and implement the missing ones. A fast frame loop cannot establish that their choreography is historically correct.

### F09 — P2/P3: Help and window behavior are approximations

All presets use a small custom Help viewer and largely shared text. Vista Solitaire's help even mentions Deck, a timer option, a status-bar option and manually turning an exposed face-down card, although its menu/options and automatic flipping differ. See [shared help content](../src/GameWindow.PeriodDialogs.cs#L20). This is an internally observable documentation mismatch as well as a historical one.

Windows 3.0/3.1 also use the later Contents/F1 convention. Microsoft's [Q58384](https://jeffpar.github.io/kbarchive/kb/058/Q58384/) documents their Help > Index route and the original nonfunctional F1 shortcut. Preserving that harmless quirk is a low-priority fidelity decision; reproducing historical crashes is not a requirement.

Dialogs are painted within the parent form, and the system menu lacks the normal Move and Size commands. The source uses a custom borderless window rather than the historical window manager. Native popup behavior, dialog movement outside the parent, task switching and every maximized/non-client state have not been demonstrated as equivalent.

## Visual and evidence limitations

**Fonts still depend on the host.** Windows 3.x uses the installed System raster font. Windows 95/98/Me use an installed raster font at integral scaling, but switch to scalable Microsoft Sans Serif at 125% and 150%. Windows 2000/XP use the current host's Tahoma; Vista uses its Segoe UI. The exact historic fonts are not embedded. Consequently, a self-contained executable is not yet a self-contained typography reproduction. This explains a potential source of changing weight/spacing, but does not establish that every current string is unreadable. See [font selection](../src/Skin.cs#L48) and [raster loading](../src/RasterFont.cs#L18).

**XP's latest palette correction is real but narrower than a full match.** The latest caption rows and button color samples have reference-based checks. The button surfaces, hover/pressed states and glyph treatment remain reconstructions. Selected matching pixels do not certify every title-bar pixel. Vista's frame has preserved image parts, but its glass does not blur the real desktop. Older frame references frequently come from Calculator or system dialogs, which help establish shell appearance but cannot validate Solitaire's own layout. These limitations are visible in the renderer and documented in [XP color evidence](../docs/XP-COLOUR.md) and [window references](../docs/WINDOW-REFERENCES.md).

**Historical resources are uneven.** XP has preserved menu, accelerator, dialog, string, icon and some sound data. Earlier game builds have much less direct evidence. Me Spider reuses XP assets without a Me binary comparison. Vista art comes through preservation sources covering Vista/7; every asset's Vista-specific identity, original defaults and small-icon rendering have not been established. Asset reuse is an evidence gap, not proof that the original releases differed. See [provenance](../docs/ASSET-SOURCES.md).

**Scoring and timing certification is incomplete.** Original focus-loss pausing, exact right-click collection order, Spider stock-deal score cost, uncommon classic FreeCell multi-column limits, Vista scoring details and the 200-state undo cap remain unresolved original-comparison items. Do not describe a bounded undo history as unlimited. Klondike/Spider use Solitude's random-deal generator; only FreeCell's numbered layouts have an independently checked Microsoft mapping. Negative-number FreeCell Easter eggs are not implemented.

## Per-profile result

Every profile received current production-render coverage and existing UI/rule coverage. “Partial” means important behavior is implemented but known gaps or missing original evidence prevent sign-off. “Incomplete” highlights Vista's broader missing application flow; it is not a numerical accuracy score.

| Windows | Game | What is materially supported | Result and outstanding items |
|---|---|---|---|
| 3.0 | Solitaire | Early frame/fonts/backs, manual turning, classic scoring family, one Undo | **Partial:** F01, F05, F09; direct original game/dialog/runtime baseline incomplete |
| 3.1 / 3.11 | Solitaire | Classic rules, solid caption, original Options screenshot | **Partial:** F01, F05, directly mismatched Options F06, Help F09; 3.1/3.11 not independently compared |
| 95 | Solitaire | Classic rules, silver chrome, original Options screenshot | **Partial:** F01, F05, directly mismatched Options F06; original runtime not compared |
| 95 | FreeCell | 32,000 deal range, classic selection/home rules and compact controls | **Partial:** F03/F04/F05; original 95 resource/timing baseline incomplete |
| 98 | Solitaire | Classic rules and gradient chrome | **Partial:** F01/F05, shared dialog reconstruction; original gameplay identity not established |
| 98 | FreeCell | Classic FreeCell behavior and 32,000 deal range | **Partial:** F03/F04/F05; release-specific resource evidence incomplete |
| Me | Solitaire | Classic rules, early backs, warm-gray frame | **Partial:** F01/F05; release-specific controls/animation evidence incomplete |
| Me | FreeCell | Classic FreeCell rules and deal range | **Partial:** F03/F04/F05; Me-specific resource evidence incomplete |
| Me | Spider | 104 cards, suit difficulties, classic undo barriers, Spider art | **Partial:** F08/F09; XP resources reused, Me equivalence unverified |
| 2000 | Solitaire | Classic rules, warm-gray chrome and Tahoma | **Partial:** F01/F05; resource/layout/font identity unverified |
| 2000 | FreeCell | Classic FreeCell behavior and 32,000 deal range | **Partial:** F03/F04/F05; 2000-specific resource/font baseline incomplete |
| XP | Solitaire | XP card backs, Luna references and preserved menu/dialog resources | **Partial:** F01/F05/F06/F09; stronger evidence than older presets, still not exact |
| XP | FreeCell | Million-deal range, original controls/strings and deal mapping | **Partial:** F03/F04/F05 directly supported by XP evidence; animation coverage incomplete |
| XP | Spider | Original card/felt/About/sound assets and menu evidence | **Partial:** F08/F09; scoring corner cases, audio mapping and finish behavior need comparison |
| Vista | Solitaire | Proportional cards/felt, automatic turning, multi-step undo, Aero parts | **Incomplete:** F01/F02/F07/F08/F09; exact options/scoring/frame behavior unverified |
| Vista | FreeCell | Numbered deals, recursive sequence capacity, alternate artwork | **Incomplete:** F07/F08; original option/menu/win flow, undo limit and audio unverified |
| Vista | Spider | Scalable artwork; undo across deals/completions | **Incomplete:** F07/F08; original option/win flow, audio and scoring details unverified |

Shared keyboard/dialog, Help, font and window-manager limitations also apply where not repeated in each row. F02's underlying suspension logic is shared; its direct reproduction was Vista Solitaire.

## What passed, and what that establishes

- **50 rule verification groups passed**, including long rule-driven action sequences, card conservation, invalid-save handling and persistence behavior already covered by the suite. [Log](../artifacts/audit-2026-09-09/rules.log).
- **1,725 existing UI checks passed**, including all 17 profiles, double-click event sequences, Settings access, compatible-preference sharing, motion endpoints and partial repaint checks. These tests did not cover the newly reproduced failures above. Their “original menus/options” label covers selected expectations, not complete original-program equivalence. [Log](../artifacts/audit-2026-09-09/ui.log).
- **33,001 complete FreeCell layouts matched** an independent array translation of [Jim Horne's published shuffle](https://www.solitairelaboratory.com/mshuffle.txt): all 1–32,000, another 1,000 spaced seeds above 32,000, and 1,000,000. This establishes layout mapping for those seeds; it does not test all million deals or their solvability.
- **41 diagnostic observations** were recorded: 15 classic FreeCell input/loss probes, two preset-suspension cases, the deal comparison, seven Space-key probes and 16 scoring-loop cases. These intentionally record observed defects rather than assert the defective behavior as desired. [Harness](../artifacts/audit-2026-09-09/Audit.cs), [results](../artifacts/audit-2026-09-09/diagnostics.json), [blocked-state fixture](../artifacts/audit-2026-09-09/freecell-blocked-state.json).
- The **0.8.1 single executable freshly produced 225 application views and 36 motion frames**, covering all profiles at 100/125/150/200%. All 17 board overviews and all 17 Options crops were visually inspected, plus representative original game/dialog images. The 261 images were generated, not individually certified against 261 original screenshots. [Render completion](../artifacts/audit-2026-09-09/package-renders/complete.txt).
- Core classic Solitaire behavior aligns in several respects with the [archived scoring help](https://documentation.help/Solitaire/sol28bz.htm): standard move/recycle scoring, Vegas ante and keep-score support. This positive coverage does not erase F01. FreeCell capacity differs between classic and Vista; it is not just a different skin. Classic Spider's deal/completion undo barriers agree with the relevant section of a [firsthand 2009 investigation](https://gamefaqs.gamespot.com/pc/566283-spider-solitaire/faqs/57700). Exact release-specific edge cases remain as listed above.

Session statistics are keyed by Windows preset and game in the current source. The suspected cross-game session-statistics leak was checked and **not found**; it is not a finding. The requested isolated double-click policy and shared compatible settings remain covered by passing checks.

## Performance and packaging

The fresh offscreen renderer benchmark measured these warm median/p95 CPU costs at 150%:

| Profile | Median ms | p95 ms |
|---|---:|---:|
| XP Solitaire | 1.63 | 3.15 |
| XP FreeCell | 1.26 | 2.11 |
| XP Spider | 2.16 | 2.93 |
| Vista Solitaire | 3.97 | 4.67 |
| Vista FreeCell | 4.13 | 5.12 |
| Vista Spider | 4.82 | 6.19 |

The offscreen production-clock/message-queue probe delivered approximately 60, 120 and 144 callbacks per second at those targets; p95 intervals were about 17.1, 8.7 and 7.4 ms. **These are not measured monitor FPS, input latency, historical animation fidelity or a live play-session smoothness result.** Audio output, compositor behavior, multi-monitor DPI transitions and prolonged interactive play were not tested. [Benchmark data](../artifacts/audit-2026-09-09/benchmark.json). An NU1900 warning means the online NuGet vulnerability feed was unavailable; dependency security was not certified by these runs.

There are two different delivered locations:

| Path | Verified version | Size | SHA-256 |
|---|---|---:|---|
| `artifacts/release-v081/Solitude.exe` | 0.8.1.0 | 59,131,235 bytes | `5BA71ABDF97EE53FA5E3921357827177B6C051500E120C03BE17253B3EB0BBB6` |
| `dist/Solitude.exe` | **0.8.0.0** | 59,127,139 bytes | `9ACFA8D254E883F2431BFE2CA91AB515EE8A0EBA7860C0B8533C377F99FEB6CC` |

Thus launching the usual `dist` copy still misses the 0.8.1 XP color correction. This is a delivery inconsistency, separate from the fidelity problems in the latest build. Neither executable was replaced during the audit. Single-file publishing is configured and packaged rendering works here; native-library extraction and external user settings are still used. A clean-machine portability test was not performed.

## Visual evidence and next acceptance work

The generated overviews are navigation aids. They are scaled for comparison and should not be used to judge individual font pixels; use the underlying full-size renders for that.

- [All eight Solitaire boards](../artifacts/audit-2026-09-09/klondike-overview.png)
- [All six FreeCell boards](../artifacts/audit-2026-09-09/freecell-overview.png)
- [All three Spider boards](../artifacts/audit-2026-09-09/spider-overview.png)
- [Solitaire Options](../artifacts/audit-2026-09-09/klondike-options-overview.png), [FreeCell Options](../artifacts/audit-2026-09-09/freecell-options-overview.png), [Spider Options](../artifacts/audit-2026-09-09/spider-options-overview.png)
- [Original-versus-current Options comparison](../artifacts/audit-2026-09-09/options-reference-comparison.png)

The correction order should be: scoring and session preservation; FreeCell loss/inspection/counter and original keyboard controls; measured dialog layouts; then Vista's complete options, sound, save/restart and finish flow. Preserve the user-requested selector and double-click policy throughout.

For historical sign-off, build a separate baseline for each original release: identify its executable/resource revision, capture normal/inactive/maximized windows and every original dialog, record key mouse/keyboard actions, and record score/timer/undo/save/win/loss transitions. Compare the same theme, scale and state. Existing tests should then be extended from those independent observations. Until that work is done, the honest description is **a partly faithful recreation with known defects**, not an exact time machine.
