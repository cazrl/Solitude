# Verification — Solitude 0.11.6

The release corrects ORBIT's corner rim, makes the logo button flush with the window corner, centers button glyphs, preserves settled foundation cards beneath moving cards, and fits full columns without scrolling. It also adds diagnostic logging and guarded recovery for a first recoverable ORBIT UI exception. The original reported crash was not reproduced and its root cause is not confirmed fixed. See the [polish and crash report](ORBIT-POLISH-0.11.6.md).

The full historical/ORBIT suite passed **21,931 UI checks**. After the final primary-button alignment and completion-dock adjustments, **264 focused checks** and **384 native-window checks** passed. The latter include 12 last-reveal repetitions and 372 further gameplay actions with accessibility callbacks and animated frames. The engine suite passed **53 verification groups**. The clock check measured **10.006 wall-clock seconds and 10 displayed game seconds**.

Tests cover pixel centering at all four scales, native-region corner composites, the Ace under an arriving Two, the last column move through completion and victory, all face-up cards in a long fitted column, and valid-deal preservation after an injected UI failure. No user desktop input was sent; test windows were hidden. Physical display smoothness and end-to-end screen-reader behavior remain unverified.

The single-file Windows x64 package is **0.11.6.0**, **63,692,133 bytes**, at `dist/Solitude.exe`, with an identical retained copy at `artifacts/release-v0116/Solitude.exe`. Previous EXEs were backed up.

SHA-256: `FDF4017023C0E92F77D47B0E28CC13FB8DF796A6637589B5C09CB6C8EBDE2314`

All **18 source/package ORBIT PNGs match byte-for-byte**: `artifacts/source-v0116` and `artifacts/package-v0116`. Inspected normal/minimum/wide views and magnified native corner composites are under `artifacts/orbit-polish`.

Logs: `artifacts/ui-v0116.log`, `artifacts/orbit-polish-final.log`, `artifacts/orbit-native-final.log`, `artifacts/engine-v0116.log`, and `artifacts/build-v0116.log`. NuGet vulnerability data was unavailable during UI test builds (NU1900); compilation and tests completed successfully. The release build succeeded.

This is a local package. The public GitHub release remains 0.10.1. Earlier verification: [0.11.5](VERIFICATION-0.11.5.md), [0.11.4](VERIFICATION-0.11.4.md), [0.11.2](VERIFICATION-0.11.2.md).
