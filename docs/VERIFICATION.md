# Verification — Solitude 0.10.0

Built and checked locally on 10 September 2026. This release adds the fictional [ORBIT / Solitude 2126 edition](ORBIT-2126.md). Previous results remain in [VERIFICATION-0.9.1.md](VERIFICATION-0.9.1.md).

## Delivered executable

| Item | Verified result |
|---|---|
| Launch path | `dist/Solitude.exe` |
| Retained release | `artifacts/release-v0100/Solitude.exe` |
| File version | **0.10.0.0** at both paths |
| Size | **63,646,565 bytes** |
| SHA-256 | `EC468CAAF8C19166ADB43B448436EA6C5EE9821D1588FA9C87F23DC6999BB9C7` |
| Package | Exactly one self-contained Windows x64 EXE |
| Previous build | Preserved during atomic replacement; 0.9.1 retained under `artifacts/release-v091` |

The normal and retained copies have identical hashes. The package contains the runtime, artwork and notices. Native runtime components may extract into Windows temporary storage. Build log: `artifacts/build-final-v0100.log`.

## Rules, input and persistence

- **53 rules/persistence groups passed; 0 failed.** The existing engine, 60,000-action conservation exercise and 17-profile rules/restore exploration remain intact. The new edition uses the existing modern Klondike rules, with automatic flips and full-session Undo.
- **5,805 production UI assertions passed.** These cover all 17 historical profiles and ORBIT at 100/125/150/200%, including actual input-handler event sequences, dialog controls, settings, saves and animation.
- **677 ORBIT-specific assertions** cover three palettes, repeated frames, cache handoff, rounded edges, double-click isolation, stock double-click suppression, reduced motion, idle frame scheduling, palette application, nested settings Apply/Cancel, edition round trips, disk restoration and Undo history, compatibility boundaries, minimum-window controls/dialogs and both win paths. Four of these verify that interrupting a flip with Undo preserves the card's position, width and bank angle and eventually settles it flat.
- The shared motion suite also exercises ORBIT stock flips and mid-flight Undo. The remaining historical rendering, double-click, input and persistence checks continue to pass.
- Appearance opened from Experience keeps the uncommitted draft. Cancelling Experience cancels its nested palette as well. Independent F7 appearance changes save directly.

Logs: `artifacts/orbit-rules.log` and `artifacts/orbit-ui-release.log`. Tests used ephemeral forms and isolated workspace state. No visible game window was launched, desktop input sent, or personal save read or altered.

## Rendering, regression boundaries and package parity

The final source and packaged EXE each rendered **237 application views and 42 motion samples**. All **279 PNG hashes match**. All **five exported embedded notices match** their source files.

Of the previous release's 261 renders, **244 remain byte-identical**. The only differences are the 17 Settings views, which now include the ninth edition. Historical boards, captions, menus, period dialogs and motion samples in this comparison did not change.

ORBIT's continuity checks compare animation frames with a freshly rebuilt static board cache at each frozen time. They exercise the gap between a flight reaching its deadline and the animation loop removing it. Repeated window-edge blending was caught during optimization and fixed by clearing the exposed surface before drawing alpha edges. Existing Vista continuity checks still pass.

Visual review covered the three palettes, constellation and win dialog, plus the packaged 150% Experience and Settings views. Motion previews under `docs/images` were sampled from production render frames; they are not a monitor recording or a frame-rate measurement.

Evidence:

- `artifacts/source-final-v0100-renders`
- `artifacts/package-final-v0100-renders`
- `artifacts/orbit-checks` and `artifacts/orbit-demo`
- `artifacts/notices-final-v0100`
- `artifacts/package-validation-v0100.json`

## Performance

The final benchmark ran without competing UI/render tests. Each size records 180 warm moving frames and 180 warm settled frames. The moving sequence includes staggered cards, bank transforms, flips and light trails. Timed calls replay flight positions through the production renderer; they are not measurements of presentation to a physical display.

| Display size | Moving median / p95 ms | Settled median / p95 ms |
|---|---:|---:|
| 100% | 4.36 / 5.95 | 1.41 / 1.65 |
| 125% | 6.63 / 12.80 | 2.25 / 3.04 |
| 150% | 10.66 / 15.69 | 4.65 / 6.62 |
| 200% | 16.35 / 23.17 | 6.04 / 9.31 |

A separate 150% drag probe using the real Windows message queue and production frame clock delivered **60.0 / 120.0 / 144.0 callbacks per second**, with p95 intervals of **17.10 / 8.73 / 7.32 ms**. Its independent heartbeat remained responsive. The application targets 120 updates per second while animating and stops requesting continuous frames when idle.

These are CPU costs and offscreen callback rates, not monitor FPS or latency. The more demanding deal animation, particularly at 200%, can exceed a 120 Hz or 60 Hz frame budget. Results vary with hardware and system load. No universal smoothness guarantee is made.

The implementation caches the scene, cards and static board, clips card flights to the playing area and rasterizes the rounded scene corners once. This avoids the original full-window complex clip around every moving card. Earlier probes are retained as `artifacts/orbit-performance*.json`; final results are published in [ORBIT-PERFORMANCE-0.10.0.json](ORBIT-PERFORMANCE-0.10.0.json).

## Scope and limits

ORBIT's art, scene, icon and synthesized audio are original procedural code. No new third-party assets or package dependencies were introduced. It is a fictional Klondike edition, not a prediction or reproduction of a future Windows product. FreeCell and Spider retain their historical editions; Windows 7–11 remain deferred.

The historical fidelity limits from 0.9.1 still apply. Matching old renders proves regression stability, not identity to every original Windows build. Monitor-visible animation, audio playback, task switching, multiple monitors, clean-machine portability and a prolonged visible-session resource soak were not tested in this release.
