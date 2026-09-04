# Linux parity roadmap

Devolutions Terminal on Linux should feel like Windows Terminal to a Windows
Terminal user on day one, stay one cross-platform product, and look like a
native app on the desktop it is running on.

This file is the product plan for that bar. Measured behavior stays in
[parity-status.md](parity-status.md). Historical port notes stay in
[history/porting.md](history/porting.md). macOS stays on its own track in
[macos.md](macos.md). Native GTK and Qt shell architecture lives in
[linux-native-shells.md](linux-native-shells.md).

## North star

Three promises, held at the same time:

1. **Familiar behavior.** Tabs, panes, keybindings, command palette, `settings.json`,
   profiles, schemes, actions, CLI (`dt` / `wt` shape), startup, and multi-window
   broker routing match the Windows Terminal subset we claim. A user can drop in a
   real WT settings file for the supported surface and get the same chords and the
   same results.
2. **Cross-platform core.** One settings model, one action catalog, one broker,
   one engine and PTY stack. Linux does not fork the product. Shell frontends
   plug in under shared services.
3. **Real native Linux presentation.**
   - GNOME and other libadwaita desktops: **GTK4 + libadwaita** own window chrome,
     header, tabs, menus, and dialogs.
   - KDE Plasma and other Qt desktops: **Qt 6** owns the same roles with Plasma
     styling.
   - Avalonia remains the shell on Windows and macOS, and a Linux fallback only.
     It is not the long-term way we "look like GTK" or "look like Qt."

If those ever fight, **behavior keeps the WT name and semantics**, and
**presentation maps into the active desktop toolkit**. We do not fake Mica, DWM
caption buttons, Explorer jump lists, painted Adwaita, or painted Breeze.



## Chrome shape (Linux)

GNOME header first, tabs second:

 - Header bar owns new tab, window title, find, app menu, and a close control.
 - `+` opens the default profile in one click. Chevron beside `+` picks another
   shell or profile. Hamburger is the app menu (settings, palette, about).
 - No minimize or maximize buttons. GNOME users maximize with Super+Up or
   header double-click. Fullscreen still has an exit control.
 - Tab strip sits on the row under the header when tabs are visible.
 - Single-tab default hides the tab row (AlwaysShowTabs=false), keeps the header.
 - Header buttons stay flat like Adwaita (fill only on hover). No permanent
   gray pills. Header and tab row share one CSD surface color.
 - Windows Terminal keeps tabs-in-titlebar on Windows and macOS. Linux does not
   copy that strip into the CSD header.

That is the screenshot bar: WT chords and tab behavior, Adwaita-shaped chrome.

**Accepted baseline (2026-09-04).** The flat header, `+` / shell chevron /
hamburger split, close-only captions, shared CSD surface, and tab row under the
header is the balance we want. WT design for actions and tab muscle memory.
GNOME standards for chrome. Do not walk this back toward Win caption strips or
permanent gray pill buttons without a product reason.

**Materials (interim Avalonia only).** While the Avalonia Linux shell still
runs, chrome may use libadwaita dark tokens via `AdwaitaChrome`
(`headerbar_bg` `#2e2e32`, window `#222226`, view `#1d1d20`, popover `#36363a`).
That paint is a stopgap. It is not the destination.

**Honest floor (2026-09-04 screenshot set).** Side by side with real GNOME
Terminal, Calculator, Text Editor, Files, and Settings:

 - Layout and behavior are the accepted baseline. Buttons, split, menus,
   close-only captions, tab row: keep as the *shell IA* for native frontends.
 - The remaining gap is not missing corner radii. It is toolkit paint.
 - Real GNOME apps draw through libadwaita + GSK + Pango + Adwaita symbolic
   icons + Mutter CSD. Real Plasma apps draw through Qt and Breeze. Avalonia
   Skia can copy colors and structure. It cannot become GSK or the Qt scene
   graph. Blind A/B still reads "inspired by," not "is."
 - **Direction (personal spike, 2026-09-04):** stop relying on Avalonia to
   impersonate GTK or Qt. Link and use actual GTK on GNOME and actual Qt on
   KDE. Shared WT engine and behavior core stay. Architecture, phases, and
   gates: [linux-native-shells.md](linux-native-shells.md).
 - Interim: keep the accepted Avalonia layout for daily Linux use. Do not spend
   cycles on fake-GNOME or fake-Breeze material polish. Behavior parity and
   install/XDG workstreams stay valid on every shell.

## What "done" means on Linux

