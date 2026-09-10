# Verification — Solitude 0.8.1

Built locally on 2026-09-08. This update corrects the XP caption and button colour profiles shown in the user's comparison.

## Delivered executable

- `artifacts/release-v081/Solitude.exe`, version **0.8.1.0**; exactly one self-contained Windows x64 EXE.
- Size: **59,131,235 bytes**.
- SHA-256: `5BA71ABDF97EE53FA5E3921357827177B6C051500E120C03BE17253B3EB0BBB6`.
- The running `dist/Solitude.exe` remains 0.8.0, with its hash unchanged. Close that copy before opening the new executable. No process was terminated.
- Local build only; no installation, upload, visible game launch or desktop input. Render checks use isolated ephemeral state, not personal saves.
- Publish passed with NU1900: online vulnerability metadata was unavailable, so that scan did not complete.
- Previous report: [VERIFICATION-0.8.0.md](VERIFICATION-0.8.0.md).

## Changes and checks

The active XP caption now includes the reference's brighter lower blue band. Blue and red button faces use sampled colour profiles instead of a broad white overlay. No game engine, storage, settings or caption geometry changes were made. Source measurements and limits: [XP-COLOUR.md](XP-COLOUR.md).

- **1,725 production UI checks passed** against final source: `artifacts/ui-v081.log`. This includes all 17 game profiles, four scales, double-click isolation, shared settings, caption hit testing and partial/full redraw equivalence.
- Development and packaged executables each rendered 225 application views plus 36 motion samples. All **261 PNG hashes match**.
- XP was visually inspected at 100/125/150/200%. The before/after comparison includes the user's original Form1 reference at its original size: `artifacts/xp-colour-comparison-v081.png`.
- Sampled caption and button RGB values now match the lossless XP reference at the documented probe locations: `artifacts/xp-colour-v081.json`. This does not establish full-frame pixel identity.
- Packaged render and notice export exited 0. All five notices match their source files.
- Final artifact checks: `artifacts/package-validation-v081.json`; build log: `artifacts/package-v081.log`.
- Rules/persistence and performance were not separately rerun for this colour-only update. Earlier results remain in the versioned reports; the full production UI suite above was rerun.
