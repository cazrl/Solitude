# Verification — Solitude 0.10.1

This patch gives ORBIT's face-up cards a contrasting slate outline and increases the draw-three fan spacing in proportion to card size. The exposed strip now accommodates two-digit ranks and corner suits. Animation destinations, clickable positions and hint outlines use the same spacing. Historical card art and spacing are unchanged.

**689 ORBIT checks passed**, including 12 new checks for visible overlap edges, rank/suit clearance and matching waste guidance at 100/125/150/200%. The previous 677 checks cover motion, persistence, nested settings, reduced motion and wins. Visual review of the 150% draw-three pile confirms separate outlines. Tests were offscreen; no personal save was accessed.

The single EXE is version **0.10.1.0**, **63,646,565 bytes**. The normal `dist/Solitude.exe` and retained `artifacts/release-v0101/Solitude.exe` match. SHA-256:

`64646E0E5CB2100D60E39A41A3B45DF27A656A5261404708753C36C1CC3D1CC6`

Test and build logs: `artifacts/ui-v0101.log`, `artifacts/build-v0101.log`. The broader rules, historical regression and performance baseline remains documented in [VERIFICATION-0.10.0.md](VERIFICATION-0.10.0.md); those broader measurements were not rerun for this appearance patch.
All 18 ORBIT source/package diagnostic images also match byte-for-byte. Evidence: `artifacts/source-v0101` and `artifacts/package-v0101`.
