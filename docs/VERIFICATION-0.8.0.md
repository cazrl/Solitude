# Verification — Solitude 0.8.0

Built locally on 2026-09-08. This update corrects the frame alignment shown in the user's XP/Vista screenshots, Vista's substituted serif lettering, and fractional card repainting.

## Delivered executable

- `dist/Solitude.exe`, file version **0.8.0.0**.
- Size: **59,127,139 bytes**.
- SHA-256: `9ACFA8D254E883F2431BFE2CA91AB515EE8A0EBA7860C0B8533C377F99FEB6CC`.
- Exactly one portable, self-contained Windows x64 EXE in `dist`.
- The previous 0.7 package remains unchanged at `artifacts/release-v070/Solitude.exe`. Its report is [VERIFICATION-0.7.0.md](VERIFICATION-0.7.0.md).
- Close any older copy before opening this EXE so only one process writes the shared save.
- Local packaging only. No installation, upload, visible game launch or desktop input. Verification used isolated/ephemeral state; personal saves were not opened or changed.
- Publish passed. NU1900 reports that the online NuGet vulnerability-data endpoint was unavailable; that scan did not complete.

## Corrections and evidence

- Caption icons render through the same graphics transform as the title and controls. This fixes the tiny, displaced icon in the user's screenshots.
- XP and Vista use explicit caption layouts shared by painting and hit testing. Vista's restored header is 27 logical pixels; its maximized header is 21 with flush client edges. The upper part of a caption button no longer triggers native edge resizing.
- Menu label spacing is measured, and each popup anchors to its own label. The Settings button remains visible.
- Vista uses preserved button, glyph, frame, reflection and game-icon images. The title glow derives from the actual text mask.
- Vista's old mask produced serif glyphs despite reporting Segoe UI. Output-resolution GDI+ masks now paint the requested face correctly. Classic bitmap/GDI text remains unchanged.
- Board and card caches copy at snapped device-pixel origins, preventing clipping from changing the sampled card pixels during partial redraws.
- Shared compatible settings, per-preset deals and the requested isolated double-click behavior remain intact.

Reference captures, measured coordinates, artifact provenance and limits are in [FRAME-ALIGNMENT.md](FRAME-ALIGNMENT.md). The before/after image is `artifacts/caption-comparison-v080.png`: its before rows are crops of the user's actual screenshots; its after rows are production 0.8 renders at 150%.

## Validation

- **1,725 production UI checks passed**: `artifacts/ui-v080.log`.
- The 100 new caption/font checks measure actual icon pixels at four scales and translated origins, compare XP/Vista reference coordinates, reject Vista serif glyphs, exercise native WM_NCHITTEST over caption controls, and check menu anchors and maximized client bounds.
- Existing coverage passes for all 17 profiles, four display sizes, shared settings and restart persistence, pending difficulty, unchanged suspended cards, same-rule Restart Game, save recovery, complete double-click event sequences, animation interruption, and partial/full repaint equivalence.
- **50 rules/persistence groups passed in 0.7**: `artifacts/rules-v070.log`. Engine and storage code are unchanged in 0.8; those groups were not rerun. The full production UI suite above was rerun against the final 0.8 source.
- Development and packaged EXE each rendered **225 application views** plus **36 motion samples**. All **261 PNG hashes match**.
- Visual inspection covered XP/Vista at 100/125/150/200%, restored/maximized captions, the Vista Options dialog, visible Settings access, game icons, readable Segoe UI, and card/table placement. Caption comparisons use original Microsoft DWM captures and the recorded XP references.
- Packaged offscreen render and notice export exited 0. All **five** exported notices match their source files.
- Artifacts: `artifacts/renders-v080-dev`, `artifacts/renders-v080-package`, `artifacts/licenses-v080`, `artifacts/package-validation-v080.json`, `artifacts/package-v080.log`.

The final offscreen frame-pump benchmark delivered **119.1 updates/second** at the normal 120 target in partial-redraw mode, p95 interval **8.96 ms**. All six pacing cases passed the Windows-timer starvation guard. These measure CPU/message-loop behavior, not monitor FPS or end-to-end input latency. Details: [PERFORMANCE.md](PERFORMANCE.md).

The frame still uses reconstructed glass tint rather than live desktop blur. Tests establish the implemented corrections, not complete pixel/behavior identity with every original Windows installation. Other known historical gaps remain documented in [PERIOD-UI-AUDIT.md](PERIOD-UI-AUDIT.md) and [MECHANICS-AUDIT.md](MECHANICS-AUDIT.md).
