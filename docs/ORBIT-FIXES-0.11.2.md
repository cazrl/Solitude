# ORBIT centering and game time — 0.11.2

Selecting ORBIT now resizes and centers the application window within the current monitor working area. The monitor is captured before resizing, so enlargement cannot select another monitor accidentally. Taskbar space is excluded. The same behavior applies when the selector is confirmed with ORBIT already selected. Startup still uses the existing centered start position.

Gameplay time now samples `Environment.TickCount64`, accumulating whole milliseconds and advancing exactly once per 1,000 accumulated active milliseconds. It is independent of the high-resolution stopwatch used for animation. Repeated callbacks at the same timestamp add no time; slow callbacks account for their actual duration. Opening a new game, loading a checkpoint, switching editions and closing dialogs reset the appropriate baseline. Activation also resets the baseline to avoid charging time spent away when timer delivery was delayed.

## Evidence

- 14,888 final ORBIT checks passed, including centering through the Settings UI at 100%, 125%, 150% and 200%.
- Clock tests covered XP, Vista and ORBIT at simulated 1, 8, 16, 250 and 1,700 millisecond callback intervals, repeated timestamps, paused intervals and reactivation.
- Final real-time check: 10.009 wall-clock seconds, 10.000 seconds from the gameplay source, 10 displayed game seconds. The animation stopwatch measured 10.010 seconds. An earlier clock-only run produced equivalent results.
- Original continuous timer acceleration was not reproduced locally; the original stopwatch also measured normally in this environment. The clock-source change is a defensive correction with verified real-time progression, not a confirmed diagnosis of the user's reported acceleration.

Logs: `artifacts/orbit-center-final-ui.log`, `artifacts/orbit-clock-ui.log`, `artifacts/build-v0112.log`. UI interactions were simulated in offscreen forms with isolated game data; physical multi-monitor switching and the user's original running instance were not observed.
