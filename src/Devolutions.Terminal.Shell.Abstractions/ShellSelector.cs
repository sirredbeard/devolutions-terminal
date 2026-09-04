namespace Devolutions.Terminal.Shell;

/// <summary>
/// Picks a shell frontend before any UI toolkit init.
/// Qt auto-select is recorded but not enabled until that spike ships.
/// </summary>
public static class ShellSelector
{
    public const string EnvironmentVariableName = "DT_SHELL";
    public const string CliOptionPrefix = "--shell=";
    public const string CliOptionName = "--shell";

    /// <summary>
    /// When false, Plasma/KDE auto paths fall back to Avalonia instead of Qt.
    /// Phase 0 keeps this false on purpose.
    /// </summary>
    public static bool QtShellEnabled { get; set; }

    /// <summary>
    /// When false, GNOME auto paths fall back to Avalonia instead of GTK.
    /// Host builds that ship the GTK project set this true.
    /// </summary>
    public static bool GtkShellEnabled { get; set; } = true;

    public static ShellSelection Resolve(
        IReadOnlyList<string>? args = null,
        IReadOnlyDictionary<string, string?>? environment = null,
        bool? gtkShellEnabled = null,
        bool? qtShellEnabled = null)
    {
        var gtkEnabled = gtkShellEnabled ?? GtkShellEnabled;
        var qtEnabled = qtShellEnabled ?? QtShellEnabled;
        var env = environment ?? CaptureEnvironment();

        if (TryGetForcedKind(args, env, out var forced, out var forcedSource))
        {
            return ValidateForced(forced, forcedSource, gtkEnabled, qtEnabled, env);
        }

        var desktop = GetDesktopHint(env);
        if (IsGtkDesktop(desktop))
        {
            if (gtkEnabled)
            {
                return new ShellSelection(
                    ShellKind.Gtk,
                    Forced: false,
                    desktop,
                    $"Desktop '{desktop}' maps to GTK.");
            }

            return new ShellSelection(
                ShellKind.Avalonia,
                Forced: false,
                desktop,
                $"Desktop '{desktop}' prefers GTK, but GTK shell is disabled; using Avalonia.");
        }

        if (IsQtDesktop(desktop))
        {
            if (qtEnabled)
            {
                return new ShellSelection(
                    ShellKind.Qt,
                    Forced: false,
                    desktop,
                    $"Desktop '{desktop}' maps to Qt.");
            }

            return new ShellSelection(
                ShellKind.Avalonia,
                Forced: false,
                desktop,
                $"Desktop '{desktop}' prefers Qt, but Qt shell is deferred; using Avalonia.");
        }

        return new ShellSelection(
            ShellKind.Avalonia,
            Forced: false,
            desktop,
            desktop is null
                ? "No desktop hint; default Avalonia shell."
                : $"Desktop '{desktop}' has no native shell mapping; default Avalonia.");
    }

    /// <summary>
    /// Pulls <c>--shell</c> / <c>--shell=</c> tokens out of argv for the real CLI parser.
    /// </summary>
    public static string[] StripShellArgs(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Count == 0)
        {
            return [];
        }

        var kept = new List<string>(args.Count);
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (arg.StartsWith(CliOptionPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(arg, CliOptionName, StringComparison.Ordinal))
            {
                if (i + 1 < args.Count)
                {
                    i++;
                }

                continue;
            }

            kept.Add(arg);
        }