A GNOME user can:

 - Install from the distro package or AppImage and get a valid `.desktop`, icons,
   AppStream metadata, and a reversible default-terminal helper.
 - Open the app and immediately use WT muscle memory: new tab, split pane, close
   pane, command palette, profile picker, search, copy/paste modes, zoom, focus
   mode, quake/summon *where the desktop allows it*.
 - Keep using the same `settings.json` concepts and action names. Unknown keys
   still round-trip. Linux-only paths use XDG.
 - See one coherent header from the **desktop toolkit** (GTK on GNOME, Qt on
   Plasma): tabs and window controls with no double titlebars, no Windows
   caption strip, no random CSD fights with the compositor.
 - Get honest diagnostics when a WT feature is Windows-only or the compositor
   cannot do the thing (global hotkey portal, virtual desktop move, and friends).

A Windows Terminal user on Linux should not need a second manual for tabs and
keys. A GNOME user and a Plasma user should each get chrome that belongs on
their desktop.

## Two contracts

### Behavior contract (shared, WT-shaped)

Owns:

 - Action catalog and keybinding resolution
 - Tab and pane tree semantics
 - CLI and broker multi-window routing
 - Settings layering, profiles, schemes, fragments, state/workspaces
 - VT / engine selection and connection lifecycle
 - Accessibility and clipboard policy at the control layer

This contract is **cross-platform**. Linux does not get a parallel action system.

### Presentation contract (Linux / desktop-toolkit-shaped)

Owns:

 - Shell frontend selection (GTK, Qt, Avalonia fallback)
 - Window state machine (normal, maximized, fullscreen, focus mode, quake)
 - Header and tab chrome layout via the active toolkit
 - System font defaults (Adwaita Mono / Noto / Plasma defaults) and emoji fallback
 - Opacity / blur mapping from WT acrylic and Mica settings (no fake materials)
 - Freedesktop portals, notifications, default terminal, MIME/`dterm:` handlers
 - Tray / status icon where the session actually supports it
 - Package metadata and desktop integration helpers

Linux presentation is not "turn off a few Windows ifdefs" and not "theme
Avalonia until it looks native." It is first-class shell frontends behind small
interfaces. See [linux-native-shells.md](linux-native-shells.md).

## In scope

 - GNOME on Wayland as the primary GTK desktop target (Mutter + libadwaita).
 - KDE Plasma on Wayland as the primary Qt desktop target.
 - Other free desktops on a best-effort path through GTK, Qt, or Avalonia
   fallback, with capability reports instead of silent no-ops.
 - Toolkit-neutral terminal surface so cell rendering is not stuck inside one
   UI framework forever.
 - WT settings and action parity for the subset already inventoried in
   `compat/windows-terminal.json` and registered in the app.
 - Local shells through `dt-pty-host`, Azure Cloud Shell, built-in and Ghostty
   engines with explicit capability limits.
 - Reproducible x64 and ARM64 packages: tar, deb, rpm, AppImage, desktop install
   helper.
 - Shell integration data that feeds Suggestions, Quick Fix, marks, and progress
   when the shell provides it.
 - Live UI acceptance on a real GNOME session, not only headless package gates.

## Out of scope

Leave these alone on Linux. Document them as unavailable when a settings file
asks:

 - DWM, Mica, acrylic materials, Win32 caption metrics
 - Jump lists, Explorer `IExplorerCommand`, OpenConsole default-terminal handoff
 - ConPTY-identical process semantics
 - Live cross-window PTY handle transfer (no portable public API)
 - Pixel-identical AtlasEngine / HLSL shaders
 - Shipping a second settings model or a GNOME-only / Plasma-only action rename
   layer
 - Loading GTK and Qt in the same process
 - Using Avalonia themes as the long-term stand-in for libadwaita or Breeze
 - Turning Linux into a full remote hub (serial, generic SSH session types) unless
   product later expands the connection surface on every OS together

macOS is not part of this roadmap except where a shared `IShellFrontend`
abstraction falls out naturally.

## Current baseline (orientation only)

Do not treat this section as the plan. It is just where the tree sits so the work
below is not aimed at ghosts.

 - Shared app shell, settings, actions, broker, Skia renderer, and both engines
   already exist.
 - Linux PTY, XDG settings paths, dynamic Unix profiles, and package/desktop
   metadata already exist.
 - Desktop open and notifications prefer portals with `xdg-open` /
   `notify-send` fallbacks.
 - Global hotkeys on Linux are explicitly unsupported pending a real
   GlobalShortcuts portal session story.
 - Window chrome still carries too much Linux policy inside `MainWindow`
   (`OperatingSystem.IsLinux()` treated as GNOME). That is a structure problem,
   not a missing tab feature.
 - [parity-status.md](parity-status.md) remains the measured truth. When this
   roadmap and that file disagree on a fact, fix the fact in parity-status.

