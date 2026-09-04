using Devolutions.Terminal.Shell;
using Xunit;

namespace Devolutions.Terminal.Shell.Abstractions.Tests;

public sealed class ShellSelectorTests
{
    [Fact]
    public void Resolve_forces_gtk_from_equals_form()
    {
        var selection = ShellSelector.Resolve(
            ["--shell=gtk", "nt"],
            environment: EmptyDesktop(),
            gtkShellEnabled: true,
            qtShellEnabled: false);

        Assert.Equal(ShellKind.Gtk, selection.Kind);
        Assert.True(selection.Forced);
        Assert.Contains("forced Gtk", selection.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_forces_avalonia_from_space_form()
    {
        var selection = ShellSelector.Resolve(
            ["--shell", "avalonia"],
            environment: Desktop("GNOME"),
            gtkShellEnabled: true,
            qtShellEnabled: false);

        Assert.Equal(ShellKind.Avalonia, selection.Kind);
        Assert.True(selection.Forced);
    }

    [Fact]
    public void Resolve_maps_gnome_desktop_to_gtk_when_enabled()
    {
        var selection = ShellSelector.Resolve(
            args: null,
            environment: Desktop("GNOME"),
            gtkShellEnabled: true,
            qtShellEnabled: false);

        Assert.Equal(ShellKind.Gtk, selection.Kind);
        Assert.False(selection.Forced);
        Assert.Equal("GNOME", selection.Desktop);
    }

    [Fact]
    public void Resolve_falls_back_when_gtk_disabled_on_gnome()
    {
        var selection = ShellSelector.Resolve(
            args: null,
            environment: Desktop("ubuntu:GNOME"),
            gtkShellEnabled: false,
            qtShellEnabled: false);

        Assert.Equal(ShellKind.Avalonia, selection.Kind);
        Assert.Contains("GTK shell is disabled", selection.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_defers_qt_on_plasma()
    {
        var selection = ShellSelector.Resolve(
            args: null,
            environment: Desktop("KDE"),
            gtkShellEnabled: true,
            qtShellEnabled: false);

        Assert.Equal(ShellKind.Avalonia, selection.Kind);
        Assert.Contains("Qt shell is deferred", selection.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_can_enable_qt_when_flag_on()
    {
        var selection = ShellSelector.Resolve(
            args: null,
            environment: Desktop("KDE"),
            gtkShellEnabled: true,
            qtShellEnabled: true);

        Assert.Equal(ShellKind.Qt, selection.Kind);
    }

    [Fact]
    public void Resolve_forced_qt_falls_back_while_deferred()
    {
        var selection = ShellSelector.Resolve(
            ["--shell=qt"],
            environment: EmptyDesktop(),
            gtkShellEnabled: true,
            qtShellEnabled: false);

        Assert.Equal(ShellKind.Avalonia, selection.Kind);
        Assert.True(selection.Forced);
        Assert.Contains("deferred", selection.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_reads_dt_shell_environment()
    {
        var env = Desktop("GNOME");
        env[ShellSelector.EnvironmentVariableName] = "avalonia";

        var selection = ShellSelector.Resolve(
            args: null,
            environment: env,
            gtkShellEnabled: true,
            qtShellEnabled: false);

        Assert.Equal(ShellKind.Avalonia, selection.Kind);
        Assert.True(selection.Forced);
    }

    [Fact]
    public void StripShellArgs_removes_both_forms()
    {
        var stripped = ShellSelector.StripShellArgs(["--shell=gtk", "nt", "--shell", "avalonia", "-w", "0"]);
        Assert.Equal(["nt", "-w", "0"], stripped);
    }

    [Fact]
    public void Resolve_throws_on_unknown_shell()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ShellSelector.Resolve(["--shell=motif"], EmptyDesktop()));
        Assert.Contains("Unknown shell", ex.Message, StringComparison.Ordinal);
    }

    private static Dictionary<string, string?> EmptyDesktop() => new(StringComparer.Ordinal)
    {
        ["XDG_CURRENT_DESKTOP"] = null,
        ["XDG_SESSION_DESKTOP"] = null,
        ["DESKTOP_SESSION"] = null,
    };

    private static Dictionary<string, string?> Desktop(string value)
    {
        var map = EmptyDesktop();
        map["XDG_CURRENT_DESKTOP"] = value;
        return map;
    }
}