        return kept.ToArray();
    }

    public static bool TryParseKind(string? value, out ShellKind kind)
    {
        kind = ShellKind.Avalonia;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "avalonia":
            case "ava":
            case "default":
                kind = ShellKind.Avalonia;
                return true;
            case "gtk":
            case "gnome":
            case "adwaita":
            case "libadwaita":
                kind = ShellKind.Gtk;
                return true;
            case "qt":
            case "kde":
            case "plasma":
                kind = ShellKind.Qt;
                return true;
            default:
                return false;
        }
    }

    private static ShellSelection ValidateForced(
        ShellKind forced,
        string source,
        bool gtkEnabled,
        bool qtEnabled,
        IReadOnlyDictionary<string, string?> env)
    {
        var desktop = GetDesktopHint(env);
        if (forced == ShellKind.Gtk && !gtkEnabled)
        {
            return new ShellSelection(
                ShellKind.Avalonia,
                Forced: true,
                desktop,
                $"{source} requested GTK, but GTK shell is not enabled in this build; using Avalonia.");
        }

        if (forced == ShellKind.Qt && !qtEnabled)
        {
            return new ShellSelection(
                ShellKind.Avalonia,
                Forced: true,
                desktop,
                $"{source} requested Qt, but Qt shell is deferred; using Avalonia.");
        }

        return new ShellSelection(
            forced,
            Forced: true,
            desktop,
            $"{source} forced {forced}.");
    }

    private static bool TryGetForcedKind(
        IReadOnlyList<string>? args,
        IReadOnlyDictionary<string, string?> env,
        out ShellKind kind,
        out string source)
    {
        kind = ShellKind.Avalonia;
        source = string.Empty;

        if (args is not null)
        {
            for (var i = 0; i < args.Count; i++)
            {
                var arg = args[i];
                if (arg.StartsWith(CliOptionPrefix, StringComparison.Ordinal))
                {
                    var value = arg[CliOptionPrefix.Length..];
                    if (!TryParseKind(value, out kind))
                    {
                        throw new ArgumentException($"Unknown shell '{value}'. Use avalonia, gtk, or qt.");
                    }

                    source = CliOptionPrefix + value;
                    return true;
                }

                if (string.Equals(arg, CliOptionName, StringComparison.Ordinal))
                {
                    if (i + 1 >= args.Count)
                    {
                        throw new ArgumentException("--shell requires a value (avalonia, gtk, or qt).");
                    }

                    var value = args[++i];
                    if (!TryParseKind(value, out kind))
                    {
                        throw new ArgumentException($"Unknown shell '{value}'. Use avalonia, gtk, or qt.");
                    }

                    source = $"{CliOptionName} {value}";
                    return true;
                }
            }
        }

        if (env.TryGetValue(EnvironmentVariableName, out var envValue) &&
            !string.IsNullOrWhiteSpace(envValue))
        {
            if (!TryParseKind(envValue, out kind))
            {
                throw new ArgumentException(
                    $"Unknown {EnvironmentVariableName}='{envValue}'. Use avalonia, gtk, or qt.");
            }

            source = $"{EnvironmentVariableName}={envValue}";
            return true;
        }

        return false;
    }

    private static string? GetDesktopHint(IReadOnlyDictionary<string, string?> env)
    {
        if (env.TryGetValue("XDG_CURRENT_DESKTOP", out var current) &&
            !string.IsNullOrWhiteSpace(current))
        {
            return current.Trim();
        }

        if (env.TryGetValue("XDG_SESSION_DESKTOP", out var session) &&
            !string.IsNullOrWhiteSpace(session))
        {
            return session.Trim();
        }

        if (env.TryGetValue("DESKTOP_SESSION", out var legacy) &&
            !string.IsNullOrWhiteSpace(legacy))
        {
            return legacy.Trim();
        }

        return null;
    }

    private static bool IsGtkDesktop(string? desktop)
    {
        if (string.IsNullOrWhiteSpace(desktop))
        {
            return false;
        }

        foreach (var part in desktop.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (part.ToUpperInvariant())
            {
                case "GNOME":
                case "UNITY":
                case "PANTHEON":
                case "COSMIC":
                case "BUDGIE":
                case "CINNAMON":
                case "XFCE":
                case "MATE":
                    return true;
            }
        }

        return false;
    }

    private static bool IsQtDesktop(string? desktop)
    {
        if (string.IsNullOrWhiteSpace(desktop))
        {
            return false;
        }

        foreach (var part in desktop.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (part.ToUpperInvariant())
            {
                case "KDE":
                case "PLASMA":
                case "LXQT":
                    return true;
            }
        }

        return false;
    }

    private static IReadOnlyDictionary<string, string?> CaptureEnvironment()
    {
        var map = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var key in new[]
                 {
                     EnvironmentVariableName,
                     "XDG_CURRENT_DESKTOP",
                     "XDG_SESSION_DESKTOP",
                     "DESKTOP_SESSION",
                 })
        {
            map[key] = Environment.GetEnvironmentVariable(key);
        }

        return map;
    }
}
