# Verification — Solitude 0.9.0

Built locally on 9 September 2026. This is the correction build following the complete 0.8.1 fidelity audit. The [fix report](FIDELITY-FIXES-0.9.0.md) maps every finding to implementation, tests and remaining historical limits. The previous report is preserved as [VERIFICATION-0.8.1.md](VERIFICATION-0.8.1.md).

## Delivered executable

| Item | Verified result |
|---|---|
| Normal launch path | `dist/Solitude.exe` |
| Retained release | `artifacts/release-v090/Solitude.exe` |
| File version | **0.9.0.0** at both paths |
| Size | **63,598,947 bytes** |
| SHA-256 at both paths | `05C5FC036B08FC5BD44A9FC12FA25B6D4383AA75821861E5B1B22BB68D11A3AF` |
| Package contents | Exactly one self-contained Windows x64 EXE in each release directory |
| Previous executable | Preserved by atomic replacement under `artifacts/Solitude-before-*.exe`; 0.8.1 also retained separately |

`build.ps1 -SkipTests` packaged the source after the rules/UI checks below; its log is `artifacts/build-final-v090.log`. The script verifies equal hashes. The previous mismatch between the normal 0.8.0 launch path and the retained 0.8.1 build is resolved.

No existing game process was terminated. No visible game window was launched, desktop input sent, original game binary executed, personal save accessed, installer run, or release uploaded. Test forms and render sessions used ephemeral state or isolated workspace directories. Self-contained runtime extraction still uses the Windows temporary directory. A clean-machine portability test was not performed.

## Rules, state and production UI

- **50 rules/persistence verification groups passed; 0 failed.** Log: `artifacts/rules-final-v090.log`. Includes card conservation over 60,000 rule-driven actions and existing deal, score, move, Undo and save validation.
- **4,063 production UI checks passed.** Log: `artifacts/ui-final-v090.log`. Covers all 17 profiles, 100/125/150/200%, full double-click sequences, shared compatible settings, motion interruption/endpoints, outline/drag interaction, keyboard input, and partial/full repaint equivalence.
- The new fidelity subset contains **2,338 checks**, included in the total above: `artifacts/fidelity-final-v090.log`. It exercises repeated foundation rearrangements, all-profile save-disabled suspension versus disk persistence, classic FreeCell inspection/one-move/loss/statistics, classic negative deals, original Options geometry/access keys, Vista sound/tips/save/resume behavior, owned dialog input, more than 200 Undo states, and win effects/results.
- Classic FreeCell game -1 is exercised through four legal moves into a loss. The independent general zero/one-move fixtures are structurally valid; ordinary numbered-deal reachability is not claimed for those artificial fixtures.
- Mnemonic labels are checked against labels without an underline in all eras and scales. Glyphs must remain unchanged above the underline; this catches the clipped Vista Exit label discovered during visual review.
- Owned dialog tests move offscreen forms outside their parent bounds and exercise mouse routing. They do not establish visible activation, task switching, DWM composition or multi-monitor behavior.
- Embedded WAV headers and sound event/option gates passed. Speaker output, latency, overlap and original audio mixing were not tested.

## Rendering and package checks

The tested source executable and final packaged EXE each produced **225 application views plus 36 motion samples**. All **261 PNG SHA-256 hashes match** between the runs. Both completed their render manifest; the generated images cover every enabled profile and all four scales.

- Source renders: `artifacts/source-final-v090-renders`.
- Delivered EXE renders: `artifacts/package-final-v090-renders`.
- All 17 board overviews were visually reviewed in `artifacts/final-v090-Klondike-overview.png`, `final-v090-FreeCell-overview.png` and `final-v090-Spider-overview.png`.
- Options contact sheets for all games were reviewed, along with final native-size 3.1 Options, XP FreeCell Statistics and Vista Options. New celebration frames and the final Vista Game Won dialog were inspected in `artifacts/fidelity-celebrations`.
- The final Vista win fixture displays base score 474, awarded bonus 3,780 and total 4,254 consistently; the Exit caption is complete. This demonstrates presentation consistency, not certification of every original Vista scoring rule.
- Notice export exited **0**. All **five embedded notice files match** their sources in `artifacts/notices-final-v090`.
- Machine-readable package/image/notice hashes: `artifacts/package-validation-v090.json`.

The matching images prove that packaging preserves the tested rendering. They are not comparisons against 261 original Windows captures. The earlier measured XP palette corrections remain; complete frame identity is not certified.

## Performance diagnostics

Warm offscreen CPU paint costs at 150%:

| Profile | Median ms | p95 ms |
|---|---:|---:|
| 3.1 Solitaire | 2.08 | 2.80 |
| 95 Solitaire | 2.52 | 3.79 |
| 95 FreeCell | 2.73 | 3.57 |
| XP Solitaire | 0.93 | 1.44 |
| XP FreeCell | 0.93 | 1.57 |
| XP Spider | 1.25 | 2.00 |
| Vista Solitaire | 3.86 | 4.54 |
| Vista FreeCell | 4.30 | 5.11 |
| Vista Spider | 5.25 | 6.99 |

The production message-queue/frame-clock probe delivered 60 and 120 callbacks/second at those targets; the 144 target delivered 142.6 for full redraws and 144.0 for partial redraws. It did not starve the independent message-queue heartbeat. These measurements ran alongside offscreen UI validation and are diagnostic, not controlled before/after measurements.

Logs/data: `artifacts/benchmark-final-v090.log` and `artifacts/benchmark-final-v090.json`. These are CPU costs and offscreen callback rates, **not monitor FPS, input latency or a live smoothness test**. Completion choreography is reconstructed from the cited references, not calibrated by this benchmark.

## Remaining verification boundaries

Vista completion references are **RC1 build 5600**, not verified RTM footage. Me Spider resource identity, original fonts for later presets, glass composition, complete dialog/accelerator coverage, some scoring/timer details, event mixing and exact animation curves still need identified original-release baselines. Native Spider file compatibility and Vista negative-number Easter eggs remain unsupported. The retained Settings control and isolated double-click are deliberate user requirements.

Publish and test builds emitted **NU1900** because the online NuGet vulnerability metadata feed was unavailable. Compilation and tests passed; the online dependency vulnerability scan did not complete. The artifact is a locally verified recreation, not a claim that every historical behavior or release condition has been certified.
