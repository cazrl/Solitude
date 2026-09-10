# Third-party notices

Historical Solitaire card artwork is associated with Microsoft Windows. Original classic card designs are credited to Susan Kare. Windows and Microsoft are trademarks of Microsoft Corporation. Solitude is an independent recreation and is not affiliated with or endorsed by Microsoft.

Preservation sources used by this local build:

- Radovan Janjic, `rjanjic/js-solitaire`: classic card sprite.
- Daniel Ricci, `danielricci/solitaire`: early Windows card-back images and their animation frames. The repository's MIT license is reproduced in `docs/licenses/danielricci-MIT.txt`.
- Zeke Sikelianos, `zeke/solitaire`: classic Windows card resources, used only as image data.
- Xiang, Steam Workshop item `262682467`: preserved Vista/Windows 7 card deck images.
- The Spriters Resource, Windows Vista / 7 Card Games, Backgrounds asset `573977`: five preserved backgrounds.
- The Spriters Resource, Windows XP Card Games, asset `144032`, extracted by Nova Stuart Hale: Spider card faces, spider back, felt tile and About artwork.
- The Spriters Resource, Windows XP Card Games, asset `103851`: the classic FreeCell king's two portraits.
- `esc0rtd3w/xp-cards`, commit `33a6d492ffa63f0b16b1ad982930c16d367f6bbb`: original game icons and Spider WAV resources extracted as data. Executable code was neither run nor embedded. Menu and dialog resources were also inspected as historical evidence. Version 0.9 also inspects the two special FreeCell deal branches as machine-code data and independently expresses the resulting ordered card layouts; the original executable is not shipped.
- `aeroshell-desktop/vistathemeplasma`, commit `888afc21db58be32e13dec0e00b19309284feb7c`: preserved Aero caption/button/glyph/frame and reflection image resources. Its README credits these resources to Microsoft. Only PNG artwork is used; no theme code is installed or executed. The archive's AGPL-3.0 license is reproduced in `docs/licenses/vistathemeplasma-AGPL-3.0.txt`; this does not establish ownership of the underlying Microsoft artwork.
- Wikimedia's preserved `Solitaire Icon (Vista).png`, `FreeCell Icon (Vista).png` and `SpiderSolitaire Icon (Vista).png`: Microsoft Vista game icon artwork.

- The Sounds Resource, Windows Vista / 7 Card Games, asset `442489`: twenty preserved game WAV effects, including FreeCell win music and Spider fireworks. These are embedded sound data only.
- Microsoft Windows System and MS Sans Serif bitmap-font strikes: extracted as FNT data from the local Windows font resources, now embedded to keep early lettering consistent between machines and scales. No font is installed. Source hashes and provenance are in `docs/FIDELITY-RESOURCE-HASHES-0.9.0.json` and `docs/ASSET-SOURCES.md`.

Original application code and the native runtime are not taken from those preservation projects. The game's logic, renderer, persistence and UI implementation are written in this repository.

The self-contained executable includes Microsoft .NET runtime components under their applicable licenses. The SDK-provided runtime notices are copied into `docs/licenses` for reference.

An upstream source-code license does not automatically license third-party Microsoft artwork. Rights to redistribute that artwork have not been independently established; see `docs/ASSET-SOURCES.md`.
