# Linux native shells (spike)

Personal spike track. Goal: keep Devolutions Terminal cross-platform and
Windows Terminal-familiar, and stop painting fake desktop chrome with Avalonia
on Linux. GNOME gets real GTK4/libadwaita. KDE Plasma gets real Qt. The shared
engine, settings, actions, and PTY stack stay one codebase.

Avalonia remains the shell on Windows and macOS until those platforms get their
own native story. Avalonia is not the long-term Linux presentation layer.

Related: [linux-parity-roadmap.md](linux-parity-roadmap.md),
[parity-status.md](parity-status.md).

## Why

Side-by-side screenshots on 2026-09-04 (Devolutions Terminal vs GNOME Terminal,
Calculator, Text Editor, Files, Settings) settled the materials question.

Layout can match. Skia cannot become GSK or Qt Quick Scene Graph. More gradient
tweaks will not close that gap. Linking the toolkit the desktop already ships is
the honest path to "looks native."

This is a presentation swap on Linux. Behavior stays WT-shaped everywhere.

## Toolkit fact (do not unlearn)

Phase 0 already uses **real** GTK4/libadwaita through GirCore (`Adw-1.dll`,
`Gtk-4.0.dll` P/Invoke into `libadwaita-1.so` / `libgtk-4.so`). There is no
Avalonia window on the `Devolutions.Terminal.Shell.Gtk` path.

What felt like "fake GNOME" in screenshot loops was mostly two mistakes:

1. **Wrong reference app.** Fedora's `gnome-terminal` 3.56 still links
   **GTK3 + VTE** (`libgtk-3.so`). Its notebook tabs, blue underline, and
   neutral gray strip are GTK3 materials. Our shell is GTK4 + libadwaita.
   Those two visual languages are not the same. Fair chrome references on
   this desktop are Adwaita apps: Text Editor, Loupe, Papers, Nautilus.
   `gnome-console` (GTK4 terminal) is the peer when installed.
2. **Paint reflex.** CSS color overrides and pixel-matching GNOME Terminal
   recreated the Avalonia uncanny-valley loop on top of a real toolkit.
   Hybrid rule: stock Adw/Gtk widgets only for chrome. Theme owns texture.
   No application CSS for header/tab fills. Content host stays unstyled until
   the frame sink lands.

If DT and Text Editor disagree on materials, that is a bug. If DT and
GTK3 GNOME Terminal disagree on materials, that is expected.

## North star (refined)

Three promises, held at the same time:

1. **Cross-platform product.** One settings model, one action catalog, one
   broker/CLI shape, one VT/connection stack. A contributor does not fork the
   terminal for each OS.
2. **Instant WT familiarity.** Tabs, panes, keybindings, command palette,
   profiles, schemes, `settings.json` names. A Windows Terminal user should not
   need a second manual for chords and tab behavior.
3. **Real native Linux chrome.**
   - On GNOME (and other GTK desktops that speak libadwaita): GTK4 + libadwaita
     draw the window, header, tabs, menus, dialogs, about, shortcuts.
   - On KDE Plasma (and other Qt desktops): Qt 6 draws the same shell roles with
     Breeze / Plasma styling.
   - No Avalonia "Adwaita-ish" or "Breeze-ish" theme as the ship path on those
     desktops. Inspired paint is interim only.

If behavior and presentation fight: **behavior keeps WT names and semantics.**
**Presentation maps into the active desktop toolkit.**

## What is not the plan

 - One process loading GTK and Qt at once. Pick one shell per process.
 - Avalonia `NativeControlHost` with GTK widgets glued on top of a Skia window
   as the primary design. Wrong polarity. Native toolkit owns the toplevel.
 - A Linux-only fork of settings keys or action names.
 - Pixel-debugging Avalonia header materials after the native shell exists.
 - Waiting on WinUI OSS. WinUI is Windows composition. It does not draw Adwaita
   or Breeze.
 - Replacing the terminal engine with VTE or Konsole's emulator. We keep our
   engine (built-in / Ghostty VT path) and host it's surface inside the native
   shell.

## Layer cake

