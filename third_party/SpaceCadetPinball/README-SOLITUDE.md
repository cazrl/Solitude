# Solitude's Space Cadet integration

Upstream: https://github.com/k4zmu2a/SpaceCadetPinball/tree/20032b08931000da0dc6c508f270c9e5c728bcb6

WindowsClassic commit `20032b08931000da0dc6c508f270c9e5c728bcb6`, downloaded 2026-09-12. `LICENSE` and `UPSTREAM-README.md` preserve upstream attribution. The contents of `SpaceCadetPinball/` are vendored sources, with the changes documented below. This is the classic Win32 branch, not the SDL/ImGui port.

Build with `tools/Build-Pinball.ps1` or the normal Solitude .NET project. `Solitude.Pinball.vcxproj` builds a Windows x64 DLL with the static C runtime and Windows system libraries. No native dependency is downloaded during builds.

`SolitudeHost.cpp` exports one entry point, `SolitudeRun(HWND)`. Solitude launches another copy of its own EXE in worker mode, extracts and verifies the DLL and data, then hosts the native child window. The worker terminates after that lifetime; native global state is never reused in the card-game process.

Changes to the upstream files:

- `winmain.cpp`: set the resource module before loading localized strings; create a child window; suppress the splash, legacy executable/table registration, independent full-screen mode and automatic restart; force XP's PINBALL.DAT; keep the original game menus available through host commands; route F1/F4/F6 to the host; detect loss of the parent; support offscreen capture; clamp voice count; ensure changing player count during the opening light show applies immediately.
- `fullscrn.cpp`: retain native scaling but let the host own placement and maximize/restore. The game does not change desktop display settings.
- `pinball.cpp`: resolve table/audio paths from the worker's verified runtime directory.
- `options.cpp`: store settings and high scores in a private INI file, rather than Windows' original Pinball registry keys; clamp player count; default to upstream's alternative full-frame rendering at 60 FPS, retaining its 120 UPS simulation.
- `Sound.cpp` and `midi.cpp`: suppress audio output in automated evidence runs, including when tests toggle sound and music preferences.

The host bridge also forwards native playfield mouse-down notifications so managed menus dismiss on outside clicks. Normal table input continues through the original handler.

All physics, collisions, missions, ranks, table objects, scoring, ball allocation and tilt rules otherwise use the vendored engine. Integration tests are in `tests/PinballChecks.cs`; no claim of exhaustive equivalence to an original XP installation is made.

The original table, audio, font, icon and splash resources are not made MIT-licensed by this directory's code license. See `THIRD_PARTY_NOTICES.md` and `docs/PINBALL-0.12.0.md` at the repository root.
