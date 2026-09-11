# Solitude 0.11.7 — complete program audit

11 September 2026. **The program is substantially working, but it is not ready for a clean sign-off.** This audit reproduced six functional or accessibility problems, measured a significant ORBIT rendering limit at large sizes, and identified remaining historical-fidelity and release-verification work. The highest priority is preventing two open instances from silently overwriting each other's saves.

The audit covered all **17 historical edition/game combinations plus ORBIT**, from Windows 3.0 through Vista. Windows 7–11 remain outside the agreed scope. It added an independent diagnostic harness and this report; production source, existing tests and the delivered EXE were preserved. No visible game window was shown and no user desktop input was sent. Test saves used isolated artifact directories.

**Build examined**

| Item | Verified result |
|---|---|
| Delivered file | `dist/Solitude.exe` |
| Version / size | 0.11.7.0 / 63,702,373 bytes |
| SHA-256 | `6A813F532D73EF18BE161D872AD858179B48487CA91F8BC13830E6A189D63B85` |
| Source preservation | All 252 files in the pre-audit source/assets/test hash inventory unchanged |
| Package render parity | 290 source PNGs and 290 EXE PNGs, no differences or missing files |
| Signature | Not signed |
| Public release | GitHub API confirmed v0.10.1, published 10 September 2026; its EXE is 63,646,565 bytes |