```
+----------------------------------------------------------+
| Host entry (Devolutions.Terminal)                        |
|  detect desktop -> select IShellFrontend                 |
+----------------------+-----------------------------------+
                       |
     +-----------------+------------------+
     |                 |                  |
 AvaloniaShell    GtkShellFrontend   QtShellFrontend
 (Win, macOS,     (GNOME / GTK)      (Plasma / Qt)
  Linux fallback)
     |                 |                  |
     +--------+--------+---------+--------+
              |                  |
              v                  v
     +------------------------------------+
     | Shared app services                |
     |  actions, keybindings, tabs/panes  |
     |  settings, profiles, broker, CLI   |
     +------------------------------------+
              |
              v
     +------------------------------------+
     | Terminal surface                   |
     |  cell grid, input, selection,      |
     |  search-in-buffer, Skia/GPU draw   |
     |  (toolkit-agnostic frame sink)     |
     +------------------------------------+
              |
              v
     +------------------------------------+
     | Engines / PTY / connections        |
     |  Ghostty VT, built-in, dt-pty-host |
     +------------------------------------+
```

### Shared (stay managed, stay one copy)

 - `Devolutions.Terminal.Core`, `.Settings`, `.Connection`, `.Ghostty`, `.Broker`,
   `.Cli`, package/desktop metadata helpers
 - Action dispatcher and keybinding resolution
 - Tab/pane tree model (not the tab *widget*)
 - Profile and scheme loading
 - Terminal interaction model (selection, search session, input routing policy)

### Shell frontends (replaceable)

Each frontend implements a small interface surface, names flexible:

 - `IShellFrontend`
   - Run main loop
   - Open/close windows
   - Bind window chrome to the tab/pane model (new tab, profile menu, app menu,
     find, close, fullscreen)
 - `IShellWindow`
   - Title, focus, fullscreen/maximize where the desktop allows
   - Content host allocation (size, scale factor, DPR)
 - `ITerminalFrameSink` / host
   - Accept a software or GPU frame from the shared terminal surface
   - Forward key, text, pointer, and IME events into the shared control

GTK and Qt frontends are Linux-only projects. They reference shared libraries.
They do not reference Avalonia.

Avalonia frontend keeps Windows and macOS, and a Linux **fallback** when neither
GTK nor Qt shell is built, or the user forces `--shell=avalonia`.

### Terminal surface (the seam that makes this possible)

Today `TermControl` is an Avalonia control. That couples grid painting to
Avalonia's visual tree.

Spike direction:

1. Extract drawing and input to a toolkit-neutral core (most of this already
   lives next to `TerminalSkiaDrawOperation` and the interaction model).
2. Define `ITerminalView`:
   - Resize(cols, rows, cellSize, scale)
   - Present(frame) or Render(canvas abstraction)
   - Events: Key, Text, Pointer, Wheel, Focus, Ime
3. Avalonia `TermControl` becomes one host adapter.
4. GTK host: `GtkDrawingArea` / paintable / GLArea adapter.
5. Qt host: `QWidget` / `QQuickItem` / `QOpenGLWidget` adapter.

Until that seam exists, any "hybrid" is a demo that blits one frame. The seam is
the real work.

## Desktop selection

Runtime, not compile-only:

| Signal | Shell |
|--------|--------|
| `XDG_CURRENT_DESKTOP` contains `GNOME`, `Unity`, `Pantheon`, `Cinnamon` (policy TBD per DE) | GTK |
| contains `KDE` | Qt |
| `DT_SHELL=gtk\|qt\|avalonia` or `--shell=` | force |
| unknown / headless / missing native libs | Avalonia fallback or error with a clear message |

Packaging choices:

 - **Fat Linux build:** ships GTK and Qt shell code; loads one set of native libs
   at runtime. Larger, one tarball.
 - **Split packages:** `devolutions-terminal`, `...-gtk`, `...-qt`. Leaner, more
   distro-honest.
 - Flatpak: pick runtime (`org.gnome.Platform` vs KDE runtime) per package id, or
   two Flatpaks.

Spike can start fat and local. Distro split is a packaging decision later.

Never initialize GTK and Qt in the same process. Selection happens before any
toolkit `init`.

## GNOME / GTK shell

**Stack:** GTK 4 + libadwaita 1, Gir.Core (or equivalent maintained bindings).

**Owns:**

 - `AdwApplication` / `AdwApplicationWindow`
 - `AdwHeaderBar`: new tab, profile menu, title, find, app menu
 - Tab bar: `AdwTabView` / `AdwTabBar` or custom tabs wired to the shared tree
 - Menus and popovers (`Gio.Menu`, symbolic icons from the icon theme)
 - About, shortcuts window, message dialogs
 - CSD and window state through GTK/Mutter

**Does not own:** cell rendering, VT, PTY, settings schema.

**Main loop:** GLib is primary. Shared services marshal onto it. No second UI
thread fighting GTK.

**NativeAOT:** treat as a hard spike gate. Gir.Core + GObject + trim needs roots
and `LibraryImport` discipline early, not after the UI is "done."

