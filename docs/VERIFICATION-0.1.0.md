# Verification — Solitude 0.1.0

Build date: 2026-09-08.

## Delivered artifact

- File: `dist/Solitude.exe`
- Size: 56,760,163 bytes (approximately 56.8 MB)
- SHA-256: `F44045ACE2554FA2A1690B1FC415CBB869CBCC9EE8570F90CF1942F8A6EDA660`
- Publish target: `win-x64`, self-contained .NET 10, compressed single-file bundle, embedded native dependencies and artwork.
- Distribution directory verified to contain exactly one file, `Solitude.exe`.
- Four embedded third-party notice/license files were extracted successfully using the shipped executable's `--licenses` option.

## Passed

The final game-engine tests passed **27 verification groups**, including 500 initial deals and 200 simulated games with up to 200 actions per game. Coverage includes:

- All 52 distinct cards, seven-column dealing and repeatable shuffles.
- Draw-one/draw-three, remainder handling, waste accessibility and stock ordering.
- Vegas one-pass/three-pass limits.
- Alternating-colour descending tableau sequences, King-only empty columns and matching-suit ascending foundations.
- Manual/automatic card turning and restoration through undo.
- Standard/Vegas/None scoring, elapsed-time preservation and undo penalties.
- Conservative automatic collection and non-revealing hints.
- Recognition of a completed game and stopping further play.
- Save/reopen with preferences and undo history.
- Rejection of invalid cards, sequences and null states.
- Backup recovery with preservation of unreadable originals.

Build completed with no compiler warnings or errors. The **shipped executable** successfully generated **56 PNG views** through its production renderer: all eight eras at 100%, 150% and 200%, plus Settings, Deck/Appearance, Options and Help at 150% for every era. Files are under `artifacts/release-renders/`.

Representative full-size views were inspected visually across the early Windows, classic gradient, XP and Vista interface families. That review caught and corrected red-suit card borders, XP card-back corner masking, Help spacing, and the different suit/rank order in the Vista Large Print source atlas. The four Vista deck previews now show the same King of Hearts.

The executable also succeeded with its normal render-mode invocation from the distribution folder. A clean Windows machine without .NET was not available for a separate installation-independence test; self-contained packaging is established by the publish configuration and bundle output. The attempt to inspect runtime module paths from the sandbox produced no module listing and is not treated as additional runtime evidence.

## Desktop testing boundary

A development build was launched through the Windows computer-use API and its live XP game window was inspected at 960 × 720. Computer Use then reported that the user stopped control with the physical Escape key. All further computer-control calls stopped.

Consequently, live end-to-end drag-and-drop, menu selection, resize interactions, and close/reopen testing of the final published EXE are **not verified**. The rule engine, persistence and production renderer were checked independently without controlling desktop windows.

## Historical fidelity boundaries

This build is playable and contains all requested presets through Vista, but is **not yet an exact pixel-for-pixel reproduction of each shipped Windows game**.

- Historical card artwork is embedded from the preservation sources documented in `ASSET-SOURCES.md`.
- Window borders, fonts, title buttons, menus and dialogs are custom reconstructions. Not every original OS build, theme and DPI combination has been captured and compared.
- Windows 3.0 and 3.1 currently share a window treatment; 95/98/Me/2000 differ primarily in caption treatment and font choices. Release-specific subtleties remain unverified.
- Vista's felt is synthesized; its original background choices, card movement transitions and victory effects are not reproduced. Vista currently uses the implemented classic cascade when animations are enabled.
- Animated classic card backs are currently static. The familiar classic bouncing-card win animation is implemented, but live animation playback was not exercised before computer control stopped.
- Extra conveniences include unlimited undo up to 200 stored states, safe collection, saved-game resumption and pause-on-unfocus timing. These are not a byte-for-byte emulation of original executable behavior.
- Microsoft artwork redistribution rights have not been independently established. This build remains local; nothing was uploaded or published.

Windows 7, 8/8.1, 10 and 11 remain deferred as requested.
