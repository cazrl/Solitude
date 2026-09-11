# ORBIT polish and crash investigation — 0.11.6

The scene and table caches were overwriting part of the window outline. The complete one-device-pixel rim is now painted last, inside the same rounded silhouette as the native window. Native-region composites were inspected at 100%, 125%, 150% and 200%. The logo button now shares the window's top-left corner, with flush top and left edges and separators on its right and bottom. Hover/open orbital animation remains available.

Button labels, including Experience, Settings and the primary action, now use the visible glyph bounds rather than the font's line box for centering. Pixel checks verify both axes within one device pixel at all four scales. Historical text rendering is unchanged.

Foundation painting now keeps the highest settled card beneath incoming flights. Previously the engine's new top card was omitted from the board while animating, and the old top card was no longer painted: dropping a Two made its Ace disappear. The fix retains the Ace until the travelling card lands.

Per-column scrolling and its controls are removed. The board reserves height for the longest legal Klondike column: six hidden cards and a full King-to-Ace run. Cards size to the window, remain stable as play progresses, and keep visible rank/suit strips. Compact windows reclaim space from header/footer gaps. Normal, minimum and wide layouts were checked for full-stack visibility and selection of every face-up card. When the deal is ready, Complete the orbit occupies the primary dock button, keeping it clear of the foundations and tableau; New Game remains in the corner menu.

## Crash evidence and limits

Windows recorded a real crash of `dist/Solitude.exe` 0.11.5 at 02:42 on 11 September 2026. Reports contain managed exception code `e0434352` followed by callback exception code `c000041d`. The retained WER reports contain no managed stack trace; there was no Solitude error log. These codes alone do not identify the underlying defect.

The reported last action was moving onto another column. Tests now exercise dragging a King to an empty column to reveal the last hidden Queen, the appearance of completion, the remaining foundation moves, and the win transition at all four scales with animation enabled and disabled. A real hidden native handle also exercised accessibility notifications and window-message callbacks through 12 endgame repetitions and 372 further gameplay actions, including the stored ORBIT seed. The original crash was not reproduced. These checks do not prove its underlying cause is fixed.

UI callback exceptions now produce a dated stack trace and edition/deal context in `%LOCALAPPDATA%/Solitude/Solitude-error.txt` (or the configured data directory). Frame-pump callback errors enter the same managed error path. A first recoverable ORBIT interface error can resume only after validating the current 52-card state: it clears transient rendering/input state, pauses motion for this session, retains the deal and Undo, and shows an explanation. Saved motion preferences are unchanged. Invalid states, repeated failures and unrecoverable exception types are not silently resumed. Fault-injection checks verify deal preservation and continued play; this is a recovery measure, not a confirmed root-cause fix.

Evidence: `artifacts/orbit-polish` contains full frames and magnified native corner composites. Logs and package details are in [Verification](VERIFICATION.md). Physical display smoothness and end-to-end screen-reader behavior remain unverified.
