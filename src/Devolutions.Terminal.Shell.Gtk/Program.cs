using Devolutions.Terminal.Shell;
using Devolutions.Terminal.Shell.Gtk;

// Standalone entry for Phase 0. The main host can also call GtkShellApplication.Run.
var selection = ShellSelector.Resolve(
    args,
    gtkShellEnabled: true,
    qtShellEnabled: false);

if (selection.Kind is not ShellKind.Gtk and not ShellKind.Avalonia)
{
    Console.Error.WriteLine($"dt-gtk: refusing shell {selection.Kind}: {selection.Reason}");
    return 2;
}

if (selection.Kind == ShellKind.Avalonia && selection.Forced)
{
    Console.Error.WriteLine($"dt-gtk: {selection.Reason}");
    Console.Error.WriteLine("dt-gtk: this binary only runs the GTK frontend. Use Devolutions.Terminal for Avalonia.");
    return 2;
}

Console.Error.WriteLine($"dt-gtk: {selection.Reason}");
return GtkShellApplication.Run(ShellSelector.StripShellArgs(args));
