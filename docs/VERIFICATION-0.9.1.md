# Verification — Solitude 0.9.1

Built and checked locally on 10 September 2026. The [application audit](AUDIT-0.9.1.md) explains the reproduced Vista flicker defects and the broader fixes. Previous results remain in [VERIFICATION-0.9.0.md](VERIFICATION-0.9.0.md).

## Delivered executable

| Item | Verified result |
|---|---|
| Normal launch path | `dist/Solitude.exe` |
| Retained release | `artifacts/release-v091/Solitude.exe` |
| File version | **0.9.1.0** at both paths |
| Size | **63,600,483 bytes** |
| SHA-256 at both paths | `007D308069C93F50075BDDB1B640FD892C8EA1E945AFF4F2EB6C2273DEBF4858` |
| Package contents | Exactly one self-contained Windows x64 EXE |
| Included runtime | .NET and WindowsDesktop **10.0.11** |
| Previous executable | Preserved by atomic replacement; 0.9.0 also retained in `artifacts/release-v090` |

`build.ps1 -SkipTests` packaged the source after the checks below. Build log: `artifacts/build-final-v091.log`. The delivered and retained copies have identical hashes. No existing game was terminated or visible game window launched. Tests used ephemeral forms and isolated workspace state; personal saves were not read or altered. Native single-file components can extract into Windows temporary storage. Clean-machine portability remains untested.

## Rules, persistence and interface

- **53 rules/persistence groups passed; 0 failed.** Includes the existing 60,000-action conservation test, malformed nested-save recovery, signed Vegas records, and a new 17-profile rules/restore exploration over 12 seeds per profile.
- **5,120 production UI checks passed.** All 17 game profiles, 100/125/150/200%, full double-click event sequences, shared compatible preferences, suspended sessions, captions, dialog geometry, keyboard/mouse input, save choices and animation interruption remain covered.
- The total includes **972 new Vista animation assertions**, **53 immediate-input/checkpoint assertions** and **32 dialog-region reuse assertions**. No visible window or desktop input was used.
- The animation subset compares full ARGB frames at cache handoff for all four Vista decks. It also compares clipped/full rendering over 45 deal frames and 30 stock-flip/transfer frames for each Vista game and scale, repeats frozen intermediate frames, and checks felt-edge opacity. The deadline/cleanup gap failed before the fix.
- Immediate `Alt+G`, Down, Enter reproduced a division-by-zero exception before the fix. Menus and dialog acceptance now work without an intervening paint. Tests also cover Enter after focusing a FreeCell Options checkbox and clearing old effects on Spider checkpoint load.
- Corrupt-save probes exercise ten null nested fields. They verify that loading preserves the bytes and recovery keeps an unreadable copy before writing a fresh state. Existing background write ordering, reporting and flush checks also pass.

Logs: `artifacts/rules-audit-v091.log`, `ui-final-v091.log`, `render-audit-v091.log`, and `input-audit-v091.log`. Failed-before logs and the precise scope are listed in the [audit](AUDIT-0.9.1.md).

## Rendering and package parity

The tested source and final packaged EXE each completed **225 application views plus 36 motion samples**. All **261 PNG hashes match**. All **five exported embedded notices match** their source files; export exited 0.

- Source renders: `artifacts/source-final-v091-renders`.
- Package renders: `artifacts/package-final-v091-renders`.
- Native-size overview sheets for all 17 profiles: `artifacts/qa-v091/Klondike-overview.png`, `FreeCell-overview.png`, `Spider-overview.png`.
- Visual review covered those sheets, native 150% Vista Options and XP FreeCell Statistics, plus Vista motion samples. The 972 exact animation assertions provide the continuity checks that still images cannot establish.
- Notice export: `artifacts/notices-final-v091`.
- Machine-readable image/notice/package hashes: `artifacts/package-validation-v091.json`.

Matching source/package images verify packaging, not equality to 261 original Windows screenshots. The existing measured XP palette and historical presentation are retained. No original Microsoft executable was run during this audit.

## Performance

Three alternating runs of the retained 0.9.0 benchmark and new 0.9.1 benchmark used the same warm offscreen renderer at 150%. Each run records 120 frames per profile. The table reports the median of the three per-run medians, and the median of their p95 values. Heavy UI/render checks did not run concurrently with these measurements.

| Profile | 0.9.0 median ms | 0.9.1 median ms | 0.9.0 p95 ms | 0.9.1 p95 ms |
|---|---:|---:|---:|---:|
| 3.1 Solitaire | 1.88 | 1.77 | 2.34 | 2.30 |
| 95 Solitaire | 1.83 | 1.83 | 2.69 | 2.23 |
| 95 FreeCell | 2.03 | 2.05 | 2.55 | 2.54 |
| XP Solitaire | 0.84 | 0.84 | 1.01 | 0.99 |
| XP FreeCell | 0.86 | 0.83 | 1.02 | 1.19 |
| XP Spider | 1.03 | 1.13 | 1.24 | 1.69 |
| Vista Solitaire | 3.76 | 3.76 | 4.40 | 4.37 |
| Vista FreeCell | 3.70 | 3.61 | 4.15 | 4.51 |
| Vista Spider | 3.97 | 3.94 | 4.62 | 4.41 |

Vista's typical warm paint cost is effectively unchanged. XP Spider showed a small increase in these samples; this is not a universal speedup claim. Initial single runs had larger variance, so the alternating repetitions are reported instead. These warm costs include cached boards, not an exhaustive cost profile of every active card flight.

The updated production message-queue/frame-clock probe delivered **60.0 / 120.0 / 143.1 callbacks per second** for full redraws and **60.0 / 120.0 / 144.0** for partial redraws. The independent message-queue heartbeat remained responsive. These are **offscreen CPU costs and callback rates, not monitor FPS, display latency or a visible smoothness certification**.

Data: `artifacts/benchmark-comparison-v091.json`, `benchmark-pair-v090-{1,2,3}.json`, `benchmark-pair-v091-{1,2,3}.json`, and `benchmark-v091.json`. The rendering fix removes missing/jumping frames; it does not change the intended historical motion timing.

## Dependencies and remaining limits

The online NuGet vulnerability query completed successfully against `api.nuget.org` and returned no package findings. There are no third-party PackageReferences. Result: `artifacts/dependency-audit-v091.json`. Early test builds reused a cached NU1900 metadata warning; the final packaging build completed without that warning. This package query does not certify the embedded runtime or host Windows against every security issue.

The audit remains bounded by the available original-release evidence. Me Spider still uses preserved XP artwork. Vista finish references are RC1 build 5600, not verified RTM. Exact historical font bytes, desktop glass composition, complete original dialog/shortcut coverage, some scoring/focus rules, audio mixing and animation curves remain unverified. Vista negative-number Easter eggs and native Spider save-file compatibility are unsupported. The retained Settings button and isolated double-click behavior are deliberate user requirements.

Live activation/task switching, multi-monitor behavior, monitor-visible animation, audio output and a prolonged visible-session resource soak were not performed. These limits are explicit; local tests establish the recreation's behavior and package consistency, not complete historical identity.
