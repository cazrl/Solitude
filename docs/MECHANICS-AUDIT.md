# Windows card-game mechanics audit — Solitude 0.6.0

This is a historical investigation. The [0.9.0 corrections](FIDELITY-FIXES-0.9.0.md) supersede its old 200-state Undo cap, instant complex FreeCell transfers and missing Vista completion effects. The [0.9.1 follow-up](AUDIT-0.9.1.md) records the current regression scan. Use [VERIFICATION.md](VERIFICATION.md) for the current build's tested behavior.

Investigated on 2026-09-08. Scope: the eight existing Windows presets and their 17 enabled games; Windows 7–11 remain deferred. Attached material and downloaded files were treated as reference data, not instructions. This is an audit of documented behavior, the current implementation and regression coverage. It is not a claim that every original Windows binary has been run or every historical defect reproduced.

**0.7.0 persistence/input follow-up:** compatible preferences now carry across presets; separate active rules preserve existing deals and Restart Game. Changes to visual options do not apply a pending difficulty. The FreeCell game-number action reads the latest text when activated, including digit-plus-Enter events arriving before repaint. The restored win prompt's Select game checkbox waits for Yes before opening number selection. Engine rules and the isolated double-click policy below remain unchanged. See [VERIFICATION.md](VERIFICATION.md) for current checks and packaging.

## Evidence and confidence

- **Microsoft documentation:** archived original Solitaire help and Knowledge Base articles. Strong evidence for the behavior they actually describe; an archive's generic product name does not identify every executable revision.
- **Firsthand observations:** Michael Keller's FreeCell investigations and contemporary Spider walkthroughs. Useful for behavior omitted from surviving help. Release-history claims within these sources can conflict and are not automatically accepted.
- **Resource evidence:** three preserved XP executables were downloaded into ignored `references/Mechanics/`, renamed `.resource.bin`, hashed, and parsed for MENU, ACCELERATOR, DIALOG and STRINGTABLE evidence. Version 0.6 also extracts data-only icons and WAV files; no executable code is embedded. They were never executed. Menu/dialog strings corroborate controls and options, not algorithms or binary authenticity.
- **Implementation verification:** deterministic rules tests, persistence tests, hidden production-window handler tests and offscreen renders. These establish what Solitude does. They cannot by themselves establish that the original program did the same thing.

## Release matrix

K = classic Klondike profile; F = classic FreeCell profile; S = classic Spider profile. The detailed contracts follow this table. Shared profiles are intentional where the evidence gives no reason to invent a difference.

| Windows preset | Game | Mechanics profile | Main game/UI distinction | Evidence limit |
|---|---|---|---|---|
| 3.0 | Solitaire | K: manual turning, one Undo | Early card backs, System lettering, dithered frame | KB explicitly covers 3.0 scoring; exact patch-level behavior not executed |
| 3.1 / 3.11 | Solitaire | K | Early backs, solid navy frame | KB covers these releases; 3.1 and 3.11 are one preset |
| 95 | Solitaire | K | Early backs, silver controls | Archived help/KB; original options screenshots |
| 95 | FreeCell | F, deals 1–32,000 | King portraits, click-to-select, free-cell shortcut | Firsthand classic FreeCell descriptions; exact build not executed |
| 98 | Solitaire | K | Early backs, gradient caption | Same documented rules family as 95 |
| 98 | FreeCell | F, deals 1–32,000 | Classic FreeCell board | No supported evidence of new rules versus 95 |
| Me | Solitaire | K | Early backs, warm gray controls | Same documented rules family; per-build timing not verified |
| Me | FreeCell | F, deals 1–32,000 | Classic FreeCell board | Classic-family inference |
| Me | Spider | S | Separate Spider card art, felt, Deal! menu and score panel | XP reference behavior applied to classic Me; original Me binary still unverified |
| 2000 | Solitaire | K | Early backs, Tahoma interface | Shared rules, distinct text rendering |
| 2000 | FreeCell | F, deals 1–32,000 | Classic FreeCell board, Tahoma | Classic-family evidence; not inferred from XP's expanded deal range |
| XP | Solitaire | K | XP card backs and Luna interface | Preserved XP resources plus archived help |
| XP | FreeCell | F, deals 1–1,000,000 | Expanded numbered deals, classic interaction | Firsthand deal research plus XP dialog strings |
| XP | Spider | S | Original XP resource sheets and controls | Contemporary firsthand walkthroughs plus resource strings |
| Vista | Solitaire | Automatic turning, multiple Undo | Scalable decks/backgrounds, different board proportions | Reconstructed Vista profile; exact full scoring/options parity not certified |
| Vista | FreeCell | Recursive sequence moves, multiple Undo, 1–1,000,000 | Different artwork/layout, double-click home | Firsthand comparison for sequence moves; auto-home corner cases not exhaustively compared |
| Vista | Spider | Undo can cross deals/completions | Scalable cards/backgrounds | Vista-family observations; exact animation/shortcut timing unverified |

