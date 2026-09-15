# Pinball menus — 0.12.1

Replaced the host-system menu renderer with an XP palette, flat gray border, blue selection, Tahoma text scaled with the window, check marks, submenu arrows, and separately aligned shortcut labels. WinForms still supplies menu keyboard navigation, mnemonics and accessibility.

The host retains the open popup and closes it on native playfield mouse clicks, frame clicks, Escape and deactivation. Native input passes through after notifying the host. Empty menu-bar space no longer opens Help. Replacing a popup disposes the old menu without stealing focus from another application.

Automated native workers suppress effects and MIDI output before playback, regardless of the sound preferences exercised by tests. Normal interactive runs retain the user's audio preferences.

Validation: `tests/PinballChecks.cs` covers menu dismissal from foreign-process left/right clicks (including the Players submenu), replacement, Escape, deactivation, empty menu-bar space and native command delivery, plus popup captures at 100%, 125%, 150% and 200%. The native integration suite also checks timing, flippers, pause/focus, settings persistence, resizing and shutdown. Evidence lives under `artifacts/pinball/menu-package-checks`; build and publish logs are in `artifacts/pinball/menu-build.log` and `menu-publish.log`.

The menus were visually inspected from captures. This is not an exhaustive pixel comparison against an XP virtual machine. Existing original-engine Player Controls and High Scores dialogs are outside this menu fix.
