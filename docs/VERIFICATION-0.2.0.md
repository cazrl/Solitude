# Verification — Solitude 0.2.0

Build date: 2026-09-08. This release addresses the feedback that the presets looked too similar by rebuilding the whole window treatment. Windows 7–11 remain deferred.

## Delivered executable

- `dist/Solitude.exe`, version `0.2.0.0`
- 56,904,547 bytes (56.9 MB)
- SHA-256: `EBA34B24FCC03281EF2EECF9972FBA6009D7CFADF330B499D19A3127D95EE40A`
- Self-contained, compressed .NET 10 `win-x64` bundle with embedded native dependencies, card images, felt and notices.
- Distribution directory contains exactly one file. No installer or separate .NET installation is required by the package configuration.
- The shipped executable exported all four embedded notice/license files successfully.

## Automated checks

**27 game/save verification groups passed**, including 500 initial deals and 200 simulated games with up to 200 actions each. These cover card integrity, stock/waste order, legal moves, scoring, Vegas pass limits, manual/automatic turning, undo, safe collection, hints, wins, save/reopen and damaged-save recovery. The game engine and save format were not changed by this release.

**125 window/input checks passed** in `tests/Solitude.UiChecks.csproj`. These instantiate the production game window without showing it and call its production event handlers. They check:

- All eight rendered frames are distinct.
- Square versus rounded window silhouettes and inactive captions.
- Availability of the installed historical bitmap fonts for the five applicable presets.
- Press/release control behavior and cancellation when releasing outside a control.
- Era list selection and applying settings without altering the current deal.
- Updating the actual window region when switching to XP.
- More/Back navigation in Options.
- Modal title dragging, maximize, restore and removal of rounded cutouts when maximized.

Compilation completed with zero warnings and errors.

## Production renderer and visual review

The **shipped EXE** generated **111 PNG views** under `artifacts/release-v020-renders/` and exited successfully:

- Eight game presets at 100%, 125%, 150% and 200%: 32 views.
- Settings, Deck/Appearance, Options, Help, About and Statistics for each preset at 150%: 48 views.
- Game menus, inactive windows and maximized-style frames at 150%: 24 views.
- Expanded Options for the seven pre-Vista presets: 7 views.

All 111 shipped-EXE render files were byte-identical to the reviewed development-render files. Representative full-size boards, menus, settings, compact options and help pages were inspected across the Windows families. The review corrected Vista card spacing, clipped rounded button borders, early font spacing, menu surfaces, status bars and empty slots. `artifacts/era-comparison.png` provides a cropped comparison of all eight frames and dialog treatments.

Historical reference screenshots and sampled palette values are documented in `WINDOW-REFERENCES.md`. The local Windows System/MS Sans Serif raster resources were read as data; no font files were installed or bundled. Vista's background now uses the preserved original felt image instead of a generated texture.

## Limits

- These checks do not certify exact pixel identity with every original OS build, theme or display setting. Window decorations are reconstructions. Vista's glass is painted and does not blur the live desktop.
- Me and 2000 correctly retain their shared warm gray/slate-blue palette; their lettering differs. Neighboring releases are not assigned fictional colors just to separate them.
- The settings, extra options, help text and convenience features remain specific to Solitude. Animated historical card backs and Vista's original transition/victory effects remain unimplemented. Vista currently uses the classic cascade when victory animation is enabled.
- No desktop control was performed in this revision. The prior Computer Use session was stopped with Escape, as recorded in `VERIFICATION-0.1.0.md`. Hidden handler tests do not establish physical mouse drag-and-drop, OS minimize/taskbar behavior or interaction with a visible packaged window.
- A clean Windows machine without .NET was not available. Self-contained packaging is verified from the bundle configuration and successful execution here, not an independent clean-machine test.
- Artwork provenance is documented in `ASSET-SOURCES.md`; public redistribution rights are not claimed. This release was built locally and was not uploaded or published.

Saved deals and statistics were not cleared, migrated or overwritten during these checks. All test windows used ephemeral state.