## Workstreams

Order matters. Each stream has a contract, a deliverable shape, and an exit
check. Prefer small vertical slices that keep NativeAOT and existing tests green.

### 1. Window state and native shell frontends

**Goal:** WT window actions keep their names. Linux chrome comes from GTK or Qt,
not from painted Avalonia. Details and phases:
[linux-native-shells.md](linux-native-shells.md).

**Build:**

 - `IShellFrontend` / `IWindowStateController` (names flexible) with Avalonia
   (Win/mac/fallback), GtkShell, and QtShell backends.
 - Toolkit-neutral terminal frame sink so the engine is not married to Avalonia
   controls forever.
 - Explicit states: Normal, Maximized, FullScreen, FocusMode, Quake.
 - One header region per desktop IA: tabs, new-tab, profile menu, app menu,
   search, caption policy that matches the DE. No second system titlebar. No
   Win-style min/max/close strip on GNOME.
 - Fullscreen and maximize go through the backend so command palette, keybindings,
   caption buttons, and launch modes cannot disagree.
 - Desktop detection (`XDG_CURRENT_DESKTOP`, `--shell=`) before toolkit init.
   Never init GTK and Qt together.
 - Map `showTabsInTitlebar`, `alwaysShowTabs`, `showTabsFullscreen`, and focus
   mode into each shell's header visibility rules without inventing new settings
   keys.
 - Interim Avalonia Linux layout stays as fallback and reference IA only.

**Exit check:**

 - `toggleFullscreen`, `setFullScreen`, `setMaximized`, focus mode, and launch
   modes change real window state on Mutter and on Plasma.
 - Blind header crop on GNOME reads as libadwaita; on Plasma reads as Qt.
 - Tab row and keybindings still match WT expectations with a WT settings fixture
   on every shell.
 - Headless unit tests cover the state machine and fake frontends; live GNOME and
   Plasma gates cover compositor paths.
 - Phase 0 GTK AOT smoke documented in linux-native-shells decision log.

### 2. Theme, type, and materials adapter

**Goal:** Same appearance settings keys. GNOME-looking defaults and mapping.

**Build:**

 - `ThemePlatformAdapter` (name flexible) resolves profile and theme appearance
   per OS.
 - Default Linux profile font faces toward Adwaita Mono / system monospace, with
   bundled Noto Color Emoji kept as the deterministic emoji fallback.
 - WT `useAcrylic`, opacity, unfocused acrylic, and `useMica` map to whatever
   Avalonia and the compositor can actually do (opacity, optional blur). No
   pretend Mica.
 - Settings editor spacing and controls stay on the shared page model; visual
   density should not fight Adwaita when running on GNOME.
 - Color schemes and WT theme JSON keep working unchanged.

**Exit check:**

 - Fresh Linux profile looks at home next to GNOME Terminal / Ptyxis without a
   custom settings file.
 - A WT settings file with Cascadia / acrylic still loads; Linux maps materials
   instead of crashing or painting a fake WinUI frame.

### 3. Freedesktop integration depth

**Goal:** Install and desktop behavior a GNOME user trusts.

**Build:**

 - Keep portal-first open and notify paths. Add notification *actions* where the
   Notifications portal allows (show window, new tab).
 - Global summon / quake: either a small reflection-free GlobalShortcuts portal
   client with an explicit permission UX, or a documented first-class path through
   GNOME Settings keyboard shortcuts + `dt -w` / broker. Pick one product story
   and test it. Do not leave "unsupported" as the only answer in the UI without a
   setup affordance.
 - Tray / StatusNotifierItem: tell the truth in the capability report; support the
   session backends Avalonia can use; do not claim a probe we do not have.
 - Default terminal: keep `xdg-terminal-exec` and Debian alternatives; extend
   helpers only where we will test them (Fedora/Arch if we claim them).
 - Optional later: Nautilus "Open in Terminal" style entry points via supported
   extension or desktop contracts. Not a blocker for the north star.
 - Virtual desktop move on summon stays best-effort or unsupported with a clear
   diagnostic. Do not pretend every compositor is the same.

**Exit check:**

 - `Install-LinuxDesktopIntegration.sh` diagnose output matches real session
   capabilities.
 - Notification and open paths work on a stock GNOME session with portals.
 - Summon/quake has a supported setup path a user can complete without reading
   source.

### 4. Shell integration provider

**Goal:** Suggestions, Quick Fix, marks, and progress stop being empty shells on
Linux when the shell can help.

