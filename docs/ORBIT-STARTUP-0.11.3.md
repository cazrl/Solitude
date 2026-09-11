# ORBIT startup position — 0.11.3

The 0.11.2 selector fix missed direct startup. The native window was positioned while it still had its default 300 by 300 size, and enlargement left its top-left corner unchanged. On the reported 2560 by 1440 monitor with a 1392-pixel working height, that produces (1130, 546), the exact origin observed on the running clipped window through Computer Use.

ORBIT now uses manual startup placement, remembers the launch monitor working area before handle sizing, and centers the final fitted window in OnLoad before display. The selector and startup share the same placement routine. The routine also reapplies fitted size after a synchronous DPI transition. At 150% in that working area, the intended 1680 by 1080 window bounds are (440, 156, 1680, 1080).

## Verification

14,936 ORBIT checks passed, including 48 new startup lifecycle assertions at four scales and four working areas. Cases cover the reported desktop, a small display, taskbar insets and positive/negative monitor origins. Each starts from the incorrect default-size origin and verifies containment and centered final bounds after OnLoad. Existing edition-switch checks also passed.

The pre-fix running window was inspected and its clipped origin confirmed. Closing that game was rejected by automatic approval review to protect its current deal. A separate development build was launched, but Computer Use was then stopped by the user with Escape before its final bounds could be inspected. No further desktop input was issued. Consequently the corrected startup has lifecycle test coverage, but final physical-window verification is incomplete; simulated signed monitor origins do not prove physical multi-monitor behavior.

Logs: `artifacts/orbit-launch-ui.log`, `artifacts/orbit-launch-build.log`, `artifacts/build-v0113.log`.