## KDE / Qt shell

**Stack:** Qt 6 Widgets or Qt Quick, Breeze-aware. Bindings candidates to evaluate
in order during spike:

1. Official Qt/.NET bridge direction (`qt/qtbridge-csharp` / QtDotNet lineage)
2. Maintained community bindings if the official bridge is host-Qt-first and
   awkward for " .NET owns process, Qt owns windows"
3. Thin C++ shell process + IPC only if in-process bindings fail AOT or lifetime
   rules (last resort; hurts single-process mental model)

**Owns:**

 - Main window and title bar / unified menu bar per Plasma HIG
 - Tab bar, toolbuttons for new tab / profile / app menu / find
 - Standard dialogs, about, config chrome later
 - Wayland and X11 through Qt's platform abstraction

**Plasma notes:**

 - Follow KDE human interface patterns, not a painted copy of Adwaita.
 - Header and tab placement can mirror the GNOME *information architecture*
   (new tab, profiles, app menu, tabs, content) without cloning GNOME widgets.
 - Server-side decorations vs CSD: obey the desktop; do not force GNOME CSD
   habits onto Plasma.

**Main loop:** `QCoreApplication` / `QApplication` is primary when Qt shell runs.

Same rule: one toolkit init per process.

## Windows and macOS

Unchanged in this spike:

 - Avalonia shell, existing chrome policies
 - Same shared core and terminal surface adapters

