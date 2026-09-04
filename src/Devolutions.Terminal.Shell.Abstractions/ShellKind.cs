namespace Devolutions.Terminal.Shell;

/// <summary>
/// Presentation frontend for a process. One toolkit per process.
/// </summary>
public enum ShellKind
{
    /// <summary>Avalonia shell (Windows, macOS, Linux fallback).</summary>
    Avalonia = 0,

    /// <summary>GTK4 + libadwaita shell (GNOME and related desktops).</summary>
    Gtk = 1,

    /// <summary>Qt shell (Plasma). Deferred; selector may still name it.</summary>
    Qt = 2,
}
