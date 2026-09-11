# Verification — Solitude 0.11.8

The six confirmed defects from the [program audit](PROGRAM-AUDIT-0.11.7.md) are addressed in the [fix report](PROGRAM-FIXES-0.11.8.md). Windows 7–11 remain deferred.

## Delivered package

`dist/Solitude.exe` is the single-file Windows x64 package, version **0.11.8.0**, **63,710,565 bytes**. `artifacts/release-v0118/Solitude.exe` is identical. The previous distribution is backed up, and the user's normal save file remained byte-for-byte unchanged during verification and delivery.

SHA-256: `C2D09B89F40C765AD5ECF76A5C31E9D7E2F0C9C1E20BEEC40C14C92E81952B36`

The package includes .NET and Windows Desktop runtime **10.0.11**. It ships no new package dependency. The successful NuGet advisory query reported no project package findings; that is not an audit of every embedded runtime component. The EXE remains unsigned. The [0.11.8 release](https://github.com/cazrl/Solitude/releases/tag/v0.11.8) distributes this verified package. Results below are local; hosted runs are reported separately in [GitHub Actions](https://github.com/cazrl/Solitude/actions).

## Fresh checks

- **53 engine verification groups** passed.
- **25,979 UI checks** passed on the final application assembly, including **1,295 new audit regression checks**.
- **384 native-window checks** passed, including 372 gameplay actions and the last-column/endgame sequence.
- **290 source/package PNGs matched byte-for-byte**, covering all historical/ORBIT profiles, menus, dialogs and motion samples. The final checked application assembly matches the assembly used to publish the EXE: SHA-256 `4BB952B300E4C021D41D2CDE0F8A2D80315108DECA39E6C15069095D81E76658`.
- Clock regression: **10.013 real seconds, 10 displayed seconds**. Animation timing uses a separate source.
- Independent foundation probes preserved the settled card in Vista Klondike, Vista FreeCell and ORBIT at 100/125/150/200%.
- Eight resource cycles ended at **33 GDI objects, 11 USER objects and 355 handles**; GDI/USER counts were stable after warm-up. Private memory ranged from 35.1 to 42.4 MB, and managed memory stayed around 0.8 MB. This bounded trial does not certify unlimited runtime.

All final application builds completed with zero warnings and zero errors. Hidden native handles were used; no game window was shown and no desktop input was sent. Visual review included the [18-profile contact sheet](images/program-fixes-v0118/all-profiles.png) and [Vista foundation frame](images/program-fixes-v0118/vista-foundation.png).

## Performance observations

Alternating cache-cold and cache-warm hint samples in the final build, with identical move choices and complete undo history retained:

| Undo entries | Cache-cold median | Warm median |
|---|---:|---:|
| 100 | 0.646 ms | 0.107 ms |
| 1,000 | 6.664 ms | 0.385 ms |
| 5,000 | 36.236 ms | 1.487 ms |

The cold case clears the cache before each sample; it is not a measurement of the previous EXE. A separate 5,000-move session saved and reloaded all 5,000 undo entries in a 10,087,914-byte file. Save took 259 ms and load 441 ms in that trial. Ordinary saves still run in the background.

Final full-suite offscreen victory paint costs, using the production renderer and 32-bit ARGB frames:

| Scale | Normal window median / p95 | Compact window median / p95 |
|---|---:|---:|
| 100% | 8.35 / 9.74 ms | 5.45 / 6.01 ms |
| 125% | 12.34 / 17.51 ms | 7.51 / 8.12 ms |
| 150% | 15.42 / 17.84 ms | 10.17 / 11.16 ms |
| 200% | 25.91 / 30.57 ms | 16.71 / 19.91 ms |

These are CPU paint costs, not physical display FPS. Large 200% victory frames still exceed a 60 FPS paint budget. The optimization removes redundant frames during the still final hold and bounds logo-only repainting; it does not claim a universal 60/120/144 FPS guarantee. Rendering experiments that did not demonstrate a reliable gain were discarded.

## Reproduction and evidence

Run `./tools/verify-program.ps1` with the .NET 10 SDK on Windows for the full gate. `-Focused` runs the new audit regressions alongside engine/native checks and package parity. The script keeps its EXE under `artifacts/verification/package` and does not overwrite `dist`.

Durable records: [delivery](audits/program-0.11.8/delivery.json), [observations](audits/program-0.11.8/observations.json). Local detailed evidence is under `artifacts/fixes-v0118/`: `final-verify.log`, `ui-final.log`, `native-final.log`, `final-assembly-parity.json`, `final/verification.json`, `final/source-render/`, `final/package-render/`, `probes.log`, `probes/selected-results.json`, `dependency-audit.json` and `personal-save-check.json`.

The [diagnostic harness](../tools/ProgramAudit/README.md) supports separate output folders and the cold/warm hint probe. The earlier verification is preserved in [0.11.7](VERIFICATION-0.11.7.md).

Exact original-OS comparison, physical mixed-DPI moves, Narrator/audio operation, clean-machine launch and extended ordinary play remain unverified. The earlier 0.11.5 crash still lacks a confirmed managed stack/root cause. Historical asset rights and signing remain separate distribution questions.
