# Verification — Solitude 0.11.5

This release adds the animated corner logo menu, reverse ORBIT edition morph, and ORBIT double-click placement onto legal tableau columns. See the [update report](ORBIT-UPDATE-0.11.5.md).

**21,682 UI checks passed**, covering historical and ORBIT behavior, persistence, rendering, accessibility, edition placement and transitions. Logo checks cover hover/open motion, menu positioning, click toggling, keyboard access, idle scheduling and reduced motion. Double-click checks cover Ace through King, foundation priority, waste/tableau/sequence transfers, Undo, illegal targets and historical isolation. Reverse morph checks cover every historical game at four display scales.

The engine suite also passed **53 verification groups**, including card preservation through 60,000 rule-driven actions and all 17 historical era/game policies. Its log is `artifacts/orbit-v0115-engine.log`.

The real-time clock check measured **10.000 wall-clock seconds and 10 displayed game seconds**. Tests used isolated offscreen windows and input handlers; no desktop input was sent. Physical display smoothness and live multi-monitor behavior remain unverified.

The single-file Windows x64 build is **0.11.5.0**, **63,687,013 bytes**, at `dist/Solitude.exe`, with an identical retained copy at `artifacts/release-v0115/Solitude.exe`. Previous distributed EXEs were backed up.

SHA-256: `14AAE162B9D95A38BEEF64606A4AD7F12CD4900D1C405168FDEB118427E575C9`

All **18 source/package ORBIT PNGs match byte-for-byte**. Evidence: `artifacts/source-v0115` and `artifacts/package-v0115`. Inspected logo states are in `artifacts/orbit-logo`; forward and reverse transition frames are in `artifacts/orbit-morph`.

Logs: `artifacts/orbit-v0115-full-ui.log`, `artifacts/orbit-logo-ui.log`, `artifacts/orbit-auto-place-ui.log`, `artifacts/reverse-morph-ui.log`, and `artifacts/build-v0115.log`. The UI test build reported unavailable NuGet vulnerability data (NU1900); compilation and tests succeeded. The release build succeeded.

This is a local package. The public GitHub release remains 0.10.1. Earlier verification: [0.11.4](VERIFICATION-0.11.4.md), [0.11.2](VERIFICATION-0.11.2.md), [0.11.1](VERIFICATION-0.11.1.md), [0.11.0](VERIFICATION-0.11.0.md).
