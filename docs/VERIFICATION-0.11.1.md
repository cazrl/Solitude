# Verification — Solitude 0.11.1

This patch tightens and centers ORBIT's playing grid. The [spacing report](ORBIT-SPACING-0.11.1.md) records reference measurements, implementation scope and visual checks.

**Passed:** 14,822 ORBIT checks, including twelve viewport/scale combinations for spacing, centering, card visibility, scroll targets and draw-three clearance. Existing animation, input, persistence and audit regressions are included in that total. Historical rules were not changed or rerun for this layout patch; see the [0.11.0 baseline](VERIFICATION-0.11.0.md).

The single-file Windows x64 build is version **0.11.1.0**, **63,676,261 bytes**, at `dist/Solitude.exe`, with an identical retained copy at `artifacts/release-v0111/Solitude.exe`. The previous distributed EXE was backed up.

SHA-256: `3AD5E15DC7F4E3675BE1DD2B188FCB812578171E008CA40A6709B2757C27EBBD`

All **18 source/package ORBIT PNGs match byte-for-byte**. Evidence: `artifacts/source-v0111` and `artifacts/package-v0111`. The reported 1680 × 1080 viewport, minimum window and ultrawide layout were visually inspected using production-renderer images under `artifacts/orbit-spacing`.

Logs: `artifacts/orbit-spacing-ui.log`, `artifacts/build-v0111.log`. Tests and renders were offscreen with isolated data. No new physical-display FPS, audio or live pointer-play claims are made. The test build reported an unavailable NuGet vulnerability feed (NU1900); compilation and tests completed, and the release build succeeded.

This is a local package; the public GitHub release remains 0.10.1.