The local package and [public download](https://github.com/cazrl/Solitude/releases/tag/v0.10.1) are different versions. This audit did not publish anything.

**Confirmed findings, in recommended repair order**

P1 means a high-priority risk to user data. P2 means a behavior or usability defect to fix. Findings distinguish an observed failure from an implementation recommendation; the proposed fixes below have not been implemented.

| ID | Priority | Affected area | Observed problem |
|---|---|---|---|
| A01 | P1 | Saving, all editions | A stale second instance overwrites newer progress and preferences without warning |
| A02 | P2 | 8 historical Klondike and 3 Spider profiles | Options can display shared next-deal rules instead of the current deal's rules; selecting those displayed rules does not apply them |
| A03 | P2 | Vista Klondike and FreeCell | The settled foundation card disappears while the next card flies onto it |
| A04 | P2 | All 17 historical profiles | 200% windows can extend outside a small monitor's work area |
| A05 | P2 | All 17 historical profiles | Custom cards and controls are not exposed through the accessible tree |
| A06 | P2 | ORBIT victory | Turning animations off through the keyboard-accessible Options dialog leaves the win animation running |

**A01 — Two open instances can silently lose progress**

Reproduction: create two independent game windows with separate `Store` objects pointing at the same isolated directory. Both start from the same deal. Instance A makes two draws, changes its palette and saves. Instance B, which still has the original snapshot, makes one draw and saves.

Observed: both saves return success and both stores report no warning. Reloading gives **one move instead of two**, and palette **0 instead of 2**. This demonstrates a stale overwrite without needing simultaneous writes. Both saves contain a complete application snapshot, so other suspended deals and records can also be replaced by stale values.

Cause: [Program.cs](../src/Program.cs#L5) has no shared-data-directory instance guard. [Store.Save](../src/Preferences.cs#L192) replaces the whole JSON file with no revision conflict check. [SaveCoordinator](../src/SaveCoordinator.cs#L4) coordinates writes within one instance only. The shared `.tmp` name creates an additional possible simultaneous-write race; that separate race was not required or reproduced here.

Repair: either allow only one active instance per canonical save directory and activate it on subsequent launches, or implement cross-process locking plus explicit revision-conflict handling. File locking alone does not prevent a later stale snapshot from overwriting a newer one. Preserve independent `--data-dir` instances.

Acceptance: open two instances, change different settings/deals, save and close in both orders; no newer progress disappears, and conflicts cannot report silent success.

**A02 — Shared settings and active deal rules disagree in historical Options**

Reproduction for Klondike: start a historical draw-three deal, switch to ORBIT, select draw one, then return to the historical deal. Open its Options dialog and choose Draw one / OK. The historical deal correctly retains its old rules when merely switching editions, but the Options dialog displays draw one and applying it still draws **three cards**. This reproduced in all eight historical Klondike presets.

For Spider, change shared difficulty to four suits in another preset, return to an existing one-suit deal, open Difficulty/Options and select four suits again. All three Spider presets display four suits while the active deal remains **one suit** after applying.

Cause: [OpenDialog](../src/GameWindow.cs#L238) copies active rules into the draft only for ORBIT. [ApplyOptions](../src/GameWindow.Dialogs.cs#L190) compares the draft against active rules only for ORBIT; historical modes compare against shared preferences instead. Therefore a displayed shared value looks unchanged even when it differs from the running deal.

Repair: consistently distinguish current-deal rules from shared defaults. Historical game Options should display the current rules and intentionally start a new deal when the user changes them. Applying appearance-only changes must preserve the deal and shared next-deal defaults. Keep the requested cross-preset sharing and suspended deals.

Acceptance: cover every compatible setting, both directions between presets, save/reopen, appearance-only Apply, Cancel and Restart. The controls and actual deal must agree, with no surprise replacement of an existing deal.

**A03 — Vista still hides the foundation under an incoming card**

Reproduction: put A♣ on a foundation and legally move 2♣ onto it. Sample the production renderer 20 ms into the flight. In Vista Klondike and FreeCell, a previously white pixel inside the Ace changes to table green and the Ace is visibly absent. Reproduced at 100%, 125%, 150% and 200%: **8 failing cases**. The equivalent ORBIT control preserves the Ace at all four scales.

Cause: the [Klondike foundation loop](../src/GameWindow.Paint.cs#L134) and [FreeCell cell painter](../src/GameWindow.Variants.cs#L171) try to draw only the new top card. [DrawGameCard](../src/GameWindow.Motion.cs#L150) correctly leaves that card to the animation layer, but these painters do not draw the settled card below it. ORBIT already searches underneath active flights.

Repair: render the highest settled card beneath every incoming foundation flight. Share that rule between the relevant painters and invalidate the cached board correctly as flights end or reverse.

Acceptance: pixel and motion tests for a 2 onto an Ace, higher ranks, consecutive foundation moves, Undo during motion and drag/drop at all four scales. The moving card remains separate and the underlying pile never vanishes.

Evidence images use controlled legal states: [Vista, Ace missing](images/program-audit-v0117/vista-foundation.png) and [ORBIT, Ace retained](images/program-audit-v0117/orbit-foundation.png).

**A04 — Historical windows can be centered partly off-screen**

The production centering routine was supplied a synthetic **1366 × 728** work area. At 200%, all 17 historical profiles exceed it: the minimum height is **860**, placing the title bar at **y = −66**. Spider additionally becomes **1560 pixels wide**, starting at **x = −97**. At 150% the tested profiles fit; ORBIT fits at both sizes.

Cause: [EditionWindowMetrics](../src/GameWindow.cs#L149) allows historical minimum dimensions to exceed the work area, then centers the oversized rectangle. ORBIT alone reduces its effective scale to fit.

Repair: fit historical effective scale and minimum dimensions to the selected monitor before centering, while retaining the user's preferred display size for larger monitors. Test switching both ways, startup, restore and dragging between monitors. Clamping only the top-left position would still leave controls inaccessible.

This is a reproduced geometry defect, not a live test on a physical 1366-pixel monitor or every mixed-DPI configuration.

**A05 — Historical modes lack accessible card and control information**

The painted board and Settings dialog return **−1 / no custom children** from the accessible root for all 17 historical profiles. ORBIT returns 29 board items and 19 Settings items in the tested initial states. Historical cards and dialogs are painted rather than native child controls, so falling back to the base form tree does not expose them.

Cause: [OrbitAccessibleRoot.Active](../src/GameWindow.Accessibility.cs#L82) disables the custom accessible tree outside ORBIT. This is a confirmed application-tree gap; Narrator speech and voice-control behavior were not tested live.

Repair: expose historical controls, piles and face-up cards with names, roles, states, focus and actions, without revealing hidden card identities. This can preserve the period visuals. Extend the tree to owned historical dialog surfaces as well. Microsoft's [WinForms accessibility guidance](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/provide-accessibility-information) explains the properties used by accessibility aids.

Acceptance: navigate and play each game using keyboard and Narrator; change edition, operate dialogs, hear selection/state changes and verify hidden cards remain private.

**A06 — Mid-victory reduced-motion changes do not stop the effect**

Reproduction: start ORBIT's card victory, press F5, turn off spatial animations and Apply. Observed: the option is false, but `showingVictory` and `NeedsFrames` remain true and cards continue animating. Mouse buttons are disabled during this state, while the keyboard shortcut still opens Options.

Cause: [ProcessCmdKey](../src/GameWindow.cs#L412) handles the Options shortcut without a victory-state gate. [StartFutureVictory](../src/GameWindow.OrbitVictory.cs#L16) checks the motion preference at entry, but the ongoing [Animate](../src/GameWindow.cs#L555) path continues until its time limit.

Repair: keep keyboard and mouse state policies consistent and finish the current effect immediately when motion is disabled, preserving the won board and recording the result once. Options must not be unexpectedly replaced by an animation-completion dialog while the user is editing them.

Acceptance: keyboard Options/Settings, disable motion, Escape, focus changes and results handoff during each phase of the victory. No animation remains active after opting out.

**Rendering performance needs further work**

These measurements are warmed production rendering into offscreen bitmaps on this machine. They measure CPU work and callback delivery, **not physical monitor FPS**, and individual timings vary with system load.

| ORBIT display size | Opening deal median | Opening deal p95 | Settled-board median |
|---|---:|---:|---:|
| 100%, 1120 × 720 | 3.68 ms | 5.01 ms | 1.62 ms |
| 125%, 1400 × 900 | 6.67 ms | 12.03 ms | 5.81 ms |
| 150%, 1680 × 1080 | 12.21 ms | 15.90 ms | 5.07 ms |
| 200%, 2240 × 1392 | 19.25 ms | 30.57 ms | 9.77 ms |

A 60 FPS frame has about 16.67 ms available; 120 FPS has 8.33 ms and 144 FPS has 6.94 ms. The 200% deal cannot reliably fit even the 60 FPS budget in this run. The 125% settled sample was slower than 150%, illustrating why these observations should not be treated as a hardware-independent scaling formula.

The separate victory phase profiler averaged **24.22–43.17 ms per frame at 200% with atmosphere enabled**, versus 6.04–7.77 ms at 100%. It samples four phases, 30 draws per phase into a premultiplied bitmap; these are phase averages, not p95 values and not directly interchangeable with the deal benchmark. Disabling atmosphere did not consistently remove the expensive work.

The real message-queue pacing probe at 150% delivered approximately **60.0 / 119.0 / 102.4 callbacks per second** for ORBIT targets 60 / 120 / 144. Vista Spider partial redraw delivered approximately 60 / 120 / 144; its full redraw became slower at the higher targets. UI heartbeat timers continued to run in every probe.

Recommendation: profile the large moving-card transforms and repeated full-window compositing first; retain cached static scenery and limit redraw damage where practical. If 120/144 FPS at large sizes remains a goal, evaluate an accelerated compositor against measured frame budgets. Retain the full card choreography and reduced-motion controls. Verify the result on the actual monitor and compositor before claiming a smoothness target.

**What passed, and what that proves**

| Area | Fresh evidence | Scope of conclusion |
|---|---|---|
| Rules and board invariants | 53 verification groups, 0 failed | Deals, legal moves, scoring, stock/recycle limits, undo, winning and card preservation; includes 60,000 variant actions and all 17 historical policies |
| UI and state transitions | 24,684 existing UI checks passed | Historical frames, typography, hit tests, dialogs, switches, saved sessions, motion, ORBIT interactions and both morph directions under the scenarios tested |
| Native endgame path | 384 checks, including 372 gameplay actions | Hidden native handles, real window messages, accessibility callbacks and repeated last-column sequences completed without a crash |
| Timekeeping | 10.008 wall-clock seconds; 10 displayed seconds | The real-time source and clock update routine agree; callback-rate and pause/resume regressions also pass |
| Source versus delivered EXE | 290 identical PNG pairs | 237 normal/dialog/frame views plus 53 motion/victory samples match; this is parity, not proof of historical authenticity |
| Visual review | Contact sheet of all 18 boards, focused foundation frames and targeted Options images | Overall edition separation and the reproduced defects were inspected; every state was not manually pixel-compared with an original OS |
| Save integrity within one instance | Existing recovery/migration/background-save groups passed | Backup recovery, malformed nested data, immutable snapshots, flush and reported write failures work in the covered cases; A01 remains outside that protection |
| Resource stability | 8 cycles through 9 edition skins and 3 dialogs per skin | GDI handles stayed 33, USER handles 11, process handles 402; private memory ended below its first sample; no persistent leak observed in this bounded soak |
| Long-session persistence | 100 / 1,000 / 5,000 draws saved and reloaded with matching move counts | A 5,000-history snapshot used 10,087,914 bytes; save 122.9 ms, load 305.7 ms; no failure in this synthetic stress case |
| Dependency audit | NuGet vulnerability command completed after refreshing network access | No vulnerable package entries returned for the application project, which declares no external PackageReference; this is not a security certification of embedded assets or the bundled runtime |

Passing check counts must not be read as a count of independently verified real-world scenarios. A02 and A03 specifically demonstrate gaps in the existing checks.

At 5,000 undo entries, ORBIT Hint took 12.06 ms median and 19.74 ms maximum in nine samples; at 100 entries the median was 0.42 ms. [OrbitHints](../src/Game.Orbit.cs#L20) rebuilds board keys from the entire history each time. Incremental history-key caching is a useful lower-priority improvement if long games become common. Preserve unlimited undo and save compatibility; do not silently discard history to improve a benchmark.

**Historical fidelity and the earlier crash remain open verification work**

The code contains real era-specific rule and presentation differences. The catalog contains the expected scoped combinations: 8 historical Klondike, 6 FreeCell and 3 Spider profiles, plus ORBIT Klondike. Shared back art between several early releases is not itself evidence of a defect. Windows 98 Plus! Spider is intentionally excluded from the base-Windows list.

Current renders and existing reference/resource measurements do **not** establish one-to-one behavior for every original release. No original Windows game executable was run during this audit. The [mechanics record](MECHANICS-AUDIT.md), [frame measurements](FRAME-ALIGNMENT.md), [resource provenance](ASSET-SOURCES.md) and earlier audit/fix records remain useful evidence, but this report does not reinstate old defects that later fixes already addressed. In particular, some Vista animation reference material is from a prerelease build rather than a verified matching retail installation.

For an exactness claim, build a controlled comparison matrix using the named original release/build, language, theme and DPI, then compare menus, dialog geometry, card spacing, font rasterization, drag and double-click behavior, scoring, sounds and win/loss flow. Capture the same deal and action sequence on both sides. The requested selector, cross-preset settings, suspended deals and deliberate ORBIT interactions are product choices, not historical defects.

The Windows Application event log contains **two reports from the same 0.11.5 process at 02:42 on 11 September**: exception codes `0xe0434352` and `0xc000041d`. They do not identify the failing managed method. The current default data directory has no `Solitude-error.txt`; the fresh 0.11.7 native sequence did not crash. Therefore the user's original last-column crash has **no confirmed root cause**, and these results do not prove that every variant of it is fixed. Retain the existing diagnostics and use the managed stack/context if it recurs.

Still requiring live checks: extended ordinary play; original-OS comparisons; actual monitor/compositor frame delivery; Narrator and voice-control operation; audible sound balance/latency; physical mixed-DPI monitor moves and sleep/resume; clean-machine first launch and native extraction; abrupt power loss or a blocked filesystem during a save. These are explicit limits, not silent passes.

**Release and maintenance improvements**

- Keep local build, public version, checksums and release notes aligned when a release is intentionally published. The currently verified public version is older than this audit target.
- Add repeatable CI/build gates for the rule suite, input/transition checks, independent regressions from A01–A06 and package parity. No `.github` workflow directory exists in this checkout. Pin performance expectations to controlled hardware rather than failing builds on noisy timing samples.
- Consider code signing for distribution. The current file is unsigned; that observation does not imply the file is malicious.
- Resolve historical asset redistribution provenance before treating it as cleared. The existing [third-party notices](../THIRD_PARTY_NOTICES.md) explicitly state that preservation-project code licenses do not establish rights to the underlying Microsoft artwork. This audit did not perform legal clearance. ORBIT's procedural assets are separately documented.
- Keep the bundled runtime updated and verify its release contents. A successful project NuGet query does not cover every embedded runtime component or constitute a clean-machine/security audit.
- After behavior fixes, remove obsolete ORBIT scroll plumbing and stale test/document labels, and centralize shared pile-underlay and active-rule handling. The currently empty scroll controls are maintenance debt, not evidence that visible scrolling has returned.

**Evidence and reproduction**

The durable [observations JSON](audits/program-0.11.7/observations.json) contains the independent reproductions, package comparison summary and current performance measurements. [All-profile contact sheet](images/program-audit-v0117/all-profiles.png). Harness: [tools/ProgramAudit](../tools/ProgramAudit/README.md).

Full local logs and rendered frames are under `artifacts/audit-program-v0117/`: `engine.log`, `ui.log`, `native.log`, `probes.log`, `probes/results.json`, `long-session.log`, `probes/selected-results.json`, `historical-performance.json`, `orbit-performance.json`, `victory-performance.log`, `render-parity.json`, `source-render/`, `package-render/`, `dependency-audit.json`, `public-release.json`, `solitude-crash-events.json` and `preservation.json`.

Commands used after building the isolated outputs:

```powershell
dotnet run --project tests/Solitude.Tests.csproj -c Release --no-restore
artifacts/audit-program-v0117/ui-build/Solitude.UiChecks.exe
artifacts/audit-program-v0117/ui-build/Solitude.UiChecks.exe --orbit-native-only
dotnet run --project tools/ProgramAudit/ProgramAudit.csproj -c Release
artifacts/audit-program-v0117/benchmark-build/Solitude.Benchmarks.exe artifacts/audit-program-v0117/historical-performance.json --pacing
artifacts/audit-program-v0117/benchmark-build/Solitude.Benchmarks.exe artifacts/audit-program-v0117/orbit-performance.json --orbit-only
artifacts/audit-program-v0117/ui-build/Solitude.UiChecks.exe --orbit-motion-profile
dotnet list src/Solitude.csproj package --vulnerable --include-transitive --format json
```

Both source and package renders used `--render <isolated-output> --data-dir <isolated-data>` and completed with exit code 0. Test/build access failures under the restricted filesystem were resolved by rerunning the same isolated builds with compiler-output access. The initial cached NU1900 advisory warning was followed by a successful fresh dependency query. Neither is recorded as a product defect.
