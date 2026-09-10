# ORBIT / Solitude 2126

ORBIT is a fictional edition of Solitude, imagined one century beyond 2026. It is a complete alternative Klondike presentation, available alongside the eight historical Windows presets in version 0.10.0. It does not claim to recreate a real Microsoft release. FreeCell and Spider remain available in their existing historical editions.

## A game inside an observatory

The design treats a future card table as a quiet observatory: a nearly black planetary horizon, thin orbital paths, distant stars and luminous foundation bays. The chrome, controls, dialogs, card backs and court cards share a geometric visual language. White porcelain-style faces keep familiar red/black suits and readable ranks; the court figures become Navigator, Sovereign and Architect motifs.

Three palettes change the cards, orbital light and scene accents: **Aurora** in teal, **Solstice** in gold and **Nebula** in violet. These are original procedural drawings rendered at the chosen display size. No new artwork, audio or fonts were downloaded for this edition.

## Motion with purpose

- Dealing fans cards from the stock with a staggered launch. A move rises along a shallow arc, banks and settles with a quintic easing curve. Turning cards compress around their center before revealing the new face.
- Subtle particles follow moving cards. Arriving foundation cards trigger expanding docking waves. Turning off light trails and waves removes those accents while keeping card motion.
- Hints outline the relevant piles and trace the transfer. A no-move message is brief and stays in the lower status area.
- A completed game gathers 52 points of light from the foundations into a moving constellation, followed by score, time and move results. Click or Escape skips to results.
- Turning **Spatial card motion** off makes moves and wins immediate. The scene is static when idle; it does not request continuous animation frames.

The active frame clock targets 120 updates per second. Actual display smoothness depends on hardware, window size and the Windows compositor. The [verification report](VERIFICATION.md) provides measured offscreen render costs and callback pacing, including larger-size limits.

These previews are sampled production-renderer frames, rather than a monitor capture or FPS measurement:

![ORBIT deal choreography](images/orbit-deal.gif)

![ORBIT constellation completion](images/orbit-win.gif)

## Playing and choosing an edition

Open **Settings → ORBIT / Solitude 2126 → OK**, or press F6. Settings remains at the top right. The action dock supplies Undo, Hint and New deal. **Complete the orbit** appears when the stock is exhausted and every tableau card is face up; it begins foundation collection.

| Action | Input |
|---|---|
| Draw | Click the stock; Space with the stock selected |
| Move | Drag, or select a card and click its destination |
| Move one card home | Double-click that exposed card |
| Collect safe foundation moves | Right-click the table |
| Undo | Dock button or Ctrl+Z |
| Hint | Dock button or H |
| New deal | Dock button or F2 |
| Help | F1 |
| Statistics | F4 |
| Experience | Header button or F5 |
| Editions and display size | Settings or F6 |
| Atmosphere palette | Experience → Atmosphere, or F7 |
| Game menu | Alt+G |
| Keyboard piles | Left/Right/Tab, Enter/Space; Up/Down within a selected column |
| Cancel or skip a win | Escape |

ORBIT follows Klondike: descending alternating-color tableau columns, Kings in empty columns, foundations ascending by suit from Ace to King. It offers draw one/three and Standard, Vegas or no scoring, automatic exposed-card flips and full-session Undo. Double-click acts on its chosen card only. Draw/scoring changes start a new deal, as stated in Experience.

## Persistence and accessibility choices

ORBIT saves the active deal and Undo history automatically. Switching to another edition suspends it; returning restores it. Draw count and scoring are shared with compatible Klondike presets. Palette, motion, sound and lighting are specific to ORBIT, while display size remains global. Selecting a different era does not copy future animation or artwork into it.

Experience edits remain a draft until Apply. Opening Atmosphere from Experience preserves that draft. Applying a nested palette returns to Experience; cancelling Experience discards the whole draft. Opening Atmosphere directly with F7 applies the palette on its own.

Display sizes are 100%, 125%, 150% and 200%, with an 800 × 540 logical minimum. Cards use cached art at device resolution, full-frame rendering avoids stale alpha edges, and the static board cache excludes cards still owned by an animation. Tests compare cached and freshly rebuilt frames, including the animation deadline/cleanup gap. Reduced motion, independent sound control, keyboard actions and high-contrast card faces are provided; complete screen-reader accessibility is not claimed.

## Source and validation

The original graphics live in `src/FutureArt.cs`; window controls in `src/Skin.Future.cs`; presentation, settings and synthesized sound in `src/GameWindow.Future*.cs`. The existing Klondike engine supplies legal moves, scoring, Hint, Undo and persistence. `tests/FutureChecks.cs` covers edition availability, three palettes at four scales, nested settings, double-click scope, saved sessions, small-window layouts and both animated and reduced-motion wins. Shared motion tests also exercise Undo in the middle of an ORBIT flip.

This release was validated with isolated offscreen production renders and input handlers. Monitor-visible animation, audio output, clean-machine operation and a prolonged visible-session soak remain unverified.
