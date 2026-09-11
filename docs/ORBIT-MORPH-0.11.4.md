# Edition centering and ORBIT morph — 0.11.4

All editions now use final-size startup placement and center after switching, in either direction. Previously, returning from ORBIT left a smaller historical window at ORBIT's top-left corner. Startup and switching now share the same placement routine and respect the selected monitor working area.

Entering ORBIT from a historical game animates the native window bounds for 0.9 seconds with quintic easing. A luminous front reveals ORBIT across the old surface while orbital arcs and particles resolve around it. Two cached production-renderer surfaces keep card layouts stable during the resize; the final frame hands back to the live board. It supports both expanding a smaller window and shrinking a maximized one.

Gameplay inputs and the game clock pause during the morph. Escape completes it immediately, and losing focus settles at the final bounds. The ORBIT Spatial card motion preference disables the transition. Returning to historical editions centers immediately. Both transition surfaces are disposed at completion or window disposal.

A regression uncovered during the all-edition matrix is also fixed: switching from ORBIT to FreeCell or Spider previously cleared motion after replacing the game but before replacing the renderer. That could apply ORBIT's seven-column scroll array to an eight- or ten-column game. Motion now stops while the old game and renderer still match.

## Verification

15,788 ORBIT checks passed in the final suite. Included coverage:

- 252 placement assertions: ORBIT startup with four working areas and scales, plus startup and ORBIT round trips for every historical game at all four scales.
- 648 morph assertions: growing and shrinking from normal/maximized windows, bounds containment, repeatable rendered frames, unchanged deal state, blocked gameplay/accessibility input, final-frame handoff, Escape, focus loss and reduced motion.
- Existing card animation, hints, completion, save state, preference and real-time clock checks.

Sampled transition frames were visually inspected. The 31-frame sequence is in `artifacts/orbit-morph`; logs are `artifacts/orbit-morph-final-ui.log`, `artifacts/edition-placement-ui.log` and `artifacts/build-v0114.log`.

These are production-renderer samples and lifecycle/simulated-input tests, not physical display FPS measurements. Earlier in this turn, the pre-fix clipped desktop origin was verified. Automatic approval review blocked closing the user's existing game to protect its current deal. A development copy was launched, then the user stopped Computer Use with Escape before the corrected launch could be inspected; no further desktop automation was used. Final physical startup/morph and multi-monitor visual verification remain incomplete.