Later optional tracks (out of this doc's commit scope): WinUI host on Windows,
AppKit host on macOS. Same `IShellFrontend` idea. Not required to prove Linux.

## Cross-platform abilities (what we refuse to break)

 - One `settings.json` language
 - One action names table and keybinding resolver
 - `dt` / `wt`-shaped CLI and broker multi-window routing
 - Profile generators and dynamic Linux shells
 - Engine selection and capability reports
 - Test projects that assert behavior without spinning GTK/Qt where possible

Shell frontends get **contract tests** against a fake `IShellFrontend` and
**live tests** on real GNOME and Plasma sessions.

## Phased spike plan

Personal spike. Exit any phase that fails a gate instead of layering hope.

### Phase 0 - Prove the outer window (GTK first)

 - New project sketch: `src/Devolutions.Terminal.Shell.Gtk` (throwaway OK)
 - Gir.Core Adw window, header with + / menu, empty content
 - Runs on Wayland GNOME, scale 1 and 2
 - NativeAOT publish smoke
 - Screenshot next to GNOME Terminal chrome (header only)

**Gate:** strangers cannot tell the header toolkit apart in a crop. If bindings or
AOT collapse, stop and write the failure down.

### Phase 1 - Frame sink

 - Toolkit-neutral present path from existing Skia draw ops into a GTK content
   widget (software buffer first)
 - Keys and pointer into the interaction model
 - One local PTY session, one tab

**Gate:** usable typing and resize at 60Hz class feel on a real session. Not a
benchmark contest. Just not janky.

### Phase 2 - Wire shared app services

 - New tab, profile list, app menu actions call the real dispatcher
 - Tab model drives Adw tabs
 - Command palette: acceptable as content overlay or Adw dialog; WT chords stay

**Gate:** WT fixture keybindings for new tab / close / split (if split hosted)
match behavior tests.

### Phase 3 - Qt twin (DEFERRED)

Held on purpose as of 2026-09-04. Do not start `Shell.Qt` until GTK hosts a
real terminal surface and we reopen Plasma. Container Qt6 deps can stay.

### Phase 4 - Host selection and packaging

 - Desktop detection + `--shell=` / `DT_SHELL` (selector exists; auto-GTK not
   default for daily driver yet)
 - Build container deps for GTK and Qt (done in azurelinux-containers)
 - Fallback policy documented
 - Avalonia Linux path marked deprecated for interactive desktop use only after
   GTK Phase 2 gate

### Phase 5 - Settings and dialogs (native)

 - Prefer native preference surfaces per toolkit, still bound to shared settings
   model
 - Or keep Avalonia settings window as a child process longer if in-process is
   painful. Child process is allowed. Purity is not the goal. Native chrome is.

## Interim Avalonia Linux UI

Until a phase gate ships for daily use:

 - Keep the accepted Avalonia GNOME-*shaped* layout (header, + / chevron /
   hamburger, close-only, tabs under header)
 - Do not invest in further fake material polish
 - `AdwaitaChrome` tokens stay as interim only; delete when Gtk shell owns paint

## Repo shape (target)

```
src/
  Devolutions.Terminal/                 # host, shell selection
  Devolutions.Terminal.App/             # shared window/tab/pane services
                                        # (shed Avalonia-only types over time)
  Devolutions.Terminal.Shell.Avalonia/  # optional split from App later
  Devolutions.Terminal.Shell.Gtk/       # spike -> product
  Devolutions.Terminal.Shell.Qt/        # spike -> product
  Devolutions.Terminal.Control/         # evolve toward toolkit-neutral surface
  ...
docs/
  linux-native-shells.md                # this file
  linux-parity-roadmap.md               # product bar; points here for shells
```

Do not boil the ocean renaming everything on day one. Phase 0 can live as a
side project under `src/` that only links Core/Render experiments.

## Risks (read these before falling in love)

 - **Two native stacks forever on Linux.** Cost is real. The alternative is
   uncanny valley forever. Pick the cost on purpose.
 - **Bindings quality and AOT.** GTK via Gir.Core and Qt via bridge are both
   less boring than Avalonia's managed path. Phase 0 AOT gate exists for a reason.
 - **IME, accessibility, and clipboard.** Each toolkit has it's own path. Shared
   policy, native calls.
 - **Split / pane hosting.** Harder than single terminal content. Do single tab
   content before mosaic panes in the native host.
 - **Non-GNOME GTK and non-Plasma Qt.** Best effort through the same frontends.
   Capability reports beat silent wrong chrome.
 - **Contributor surface.** Document which shell to run for which bug. CI needs
   at least headless contract tests; live DE tests stay manual or dedicated runners.

## Success criteria

 - Blind crop of the header on GNOME matches libadwaita apps, not Avalonia.
 - Blind crop on Plasma matches a Qt app, not a painted theme.
 - Same WT settings fixture drives tabs and keys on GTK shell, Qt shell, and
   Avalonia Windows.
 - `dotnet publish` NativeAOT still produces a runnable Linux binary for the
   shells we claim.
 - Cross-platform core stays one tree. No `linux-only` settings schema.

## Decision log

 - 2026-09-04: Avalonia materials declared insufficient for native Linux chrome.
 - 2026-09-04: WinUI OSS ruled irrelevant to GNOME/KDE presentation.
 - 2026-09-04: Personal spike direction set to real GTK + real Qt shells, shared
   WT behavior core, Avalonia retained for Win/mac and Linux fallback.
 - 2026-09-04: **Qt / Plasma held.** Build container still stages Qt6 headers.
   Product work is GTK Phase 0 only until that spike is reopened on purpose.
 - 2026-09-04: Phase 0 code lands as `Devolutions.Terminal.Shell.Abstractions`
   (`ShellSelector`) and `Devolutions.Terminal.Shell.Gtk` (Adw header + tab bar
   + placeholder surface). Host: `dt --shell=gtk` or `DT_SHELL=gtk` on a Linux
   build with `ENABLE_GTK_SHELL`. Auto GNOME map does not leave Avalonia yet.
   Standalone: `dotnet run --project src/Devolutions.Terminal.Shell.Gtk`.
 - 2026-09-04: Stop chrome rabbit holes. Structural native rules below. Product
   name only in About / desktop metadata.

When a phase gate fails, write it here with the date and the evidence. Do not
quietly return to gradient tweaks.

## Native chrome contract (structural)

GNOME Terminal is not a theme. It is a small set of rules. Copy the rules, not
the pixels.

1. **Session owns the title.** Window title and tab label are the same session
   string (`user@host:cwd`, OSC title, profile name after spawn). Never the
   product name. Never a marketing subtitle.
2. **Product name is About-only.** Also `.desktop` Name, AppStream, installer.
   Not header, not tabs, not content placeholders, not temporary chrome labels.
3. **Stock Adwaita composition.** `AdwApplicationWindow` + `AdwHeaderBar` +
   `AdwTabView`/`AdwTabBar` + content. No custom header materials, no painted
   wells, no flat-vs-linked experiments as a substitute for the right tree.
4. **Header IA matches the desktop app class.** GNOME Terminal: `+`, find, app
   menu, close. Profile pickers live in the menu (or later a proper split that
   still reads as one new-tab control). WT profile muscle memory stays in the
   action/menu model, not as a foreign second brand strip.
5. **Content is a terminal hole.** Full-bleed session surface. Empty black until
   the frame sink/PTY is hosted. No on-canvas "phase" copy.
6. **Acting native beats looking tuned.** A real PTY in that hole will do more
   for "not fake" than another CSS pass. Phase 1 is the structural next step.

Anti-pattern: tweak radii, gradients, and button classes while the title still
says the company name and the content is a placeholder card. That is cosplay.