**Build:**

 - `IShellIntegrationProvider` fed by OSC 133 / prompt marks and related sequences.
 - Shared across OSes. Linux ships XDG drop-ins for bash, zsh, fish, pwsh where we
   already generate profiles.
 - Command palette entries stay registered; no-data remains an explicit
   notification, never a silent no-op that looks like a broken keybinding.

**Exit check:**

 - With integration enabled, marks and at least one suggestions path show real
   data in a live pwsh or bash session on Linux.
 - Without integration, the user sees why.

### 5. Engine capability surface

**Goal:** Built-in and Ghostty stay selectable; limits stay honest.

**Build:**

 - Keep the pinned Ghostty ABI boundary explicit for images, DRCS, row rendition,
   and keyboard protocol state.
 - Surface capabilities in settings and in-app diagnostics the same way on every
   OS.
 - Product default on Linux can prefer built-in or Ghostty, but switching must not
   strand image-heavy workflows without a message.

**Exit check:**

 - Capability matrix in parity-status matches runtime diagnostics.
 - No Linux-only special case that lies about Ghostty features.

### 6. Validation and release gates

**Goal:** "GNOME native + WT familiar" is testable, not a vibe.

**Build:**

 - Keep existing non-UI package, PTY, broker, and ARM64 hardware gates.
 - Add a live Mutter UI gate (x64 first, ARM desktop when available): header,
   tabs, fullscreen, maximize, palette actions, portal open, notifications,
   summon setup path.
 - Add a WT settings fixture gate: load a representative WT `settings.json`,
   assert keybindings and startup actions for the supported subset.
 - Record results in [parity-status.md](parity-status.md). This roadmap only
   changes when the *plan* changes.

**Exit check:**

 - CI or a documented manual gate fails if fullscreen/palette/window state
   regresses on GNOME.
 - Package gates still block bad desktop metadata.

## Progress

Work started against this plan (branch work, not a claim that Linux is done):

 - Window chrome policy lives in `WindowChrome.Resolve` / `WindowStateTransitions`. Linux uses a client header, `WindowDecorations.None`, and no `ExtendClientArea` path (Avalonia/X11 lesson). Fullscreen and maximize actions reapply chrome after the state change.
 - `ThemePlatformAdapter` maps fonts and opacity. Acrylic/Mica stay Windows materials. Generated Linux profiles default to Adwaita Mono.
 - `dt --diagnose-desktop` documents the GNOME Settings custom-shortcut setup for summon/quake via `dt -w`.
 - Unit coverage is in `WindowChromeTests` and `ThemePlatformAdapterTests`. Live Mutter UI gate is still open.

## Sequencing

Ship in this order unless a dependency forces a swap:

1. Window state + GNOME chrome backend  
2. Theme / type / materials adapter  
3. Freedesktop integration depth (summon story + notification actions first)  
4. Shell integration provider  
5. Engine capability polish (ongoing, can interleave)  
6. Live GNOME UI + WT fixture gates wired to parity-status  

Do not start distro-wide default-terminal expansion or file-manager plugins before
the header and window state story is solid. Chrome bugs read as "Linux is broken"
even when the PTY is fine.

## Design rules for patches

 - Prefer a backend interface over another `OperatingSystem.IsLinux()` branch in
   `MainWindow`.
 - Keep WT action names and settings keys. Map presentation; do not rename the
   product language for GNOME users of WT settings.
 - Capability-report anything the session cannot do. No silent command palette
   dead ends.
 - NativeAOT stays green. No reflection-heavy portal stacks without a clear
   boundary and tests.
 - GNOME is the design reference on Linux. KDE/other desktops get the same
   backends and honest fallbacks, not a second visual identity in v1.
 - Update parity-status when behavior changes. Update this file when the plan or
   north star changes.

## Acceptance snapshots

Use these as review questions, not as a marketing checklist.

**WT familiar**

 - Can a WT user hit their muscle-memory chords for tabs, panes, palette, and
   search without relearning names?
 - Does a supported WT settings file produce the same action map?
 - Do CLI window and tab targeting still go through the broker the same way?

**GNOME native**

 - Does the running app look like it belongs next to other GNOME apps on Mutter?
 - Is there a single header story with tabs, not a Windows titlebar cosplay?
 - Do install, notifications, and open-URI paths use the desktop the way GNOME
   software is supposed to?
 - When something is impossible on the session, does the app say so in plain
   language?

When both sets of answers are yes on x64 GNOME, and the package gates stay green
on x64 and ARM64, Linux has met this roadmap's bar. Everything past that is
polish or a new roadmap.