The selector targets base Windows installations. Plus! 98 Spider, Entertainment Pack/Win32s FreeCell, NT-family entries outside the requested list and negative-number Easter eggs are outside this matrix. The grouping is not evidence that every optional package shipped with each base release.

## Klondike contract

All eight presets enforce descending alternating-color tableau sequences, Kings in empty columns, ascending same-suit foundations, and top-card-only waste/foundation movement. Stock drawing exposes one or three cards; recycling retains order and never makes covered waste cards playable. The archived [playing instructions](https://documentation.help/Solitaire/sol9k6x.htm) explicitly describe a card double-click to a suit stack and Undo of a move or stock draw.

Standard scoring: +10 to a foundation, +5 waste-to-tableau, −15 foundation-to-tableau, draw-one recycle −100, and draw-three recycle −20 beginning with the fourth pass. Vegas starts at −52 and pays +5 per foundation card; “Keep score” carries its balance across deals. Changing the scoring system starts a new deal. These contracts follow the [original scoring help](https://documentation.help/Solitaire/sol28bz.htm). Solitude also applies +5 when exposing a hidden card, −2 each ten timed seconds, a −2 Undo penalty, and a nonnegative Standard score floor. Those additional details have regression coverage but have not been separately runtime-compared against all eight releases.

The timed completion bonus is integer `700000 / elapsedSeconds` only after 30 seconds, following [Microsoft KB Q101766](https://www.betaarchive.com/wiki/index.php/Microsoft_KB_Archive/101766). Vegas permits one pass in draw-one and three in draw-three. Ordinary scoring permits continued recycling. “Keep score” is disabled unless Vegas is selected, as required by [Microsoft KB Q72925](https://www.betaarchive.com/wiki/index.php/Microsoft_KB_Archive/72925).

Classic presets expose a face-down card until it is clicked; Vista's default flips it as part of the move. Classic Undo retains one state; Vista retains up to 200. Displaying a menu or modal dialog, minimizing, and losing focus pause Solitude's timer. The precise historical focus/pause policy and Vista scoring variants remain comparison gaps. Right-click foundation collection retains conservative eligibility. Its exact historical scan order is still unverified. The invented A shortcut, Collect/Finish button and classic Hint command have been removed. Classic stock draws, moves and failed drops are immediate; outlined valid destinations invert as described in the original help.

## FreeCell contract

The eight columns and four single-card free cells contain all 52 cards face-up. Home cells build by suit and do not allow cards back onto the table. Numbered deals use the Microsoft CRT shuffle and rank-major C/D/H/S starting deck. Every card of deal #1 is checked against its known eight-column layout. Range, repeatability and card conservation are checked separately; this does not mean every numbered deal is solvable.

For sequence movement, let `f` be empty free cells and `e` spare empty columns excluding the destination. Classic limits an empty-destination move to `f+1`; the implemented occupied-destination limit is `(f+1)*(e+1)`. Vista uses `(f+1)*2^e`. Classic retains one Undo; Vista retains 200. Safe automatic home placement requires Aces/twos or, for higher cards, both opposite-color foundations to be sufficiently advanced. These choices derive from [Keller's firsthand FAQ](https://www.solitairelaboratory.com/fcfaq.html); exact classic multi-column edge cases remain an inference to compare with originals.

The classic negative-image card selection, empty-column choice and automatic-home behavior after an ordinary move are described in [Keller's Microsoft deal #5 tutorial](https://www.solitairelaboratory.com/tutorial.html). Solitude now inverts the selected bottom card and keeps an ordinary move and its safe home moves together in Undo history. Its classic double-click-to-free-cell control is also corroborated by the preserved XP dialog string. Vista's shortcut targets a home cell.

**Requested behavior change:** double-click moves only the selected, exposed card. It deliberately does not trigger FreeCell's normal automatic-home sweep. The explicit FreeCell Collect action has been removed. This is a user-requested control policy, not a claim that every original FreeCell build suppressed automatic-home moves after a double-click. A manual click-and-destination move still invokes the documented safe sweep.

## Spider contract

Two decks produce 104 distinct card identities even with repeated visible suits. The initial tableau contains 54 cards; the stock holds five ten-card packets. A new row requires every column to contain a card. Cards can be placed on the next rank of any suit, while multi-card moves require one descending same-suit sequence. Complete King-to-Ace runs are removed automatically, exposed cards turn over, and eight completed runs win. Difficulty is one suit, two suits or four suits.

Scoring starts at 500, adds 100 per removed run, and charges for actions and Undo. Difficulty records are separate. The firsthand [2007 XP walkthrough](https://gamefaqs.gamespot.com/pc/566283-spider-solitaire/faqs/49848) documents the core rules, controls, scoring and separate difficulty statistics. Its assertion about the game's first Windows release is not used.

The [2009 firsthand Undo investigation](https://gamefaqs.gamespot.com/pc/566283-spider-solitaire/faqs/57700) describes classic Undo barriers at a stock deal **and** a completed run. Me/XP now discard history at both boundaries, including obsolete history from earlier Solitude saves. Vista keeps history across them. The implementation caps retained history at 200; it does not claim unlimited history identical to the classic program.

Undo now restores the cards without refunding prior action costs: a move at 499 followed by Undo yields 498. Undoing a completed Vista run removes its 100-point reward; completing it again cannot manufacture points. Historical score exploits are not deliberately reproduced. Solitude currently counts each stock deal as one charged action. Surviving sources are not fully consistent about that detail, so exact XP deal-count/score parity remains unverified rather than being silently certified.

Difficulty statistics record separately, including streaks. Classic tabs use the original Easy/Medium/Difficult labels. Previous combined statistics remain available under the host's F6 > Records because old saves cannot reconstruct difficulty attribution. Switching difficulty records an unfinished old game against its **old** difficulty. Abandoning a started game counts as a loss; switching Windows/game presets suspends it instead. Classic Spider now has a separate fixed save slot per Windows preset, Ctrl+S/Ctrl+O, save/open prompts, auto-save/auto-open and a separate Difficulty dialog. Native Spider save-file compatibility and original loss-detection prompts remain unimplemented.

## Double-click failure analysis and coverage

Two independent paths needed correction:

1. FreeCell's generic move routine invoked the safe-home sweep even for a chosen-card shortcut.
2. Windows delivers a second mouse-down during double-click. Letting that down event perform gameplay could draw a second packet, act on a freshly exposed card, or turn a selection into another move before the double-click handler.

The handler now captures card identity, state reference and move counter on the first down event. It suppresses the second gameplay down and permits the shortcut only if the first click did not already change the game and the same card is still under the pointer. Hit-testing rejects stale or out-of-range card rectangles. Covered-card double-clicks cannot substitute the column's bottom card.

Regression tests replay down/up/down/double-click/up at 100%, 125%, 150% and 200%, with animation both enabled and disabled. They assert one stock packet per double-click, one chosen destination card, unchanged unrelated eligible aces, exact Undo behavior, no flip-then-home action, and no covered-card substitution. Tests also exercise missing intermediate repaints. Additional rules tests cover safe collection step size, normal FreeCell automatic-home grouping, classic/Vista Spider barriers, repeated Undo penalties, score-cycle prevention, and save migration.

## Resource provenance and remaining comparisons

Reference-only bytes came from [xp-cards at commit 33a6d49](https://github.com/esc0rtd3w/xp-cards/tree/33a6d492ffa63f0b16b1ad982930c16d367f6bbb). SHA-256:

| File | SHA-256 |
|---|---|
| FreeCell resource data | `BA517F623B778F4B842F559EA344CB64D3CAE9DE172AECFA4E330C733BAAD601` |
| Solitaire resource data | `A6FC95A5B288593C9559BD177EC43BF9B30D8A98CF19E82BF5A1BA5600857F04` |
| Spider resource data | `D309A839843CCD8B9EDEF7A271A611EB7794F83A825B69A87CC259C9EDE87288` |

These are provenance fingerprints, not security or authenticity certifications. No new downloaded executable is embedded in the delivered game. A second archive was listed while seeking original help files; it contained none and was not used as behavioral evidence.

Classic card-back double-click now applies the choice and closes the dialog, following the [original card-back help](https://documentation.help/Solitaire/sol39gm.htm). Hidden handler checks cover all seven classic presets. Pixel checks cover negative-image selection in the raster and Tahoma renderers.

## Version 0.6 interface correction

The complete decoded XP MENU and ACCELERATOR evidence is in ignored `references/Mechanics/*-xp-controls.json`, generated by `tools/read-period-resources.mjs`. Classic Solitaire has Deal, Undo, Deck, Options and Exit; FreeCell has its numbered-game, statistics and three-option controls; Spider has a separate Difficulty dialog, six-option dialog, fixed save/open commands and Deal!/Move commands. These tables substantiate XP controls, not every earlier release.

The yellow in-table illegal-move banner was invented and is gone. Original FreeCell's optional illegal-move dialog was real: its option and message survive in the XP DIALOG/STRINGTABLE data. Classic Solitaire returns illegal drags without a banner. Removed additions include the quick toolbar, Game Feel, FPS overlay, generic confetti, classic Hint/Collect menu entries and Options expansion. Windows/game selection and combined legacy records remain in the host's window-menu/F6 selector.

Classic FreeCell is click-to-select and click-to-destination. Quick play now controls visible temporary free-cell transfers for sequences that fit the available cells. Transfers involving additional empty columns are still instant; their exact visible decomposition is a remaining comparison gap. Ordinary single-card transfers are immediate. Classic Spider animates dealing when enabled; normal moves are immediate. Vista retains smooth movement. Animation durations remain reconstructed, not measurements from original hardware.

The working Help index and period-styled About/win/statistics dialogs replace the former modern instruction panel and generic celebration. The Help viewer is a reconstruction with paraphrased text, not WinHelp. Read [PERIOD-UI-AUDIT.md](PERIOD-UI-AUDIT.md) for the feature-by-feature ledger.

The remaining work for a stronger exactness claim is side-by-side original-OS testing: Me Spider resources/rules; Vista scoring, auto-home ordering and animations; original focus behavior and every earlier shortcut table; exact stock-deal counting; original loss prompts; native save compatibility; complex FreeCell transfer timing and Spider sound-event mapping. Vista uses simplified game-over dialogs instead of its original effects. Existing [window references](WINDOW-REFERENCES.md) and [asset provenance](ASSET-SOURCES.md) record visual evidence separately. Automated checks reduce regressions; they cannot guarantee that no unexpected behavior remains.
