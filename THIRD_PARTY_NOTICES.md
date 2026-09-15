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

The card-game logic, renderer, persistence and UI implementation are written in this repository. Pinball uses the separately attributed native engine below.

## Space Cadet Pinball

- Native engine: Andrey Muzychenko and contributors, [k4zmu2a/SpaceCadetPinball](https://github.com/k4zmu2a/SpaceCadetPinball), WindowsClassic commit `20032b08931000da0dc6c508f270c9e5c728bcb6`, under the MIT license. The full copyright and permission notice is in `docs/licenses/SpaceCadetPinball-MIT.txt`, embedded in the EXE. Modified source and host integration are in `third_party/SpaceCadetPinball`.
- Original table, font, MIDI and WAV data: Cinematronics / Maxis / Microsoft. Retrieved from the public browser port linked by upstream, [alula's Space Cadet port](https://pinball.alula.me/), on 2026-09-12. These are original game data, not MIT-licensed assets. Their redistribution rights have not been independently established. See `docs/PINBALL-0.12.0.md` for hashes and provenance.
- Solitude's host, selector integration, process isolation and private settings storage: created by Cazrl.

ORBIT / Solitude 2126 uses original procedural card designs, observatory scenery, iconography, animation and synthesized audio implemented in `FutureArt.cs`, `Skin.Future.cs` and the `GameWindow.Future` files. No downloaded artwork or audio was added for this fictional edition. It uses the host's Segoe UI font for text; that font is not newly embedded or installed by ORBIT.

The self-contained executable includes Microsoft .NET runtime components under their applicable licenses. The SDK-provided runtime notices are copied into `docs/licenses` for reference.

An upstream source-code license does not automatically license third-party Microsoft artwork. Rights to redistribute that artwork have not been independently established; see `docs/ASSET-SOURCES.md`.
