# Performance — Solitude 0.5.0

Measured locally on 2026-09-08 with the production renderer and frame pump. These are offscreen CPU-rendering and message-delivery measurements, not monitor FPS or end-to-end input latency. The previous report is retained in [PERFORMANCE-0.4.0.md](PERFORMANCE-0.4.0.md).

## Warm full-window rendering

Eight warmup frames, then 120 measured frames at 150%. Spider has all 104 cards on the tableau. Version 0.5 defaults to the menu-only board and renders Windows 2000/XP text at the actual display resolution. Raw data: `artifacts/benchmark-v050.json`.

| Profile | Pixels | 0.5 median ms | 0.5 p95 ms |
|---|---:|---:|---:|
| XP Solitaire | 960 x 720 | 1.81 | 2.42 |
| XP FreeCell | 1080 x 720 | 1.97 | 2.74 |
| XP Spider | 1350 x 900 | 3.11 | 4.06 |
| Vista Solitaire | 1080 x 810 | 6.37 | 7.93 |
| Vista FreeCell | 1080 x 810 | 6.02 | 6.97 |
| Vista Spider | 1350 x 900 | 7.68 | 9.19 |

This test uses the stationary-board cache. Cold start, arbitrary board changes and every animation frame are outside this measurement. The previous XP medians were 3.56, 3.76 and 5.35 ms, with a different default toolbar layout.

## Frame pacing

A message-only native window runs the real frame clock and production Vista Spider renderer for approximately 2.2 seconds per case, dragging a card over the 104-card board. Partial mode uses the production damaged-region calculation. Full mode deliberately redraws everything and can exceed its frame budget.

| Redraw | Requested limit | Delivered updates/sec | p95 interval ms |
|---|---:|---:|---:|
| Full | 60 | 60.0 | 17.08 |
| Full | 120 | 105.2 | 11.00 |
| Full | 144 | 100.2 | 11.69 |
| Partial | 60 | 60.0 | 17.00 |
| Partial | 120 | 120.0 | 8.74 |
| Partial | 144 | 144.0 | 7.33 |

All six cases passed the ordinary-Windows-timer starvation guard. The frame pump keeps one outstanding request and acknowledges after painting; overloaded full redraws do not accumulate a frame queue. Idle boards do not continuously request motion frames.

The settings are limits, not performance guarantees. Full-window workload and system scheduling can prevent reaching the limit. The F12 overlay reports the current application's measurements for checking an actual machine.

## Interaction safeguards

Double-click shortcuts isolate one card. Explicit FreeCell collection advances one card per animation step; ordinary FreeCell moves retain their safe-home group. Input checks current card identity, and stale painted rectangles cannot index outside a pile. Existing flip/slide endpoints, interrupted Undo, invalid-drop return, drag settling and 48 partial-versus-full pixel comparisons remain covered by the UI suite. Tests use hidden forms and send no desktop input.
