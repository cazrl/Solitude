# Performance and feel - Solitude 0.4.0

Measured locally on 2026-09-08. The new renderer substantially reduces CPU painting cost and the frame clock paces active motion independently of WinForms' ordinary timer. These are local offscreen measurements, not captured monitor FPS or end-to-end input-latency measurements.

## Full-window rendering

`tests/Solitude.Benchmarks.csproj` calls the production renderer into a bitmap at 150% display size, warms eight frames and records 120 frames. Spider deals all five stock packets first, placing all 104 cards on the table. Both builds use the same sizes, seed and card counts; 0.4 additionally draws its default quick action bar.

| Profile | Pixels | 0.3 median ms | 0.4 median ms | 0.4 p95 ms |
|---|---:|---:|---:|---:|
| XP Solitaire | 960 x 720 | 5.95 | 3.56 | 4.44 |
| XP FreeCell | 1080 x 720 | 17.35 | 3.76 | 4.47 |
| XP Spider | 1350 x 900 | 32.19 | 5.35 | 6.67 |
| Vista Solitaire | 1080 x 810 | 45.06 | 5.69 | 7.21 |
| Vista FreeCell | 1080 x 810 | 69.63 | 5.99 | 7.24 |
| Vista Spider | 1350 x 900 | 102.60 | 8.55 | 11.37 |

The busiest full-window case is about **12 times faster**. These samples repaint the entire window with a warm stationary-board cache; they do not measure cold startup or the cost of every possible animation. Raw results are in `artifacts/benchmark-v030.json` and `artifacts/benchmark-v040.json`.

## Frame pacing and overload behavior

A message-only native window uses the production `FramePump`, real Windows message dispatch and the production Vista Spider renderer. It moves one exposed card over the same 104-card board for approximately 2.2 seconds per case. Partial mode uses the production damaged-region calculation; full mode deliberately repaints the whole window. No visible window is shown and no desktop input is sent.

| Redraw | Limit | Delivered updates/sec | p95 interval ms |
|---|---:|---:|---:|
| Full window | 60 | 60.0 | 17.11 |
| Full window | 120 | 101.9 | 11.96 |
| Full window | 144 | 104.4 | 11.38 |
| Moving region | 60 | 60.0 | 17.07 |
| Moving region | 120 | 120.0 | 8.71 |
| Moving region | 144 | 144.0 | 7.34 |

Every case also delivered 35 ordinary 50 ms WinForms timer ticks and ended within 2.21 seconds. This checks that frame messages leave time for lower-priority Windows messages under load. A frame remains outstanding until painting finishes; overdue work yields at least 1 ms before another request. Earlier scheduling that acknowledged before painting could crowd out those messages and was corrected.

The high-resolution waitable timer was available in this run. Windows' lower-resolution timer and a WinForms fallback are supported. No system-wide timer-resolution change is requested. The active clock stops requesting frames when idle, unfocused or minimized. Game/status/save housekeeping still runs on its separate 250 ms timer. The F12 overlay measures application paint timing and intervals; it is not a DWM/display presentation counter. There is no VSync guarantee. Hardware, monitor refresh, larger windows, cold caches, dealing many cards and other applications can change delivered performance.

## Changes that affect play

- Cached card faces/backs at their drawing size, pre-scaled Vista felt and a cached stationary board. Stack coordinates are calculated once per layout/model change.
- Partial redraws include old and new drag positions, shadows and legal-target outlines. Classic raster scaling uses a fixed origin to prevent one-pixel phase changes at fractional display scales. All 48 incremental/full-render comparisons passed.
- Time-based eased deals, flips, drop settling and return motion in all eras. Undo begins at a moving card's current position and width. Dragged cards follow the latest pointer directly.
- Safe collection waits for each motion to finish. Classic Klondike retains its bouncing cards; the other modes use a short reconstructed celebration before the win dialog.
- 60/120/144 FPS limits, three motion speeds, reduced motion, optional legal-target highlighting/action bar and F12 diagnostics. Defaults: 120 FPS, Natural (210 ms), animations/highlights/action bar enabled.
- JSON serialization and atomic writes run through one background writer. New pending saves replace older pending snapshots; close explicitly flushes the latest state. Snapshot capture stays on the UI thread. Tests cover the latest-snapshot guarantee, backup preservation and write-failure recovery.

The game rules, scoring profiles and historical artwork remain those of the existing 17 supported profiles. Motion enhancements and the optional quick action bar are Solitude additions.

## Reproduce

```powershell
dotnet run --project tests/Solitude.Benchmarks.csproj -c Release -p:OutputPath=../artifacts/benchmark-build/ -- artifacts/benchmark.json --pacing
```

Microsoft documents the high-resolution waitable timer flag in [CreateWaitableTimerExW](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-createwaitabletimerexw), timer deadlines in [SetWaitableTimer](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-setwaitabletimer), and ordinary UI timer accuracy in [WinForms Timer limitations](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/limitations-of-the-timer-component-interval-property).
