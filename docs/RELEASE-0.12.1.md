# Solitude 0.12.1 — Space Cadet Pinball

Space Cadet Pinball joins Solitaire, FreeCell, Spider and ORBIT in the same portable Windows x64 EXE. Select **Settings → Windows XP → 3D Pinball: Space Cadet**.

- Original classic engine physics, missions, scoring, tilt, demo and one-to-four-player modes, with saved high scores and controls.
- XP-styled dropdowns with scaled Tahoma text, aligned shortcuts, check marks and submenu arrows. Menus dismiss on table clicks, Escape and focus loss.
- About pages credit **Cazrl** and include a **Donate** button linking to Ko-fi.
- Automated Pinball checks suppress both effects and music, including when testing audio preferences.
- Pinball runs in an isolated worker from the same EXE; existing card deals and settings remain preserved.

Hold **Space**, then release to launch. **Z** and **/** control the flippers. **F3** pauses; **F6** returns to Settings. An unfinished Pinball game is not resumed after closing.

## Download

Download `Solitude.exe` from this release and run it on Windows 10 or 11 x64. No installer or separate .NET installation is required.

Version: **0.12.1.0** · Size: **66,933,093 bytes**

SHA-256:
```text
EA5AFFCAFD9E7AB0E29DDCF0E9DF0EDC41BAD531B7927B181D5F5E62DE8F6959
```

## Validation

The final packaged EXE passed **49 Pinball integration checks**, including menu dismissal and display scaling at 100%, 125%, 150% and 200%. Its build completed without warnings or errors. The earlier 0.12.0 regression gate passed 53 engine groups, 27,137 UI checks, 384 native card-game checks and 290 matching source/package renders. The personal card save was unchanged by testing.

The Pinball engine uses the pinned MIT-licensed WindowsClassic source from k4zmu2a/SpaceCadetPinball. Original game artwork, sounds and resources retain their separate rights; see the repository's third-party notices. Native Player Controls and High Scores dialogs still use the current Windows dialog rendering.
