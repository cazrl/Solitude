# Verification — Solitude 0.11.4

This release fixes startup and bidirectional edition centering, adds the animated ORBIT transition, and fixes a renderer/game mismatch when switching to FreeCell or Spider. See the [implementation report](ORBIT-MORPH-0.11.4.md).

**Passed:** 15,788 ORBIT checks, including all historical games in startup/round-trip positioning, animation bounds and frame continuity, normal and maximized source windows, input isolation, Escape, focus loss and reduced motion. The final real-time check measured 10.013 wall-clock seconds and 10 displayed game seconds.

The single-file Windows x64 build is version **0.11.4.0**, **63,684,453 bytes**, at `dist/Solitude.exe`, with an identical retained copy at `artifacts/release-v0114/Solitude.exe`. Previous distributed EXEs were backed up.

SHA-256: `E538B1853DAD6A13E307FDB4A186918E11AB766B76DC4BE4F45D37BD94B1BBE2`

All **18 source/package ORBIT PNGs match byte-for-byte**. Evidence: `artifacts/source-v0114` and `artifacts/package-v0114`. Transition frames are under `artifacts/orbit-morph`.

Logs: `artifacts/orbit-morph-final-ui.log`, `artifacts/edition-placement-ui.log`, `artifacts/build-v0114.log`. The test build reported unavailable NuGet vulnerability data (NU1900); compilation and tests completed, and the release build succeeded.

The old clipped window's actual desktop origin was verified before the change. Closing it was blocked by automatic approval review to protect the current deal. A separate development copy was launched; the user then stopped Computer Use with Escape before corrected live bounds could be inspected. No further desktop automation followed. Final physical startup/morph, display smoothness and multi-monitor behavior remain unverified; lifecycle tests and inspected production-renderer frames cover those paths offscreen.

This is a local package; the public GitHub release remains 0.10.1. Earlier verification: [0.11.2](VERIFICATION-0.11.2.md), [0.11.1](VERIFICATION-0.11.1.md), [0.11.0](VERIFICATION-0.11.0.md).
