# XP colour correction — 0.8.1

The user's comparison shows that 0.8.0's caption was too flat and its buttons too pale. The active caption's four-stop gradient omitted Luna's brighter blue lower band, while a broad translucent white overlay diluted the blue/red button faces.

The colour profile is sampled from the lossless [Blue Luna Calculator capture](https://guidebookgallery.org/pics/gui/applications/office/calculator/winxppro.png) already preserved at `references/WindowsUI/winxp-calculator.png`. It agrees with the structure of the user's supplied Form1 reference; that screenshot has compression variation, so it is not used as an exact RGB oracle. No downloads or original executables were needed for this update.

`Skin.Xp.cs` contains the caption's sampled scanlines and two glyph-free colour columns from each active button. The button interior interpolates those columns, with cached device-resolution bitmaps for fractional display sizes. The broad white overlay is removed. Hover/pressed feedback remains reconstructed. Caption geometry, icon placement, Settings access and compatible saved preferences remain as in 0.8.0.

At 100%, comparison checks 29 caption rows at x150, and button columns x4/x16 over rows 2–18, aligned to their respective button rectangles. The mean absolute RGB-channel error on these selected points changes as follows:

| Region | 0.8.0 error | 0.8.1 error |
|---|---:|---:|
| Caption | 10.00 | 0.00 |
| Blue button | 27.13 | 0.00 |
| Close button | 31.71 | 0.00 |

These are checks of sampled colours, not proof that every pixel, glyph, border or interaction matches original XP. Raw results: `artifacts/xp-colour-v081.json`. Visual comparison: `artifacts/xp-colour-comparison-v081.png`. Final package checks are recorded in [VERIFICATION.md](VERIFICATION.md).
