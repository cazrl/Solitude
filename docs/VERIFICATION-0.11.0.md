# Verification — Solitude 0.11.0

The [ORBIT implementation report](ORBIT-FIXES-0.11.0.md) maps all twelve audit items to changes, reproductions and remaining validation limits.

**Passed:** 19,695 full UI checks; 53 shared rules/persistence groups; a final 13,881-check focused run covering audit regressions, adaptive scheduling and Spider snapshot isolation. The focused checks overlap the full suite and are not an additional independent total. The final About-version text correction was followed by a new package build and source/package render comparison.

The single-file Windows x64 build is version **0.11.0.0**, **63,676,261 bytes**, at `dist/Solitude.exe`, with a matching retained copy at `artifacts/release-v0110/Solitude.exe`. The previous distributed EXE was backed up.

SHA-256: `A6B94114DA502E98749145EEFD4E0CEE4450F241AC349F074C22DBE27048E45D`

All **18 source/package ORBIT PNGs match byte-for-byte**. Evidence: `artifacts/source-v0110` and `artifacts/package-v0110`.

Logs: `artifacts/orbit-fix-full-ui.log`, `artifacts/orbit-fix-final-regressions.log`, `artifacts/orbit-fix-rules.log`, `artifacts/build-v0110.log`. Benchmark data and screenshots are retained under `docs/audits/orbit-0.11.0`.

All game tests and renders were offscreen with isolated data. Physical display FPS, audible output, end-to-end Narrator operation and physical multi-monitor behavior remain unverified. Performance numbers are CPU measurements with machine-load variation, not a smoothness guarantee.

Earlier verification: [0.10.1](VERIFICATION-0.10.1.md), [0.10.0](VERIFICATION-0.10.0.md).
