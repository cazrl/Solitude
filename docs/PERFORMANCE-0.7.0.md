# Performance — Solitude 0.7.0

Measured locally on 2026-09-08 with the production renderer and frame pump. These are offscreen CPU-rendering and Windows-message delivery measurements, not monitor FPS or end-to-end input latency. No game window was shown. Previous report: [PERFORMANCE-0.6.0.md](PERFORMANCE-0.6.0.md). Raw results: `artifacts/benchmark-v070.json`. This run includes the cached GDI text-mask renderer; text masks are bounded to 256 entries or approximately 16 MB per skin.

## Warm full-window rendering

Eight warmup frames, then 120 measured frames at 150%. Spider has all 104 cards on the tableau.

| Profile | Pixels | Median ms | p95 ms |
|---|---:|---:|---:|
| XP Solitaire | 960 × 720 | 1.46 | 1.61 |
| XP FreeCell | 1080 × 720 | 1.55 | 1.74 |
| XP Spider | 1350 × 900 | 2.24 | 2.47 |
| Vista Solitaire | 1080 × 810 | 4.23 | 4.76 |
| Vista FreeCell | 1080 × 810 | 4.08 | 6.64 |
| Vista Spider | 1350 × 900 | 9.12 | 9.99 |

The stationary-board cache is active. These figures do not measure cold startup or every board mutation. Timing changes of this size between runs are not evidence of a universal speedup or slowdown.

## Frame pacing

A message-only native window exercises the real frame clock and production Vista Spider renderer for about 2.2 seconds per case, dragging over the 104-card board. Partial mode uses the production damage calculation; full mode deliberately redraws the whole window.

| Redraw | Requested updates/sec | Delivered updates/sec | p95 interval ms |
|---|---:|---:|---:|
| Full | 60 | 60.0 | 17.10 |
| Full | 120 | 92.0 | 11.73 |
| Full | 144 | 93.6 | 11.59 |
| Partial | 60 | 60.0 | 17.06 |
| Partial | 120 | 120.0 | 8.72 |
| Partial | 144 | 144.0 | 7.31 |

All six cases passed the ordinary-Windows-timer starvation guard. The frame pump keeps one outstanding request and acknowledges after painting, so overload does not build a queue of stale frames. Idle boards stop requesting motion frames.

The normal internal target is 120 active updates/sec. FPS settings and the F12 overlay have been removed from play. Classic Solitaire uses immediate moves, FreeCell flashes supported intermediate cell transfers unless Quick play is enabled, and classic Spider can animate dealing. Vista retains eased movement. There is no artificial simulation of an old CPU's frame rate.

## Interaction coverage

The hidden UI suite checks complete double-click sequences across all 17 profiles and four scales, selected-card identity, safe-home grouping, period shortcuts/options and checkpoint recovery. Drag frames are compared with full renders at 100/125/150/200% in classic, XP and Vista. The partial-frame check asserts that a real drag occurred. Vista flip/slide endpoints, interrupted Undo and invalid-drop return remain tested. None of these tests inject desktop input.

The renderer and gameplay validations are listed in [VERIFICATION.md](VERIFICATION.md).
