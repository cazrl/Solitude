# ORBIT card motion — 0.11.7

## Dealing from the stock

The opening deal used the stock's rectangle as its source but retained the destination's tableau position. The renderer therefore classified each flight as movement inside one column and clipped it to that column's viewport. The segment between the stock and tableau was invisible: cards appeared halfway down their flight, cut off along the tableau's top edge.

The source pose now explicitly identifies the stock. Dealt cards can cross the board from the visible deck, and delayed cards remain inside it until their launch time instead of painting duplicate sprites over departing cards. The existing tableau clip still handles rearrangement within a column. All 28 cards retain their staggered timing and final positions.

![Dealing from the stock](images/orbit-deal.gif)

## Winning with the cards

The former dot constellation is replaced by a 7.6-second sequence using the actual 52 cards from the completed foundations:

1. Kings lift first, progressively revealing the remaining cards beneath them.
2. Four streams of cards form a rotating ellipse, changing apparent size with depth.
3. A travelling ripple turns cards over to reveal their ORBIT backs and returns them face up.
4. The cards settle into four suit fans around the completion message, then the results appear.

The animation begins after the final moving card lands. The effect samples elapsed time instead of advancing by frame count. It only reads a snapshot of the foundation cards; it does not remove, reorder or move cards in the saved deal. Click or Escape skips to results, and disabling Spatial card motion skips the sequence entirely. The existing first-error recovery also suppresses it for that session.

![Card victory](images/orbit-win.gif)

The renderer reuses fixed-resolution card sprites as positions, rotation and flip projections change. It does not generate a new bitmap for every apparent card size. A redundant full-board glow was removed during profiling; local trails and the existing scene provide the lighting. All three palettes are supported. Both fans and the skip prompt fit the compact window.

## Verification

- 2,737 focused animation checks on the final build: stock-origin metadata and real pixels above the tableau, 52 distinct victory identities throughout, finite and bounded poses, bounded sprite caching, scale/compact coverage, all palettes, timed results, skip and reduced motion.
- A real final foundation move waits for its landing animation, enters the 52-card sequence and reaches results without changing the won state.
- 24,684 broader UI checks, 384 native-window checks and 53 engine verification groups passed. The full suite includes historical motion, partial repainting, interrupted Undo, foundation underlays, ORBIT last-column reveal, centering/morphing, settings, persistence and the clock.
- 29 source/package PNGs match, including the new victory and dealing samples. Existing static ORBIT views match the previous release except the About version text.

The GIFs use sampled production-renderer frames, assembled with `tools/render-orbit-motion-previews.py`. They are previews, not physical display recordings. Paint costs and package details are in [VERIFICATION.md](VERIFICATION.md); the largest windows can exceed a 60 FPS paint budget during this effect. The original reported endgame crash was not reproduced and is not claimed fixed by this visual change.
