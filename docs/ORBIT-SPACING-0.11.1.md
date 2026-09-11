# ORBIT spacing — 0.11.1

The reported 1680 × 1080 layout spread seven columns across almost the entire window. Cards were about 136 pixels wide with about 93 pixels between them: roughly 68% of card width. The gap grew further on wide windows once cards reached their maximum size.

## Reference comparison

Live desktop boards inspected on 11 September 2026 at a 1265 × 712 browser viewport:

| Reference | Approximate card width | Approximate clear horizontal gap | Gap / card width |
|---|---:|---:|---:|
| [Solitaired](https://solitaired.com/) | 113 px | 7 px | 6% |
| [Solitaire.com / Tripledot](https://solitaire.com/klondike-solitaire/) | 88 px | 12 px | 14% |
| ORBIT before, at the reported size | 136 px | 93 px | 68% |
| ORBIT 0.11.1, same size | 136 px | 22 px | 16% |

Reference figures are approximate measurements from rendered browser screenshots, excluding shadows. They describe those layouts at that viewport, not a universal Solitaire standard. Both show substantially closer columns than the reported ORBIT layout.

## Change

ORBIT now centers its seven-column playing grid with a gap of 16% of card width. The small extra clearance accommodates ORBIT outlines and independent column scroll rails. The card width limits still apply on wide screens, and extra width becomes outside margins rather than larger gaps.

Stock, waste, foundations, tableau hit targets, guidance and animation destinations use the same grid. Card sizes, vertical rank/suit exposure, draw-three overlaps and header/dock positions remain as before. Historical editions retain their existing layout calculation.

## Verification

14,822 ORBIT checks passed. New coverage exercises 800 × 540, 1120 × 720 and 1920 × 720 logical viewports at 100%, 125%, 150% and 200%. It checks centering, bounded gaps, dealt-card visibility, foundation/tableau alignment, separate scroll targets and draw-three clearance. Existing tests cover card movement, flip continuity, Undo, interrupted motion, hints, completion and saved preferences.

Production-renderer images at the reported 1680 × 1080 size, minimum window and ultrawide layout were visually inspected. Evidence is under `artifacts/orbit-spacing`; test log: `artifacts/orbit-spacing-ui.log`. These are offscreen renders and simulated input, not a physical mouse/play session.
