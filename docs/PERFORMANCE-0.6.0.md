# Performance — Solitude 0.6.0

Measured locally on 2026-09-08 with the production renderer and frame pump. These are offscreen CPU-rendering and Windows-message delivery measurements, not monitor FPS or end-to-end input latency. No game window was shown. Previous report: [PERFORMANCE-0.5.0.md](PERFORMANCE-0.5.0.md). Raw results: `artifacts/benchmark-v060.json`.

## Warm full-window rendering

Eight warmup frames, then 120 measured frames at 150%. Spider has all 104 cards on the tableau.

| Profile | Pixels | Median ms | p95 ms |
|---|---:|---:|---:|
| XP Solitaire | 960 × 720 | 1.99 | 2.74 |
| XP FreeCell | 1080 × 720 | 1.96 | 2.70 |
| XP Spider | 1350 × 900 | 3.04 | 3.68 |
| Vista Solitaire | 1080 × 810 | 5.67 | 7.47 |
| Vista FreeCell | 1080 × 810 | 6.23 | 7.44 |
| Vista Spider | 1350 × 900 | 8.22 | 9.66 |

The stationary-board cache is active. These figures do not measure cold startup or every board mutation. Timing changes of this size between runs are not evidence of a universal speedup or slowdown.

## Frame pacing

A message-only native window exercises the real frame clock and production Vista Spider renderer for about 2.2 seconds per case, dragging over the 104-card board. Partial mode uses the production damage calculation; full mode deliberately redraws the whole window.

| Redraw | Requested updates/sec | Delivered updates/sec | p95 interval ms |
|---|---:|---:|---:|
| Full | 60 | 60.0 | 17.07 |
| Full | 120 | 107.8 | 11.02 |
| Full | 144 | 105.2 | 11.32 |
| Partial | 60 | 60.0 | 17.04 |
| Partial | 120 | 119.0 | 8.74 |
| Partial | 144 | 144.0 | 7.41 |

All six cases passed the ordinary-Windows-timer starvation guard. The frame pump keeps one outstanding request and acknowledges after painting, so overload does not build a queue of stale frames. Idle boards stop requesting motion frames.

The normal internal target is 120 active updates/sec. FPS settings and the F12 overlay have been removed from play. Classic Solitaire uses immediate moves, FreeCell flashes supported intermediate cell transfers unless Quick play is enabled, and classic Spider can animate dealing. Vista retains eased movement. There is no artificial simulation of an old CPU's frame rate.

## Interaction coverage

The hidden UI suite checks complete double-click sequences across all 17 profiles and four scales, selected-card identity, safe-home grouping, period shortcuts/options and checkpoint recovery. Drag frames are compared with full renders at 100/125/150/200% in classic, XP and Vista. The partial-frame check asserts that a real drag occurred. Vista flip/slide endpoints, interrupted Undo and invalid-drop return remain tested. None of these tests inject desktop input.

The renderer and gameplay validations are listed in [VERIFICATION.md](VERIFICATION.md).
