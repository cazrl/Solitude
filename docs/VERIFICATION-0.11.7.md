# Verification — Solitude 0.11.7

This release makes ORBIT's opening deal visibly leave the stock and replaces the light-dot win effect with a choreography of all 52 cards. See the [card motion report](ORBIT-CARD-MOTION-0.11.7.md).

The full historical/ORBIT suite passed **24,684 UI checks**. After a final compact-window adjustment to the skip prompt, the final build passed **2,737 focused animation checks** and **384 native-window checks**, including 12 last-reveal repetitions and 372 gameplay actions. The engine suite passed **53 verification groups**. The clock regression measured 10.012 wall-clock seconds and 10 displayed seconds.

Animation checks cover the actual pixels between the stock and tableau, all 28 deal origins, all 52 victory card identities, stable sprite-cache size, all four display scales, compact windows, palette rendering, last-card landing before victory, timed results, click/Escape skip and reduced motion. The won state remains unchanged during the effect. Test windows were hidden; no user desktop input was sent.

The single-file Windows x64 package is **0.11.7.0**, **63,702,373 bytes**, at `dist/Solitude.exe`, with an identical retained copy at `artifacts/release-v0117/Solitude.exe`. The prior EXE was backed up.

SHA-256: `6A813F532D73EF18BE161D872AD858179B48487CA91F8BC13830E6A189D63B85`

All **29 source/package PNGs match byte-for-byte**: 12 ORBIT views, six deal samples and 11 victory/results samples. Evidence: `artifacts/source-v0117`, `artifacts/package-v0117`, and `artifacts/package-parity-v0117.json`. The regular board, menus and dialogs are unchanged visually from 0.11.6 except the About version number.

Final offscreen animation measurements, using complete production-renderer frames on a 32-bit ARGB bitmap:

| Display scale | Normal window median / p95 | Compact window median / p95 |
|---|---|---|
| 100% | 9.30 / 11.90 ms | 5.72 / 6.84 ms |
| 125% | 11.07 / 12.57 ms | 7.27 / 7.79 ms |
| 150% | 14.90 / 18.12 ms | 9.60 / 10.46 ms |
| 200% | 23.34 / 29.65 ms | 16.64 / 18.63 ms |

These are paint costs, not physical display FPS. Large windows can exceed a 60 FPS paint budget during the 52-card effect. Monitor/compositor smoothness remains unverified. The new GIFs are sampled production frames (30 FPS deal, 20 FPS victory), not screen recordings.

Logs: `artifacts/ui-v0117.log`, `artifacts/orbit-cards-v0117.log`, `artifacts/orbit-native-v0117.log`, `artifacts/engine-v0117.log`, `artifacts/cards-final-build.log`, and `artifacts/build-v0117.log`. UI test compilation warned that NuGet vulnerability data was unavailable (NU1900); compilation and execution succeeded. The release build succeeded.

This is a local package. The public GitHub release remains 0.10.1. The earlier reported crash still has no confirmed root cause; this change does not claim otherwise. Earlier verification: [0.11.6](VERIFICATION-0.11.6.md).
