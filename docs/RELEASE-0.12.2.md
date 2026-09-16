# Solitude 0.12.2 — Classic win animation fix

Fixes the dense intersecting lines reported during Windows 98's bouncing-card win animation. The correction applies to classic Klondike editions from Windows 3.0 through XP.

- Cards leave the foundations one at a time, preserving the classic trails without the overlapping burst.
- Fixed animation steps produce consistent trails at 60, 120 and 144 display frames per second.
- Delayed frames cannot draw an unbounded backlog of trails.
- All 52 cards complete the sequence; the existing click and keyboard skip controls remain available.
- Won deals, scores, timers and saved preferences remain unchanged.

**Validation:** 425 focused animation checks and 2,370 fidelity checks passed using silent, ephemeral windows. Build completed with zero warnings and errors. The tested assembly matched the assembly used for the packaged EXE.

**CI limitation:** The previous full GitHub verification run failed the Windows 95 FreeCell double-click check at 200% display scale. That separate failure has not been resolved in this animation release; the local results above are not a claim that the full CI suite passes. [Previous run](https://github.com/cazrl/Solitude/actions/runs/34913051358).

Download **Solitude.exe** below for Windows 10/11 x64. No installer or separate .NET installation is required.

Version: **0.12.2.0** · Size: **66,933,093 bytes**

SHA-256:
```text
23A99E0D7E8A75F8DCA940624030D2B39D832C1C20F80AE3908AA703E6CF9A41
```
