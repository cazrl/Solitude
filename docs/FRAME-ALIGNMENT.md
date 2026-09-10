# Caption and menu alignment — 0.8.0

The user's XP and Vista screenshots revealed an actual scaling error, not just a theme preference. `Graphics.DrawIcon` used the native device context while the rest of the frame used a scaled graphics transform. At 150%, the icon stayed near its unscaled position and size. The replacement draws a cached bitmap through the same transform and clip as the caption. Tests measure the rendered icon's colored pixels at 100/125/150/200%, including a translated frame origin.

Vista also added its top resize border to an already oversized caption area, producing a 31-pixel restored header. Its title sat too low, its icon came from XP, and its individually drawn button shapes/glyphs did not follow the original frame. Menu items used generic 48/40-pixel slots and centered text, pushing labels and the Help popup away from their original positions.

## Reference measurements

Primary artifact: Microsoft's [Custom Window Frame Using DWM](https://learn.microsoft.com/en-us/windows/win32/dwm/customframe) includes original Vista frames. The [standard/custom comparison](https://learn.microsoft.com/en-us/windows/win32/dwm/images/standard-custom-sidebyside.png) and [caption example](https://learn.microsoft.com/en-us/windows/win32/dwm/images/custom-caption-title.png) are preserved in `references/Frames`. Measurements use the 252-pixel example, in logical pixels at 96 DPI. The XP comparison uses the [original Luna Calculator capture](https://guidebookgallery.org/pics/gui/applications/office/calculator/winxppro.png) and the [original XP Solitaire capture](https://cdn.mobygames.com/screenshots/16120092-microsoft-windows-xp-included-games-windows-a-solitaire-game-in-.png) already recorded in WINDOW-REFERENCES.md. The latter uses Olive Luna; geometry is compared separately from its color scheme.

| Restored frame | Vista, width 252 | XP, width 260 |
|---|---|---|
| Header height | 27 | 30 |
| Icon box | x8, y7, 16×16 | x5, y8, 16×16 |
| Caption left | 28 | 25 |
| Minimize rectangle | x151, y1, 26×18 | x189, y6, 21×21 |
| Maximize rectangle | x177, y1, 25×18 | x212, y6, 21×21 |
| Close rectangle | x202, y1, 43×18 | x235, y6, 21×21 |

The XP icon's transparent padding is part of the original ICO, so its visible pixels do not begin at the icon-box origin. Vista caption buttons intentionally sit above the icon/text centerline, as the original DWM examples show. A maximized Vista frame uses a 21-pixel black caption and no restored side/bottom resize frame. The maximized policy also follows the original Solitaire capture in WINDOW-REFERENCES.md; the precise glyph rasterization of that downscaled JPEG is not used as a pixel oracle.

`Skin.CaptionLayout` is now shared by caption painting, the icon menu, caption button hit areas and the native resize-edge check. The upper portion of a Vista button no longer returns a resize hit. Menus use measured label widths plus 12 pixels and a four-pixel left inset; the popup anchors to its own label rectangle. At 100% XP this places Game at x9 and Help at x48.

## Artwork and rendering

Caption backgrounds, glyphs, frame edges and reflection PNGs are preserved by [VistaThemePlasma at commit 888afc2](https://github.com/aeroshell-desktop/vistathemeplasma/tree/888afc21db58be32e13dec0e00b19309284feb7c/misc/smod-theme/decoration). The archive identifies its resources as Microsoft artwork. These images replace the arbitrary drawn button gradients, lines and reflection polygons. Border slices are fitted to the measured native frame dimensions; the archive's Linux layout code and installation instructions are not used. Resource origin is not a binary comparison against an original Vista installation.

Each Vista game now has its own preserved Vista icon. Pre-Vista icons retain their existing period resources. The icon, caption, caption glow, buttons and button glyphs all honor display scaling; Vista frame slices and glow masks are cached at output resolution for consistent partial repainting. The glow comes from the actual caption glyph mask rather than a separately spaced outline.

Visual QA also reproduced a wrong-face result in Vista's GDI `TextRenderer` mask: `Font.Name` reported Segoe UI but the painted glyphs had serifs. The same requested font painted correctly through GDI+ `DrawString`. Vista now measures and paints with that engine at the final output resolution, then caches the mask; classic text keeps its existing GDI/bitmap path. A glyph-shape regression rejects the substituted serif capital I at all four scales. This establishes the observed rendering correction without attributing an unproven cause inside the framework.

The revised Vista header exposed fractional bitmap sampling during partial card redraws. Board and card caches now use snapped device-pixel origins and direct copies at their cached size; moving cards and stationary cards use the same coordinate mapping. This prevents clipping from changing the sampled pixels at 125/150% and leaves animation trajectories unchanged.

The game still paints its own frame. The glass tint is a fixed reconstruction with the preserved reflection texture; it does not blur the user's live desktop. Native Vista DWM compositing, every theme/DPI variation and complete pixel identity are not claimed. No system theme, font or Windows component was installed.

## Checks

`tests/CaptionUiChecks.cs` adds 100 checks covering reference geometry, actual icon pixels across six Windows eras and four scales, translated origins, Vista sans-serif glyphs, native WM_NCHITTEST results over caption controls, Help menu anchoring and maximized Vista client bounds. These complement the existing full UI suite and rendered-state comparisons. See VERIFICATION.md for the packaged build and final outcomes.
