# Verification — Solitude 0.11.2

This patch centers the window when ORBIT is selected and separates gameplay timing from animation timing. See the [implementation and evidence](ORBIT-FIXES-0.11.2.md).

**Passed:** 14,888 ORBIT checks, including centering through the Settings UI at four scales and gameplay clock tests across XP, Vista and ORBIT. The final independent clock comparison measured 10.009 wall-clock seconds and 10 displayed game seconds. Continuous acceleration was not reproduced locally; the original animation stopwatch also measured normally.

The single-file Windows x64 build is version **0.11.2.0**, **63,676,773 bytes**, at `dist/Solitude.exe`, with an identical retained copy at `artifacts/release-v0112/Solitude.exe`. The previous distributed EXE was backed up.

SHA-256: `69C02D2086566E92B8C3B822655E248CC4197E5311FD6D750FD97BA28037EF91`

All **18 source/package ORBIT PNGs match byte-for-byte**. Evidence: `artifacts/source-v0112` and `artifacts/package-v0112`.

Logs: `artifacts/orbit-center-final-ui.log`, `artifacts/orbit-clock-ui.log`, `artifacts/build-v0112.log`. Tests used isolated data and offscreen forms; the real-time test sampled actual clocks. Physical multi-monitor switching and the user's originally reported timing behavior were not observed. The test build reported unavailable NuGet vulnerability data (NU1900); compilation and tests completed, and the release build succeeded.

This is a local package; the public GitHub release remains 0.10.1. Previous verification: [0.11.1](VERIFICATION-0.11.1.md), [0.11.0](VERIFICATION-0.11.0.md).
