using Avalonia;
using Devolutions.Terminal.Broker;
using Devolutions.Terminal.Cli;
using Devolutions.Terminal.Package;
using Devolutions.Terminal.Shell;

namespace Devolutions.Terminal;

internal static class Program
{
#if ENABLE_GTK_SHELL
    private const bool IsGtkShellBuilt = true;
#else
    private const bool IsGtkShellBuilt = false;
#endif

    [STAThread]
    public static int Main(string[] args)
    {
        CliInvocation? directActivation = null;
        if (args is ["--toast-activation", var encodedActivation])
        {
            if (!ToastActivationCodec.TryParse(
                    encodedActivation,
                    out var activation,
                    out var activationError))
            {
                Console.Error.WriteLine($"dt: {activationError}");
                return 2;
            }

            directActivation = new(
                activation!.TargetWindow,
                null,
                null,
                null,
                null,
                CliLaunchMode.Focus,
                null,
                []);
        }
        else if (
            Devolutions.Terminal.App.Platform.LinuxDesktopIntegration.TryNormalizeProtocolActivation(
                args,
                out var protocolArgs,
                out var protocolError))
        {
            if (protocolError is not null)
            {
                Console.Error.WriteLine($"dt: {protocolError}");
                return 2;
            }

            args = protocolArgs;
        }

        // Shell frontend must be chosen before Avalonia (or GTK) init.
        // Phase 0: GTK only when forced (--shell=gtk / DT_SHELL=gtk). Auto desktop
        // mapping stays Avalonia until the GTK surface hosts a real terminal.
        ShellSelection shellSelection;
        try
        {
            shellSelection = ShellSelector.Resolve(
                args,
                gtkShellEnabled: IsGtkShellBuilt,
                qtShellEnabled: false);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"dt: {ex.Message}");
            return 2;
        }

        args = ShellSelector.StripShellArgs(args);

        if (args is ["--diagnose-desktop"])
        {
            Console.Out.WriteLine(new Devolutions.Terminal.App.Platform.PlatformLauncher()
                .GetCapabilityReport());
            Console.Out.WriteLine($"shell: {shellSelection.Kind} ({shellSelection.Reason})");
            Console.Out.WriteLine($"shell-gtk-built: {IsGtkShellBuilt}");
            Console.Out.WriteLine($"shell-qt-enabled: false");
            return 0;
        }

        // Phase 0: only an explicit force enters the GTK frontend. Auto desktop
        // mapping may report Gtk for diagnostics, but Avalonia still owns the
        // daily-driver window until the terminal surface is hosted.
        if (shellSelection.Kind == ShellKind.Gtk && shellSelection.Forced)
        {
#if ENABLE_GTK_SHELL
            Console.Error.WriteLine($"dt: {shellSelection.Reason}");
            return Devolutions.Terminal.Shell.Gtk.GtkShellApplication.Run(args);
#else
            Console.Error.WriteLine(
                "dt: GTK shell was requested, but this binary was built without ENABLE_GTK_SHELL. " +
                "Run project Devolutions.Terminal.Shell.Gtk, or rebuild the host on Linux.");
            return 2;
#endif
        }

        if (shellSelection.Kind == ShellKind.Qt && shellSelection.Forced)
        {
            Console.Error.WriteLine($"dt: {shellSelection.Reason}");
            Console.Error.WriteLine("dt: Qt/Plasma shell is deferred. Use --shell=avalonia or --shell=gtk.");
            return 2;
        }

        var parsed = directActivation is null
            ? new CliParser().Parse(args)
            : new CliParseResult(0, "Validated toast activation.", false, directActivation);
        if (parsed.ShouldExit)
        {
            var writer = parsed.ExitCode == 0 ? Console.Out : Console.Error;
            writer.WriteLine(parsed.Message);
            return parsed.ExitCode;
        }

        var invocation = parsed.Invocation!;
        var deferredHandler = new DeferredBrokerHandler();
        var broker = BrokerHost.TryCreate(deferredHandler);
        if (broker is null)
        {
            var response = ForwardToPrimaryAsync(invocation).AsTask().GetAwaiter().GetResult();
            if (!response.IsSuccess)
            {
                Console.Error.WriteLine($"dt: {response.Message}");
                return response.Status == BrokerStatus.WindowNotFound ? 3 : 1;
            }

            return 0;
        }

        if (invocation.TargetWindow.Equals("use-existing", StringComparison.OrdinalIgnoreCase) ||
            (int.TryParse(invocation.TargetWindow, out var requestedWindowId) && requestedWindowId > 0))
        {
            broker.DisposeAsync().AsTask().GetAwaiter().GetResult();
            Console.Error.WriteLine($"dt: terminal window '{invocation.TargetWindow}' was not found.");
            return 3;
        }

        if (invocation.SaveRequest is { Commandline.Length: > 0 } saveRequest)
        {
            broker.DisposeAsync().AsTask().GetAwaiter().GetResult();
            try
            {
                var settings = Devolutions.Terminal.Settings.SettingsService.Load();
                Devolutions.Terminal.Settings.SettingsSnippetStore.Add(
                    settings,
                    saveRequest.Name,
                    saveRequest.KeyChord,
                    saveRequest.Commandline);
                Devolutions.Terminal.Settings.SettingsService.Save(settings);
                if (OperatingSystem.IsWindows())
                {
                    var shellResult = new WindowsShellIntegrationClient().RefreshJumpList(
                        settings.Profiles
                            .Where(static profile => !profile.Hidden && !profile.Orphaned)
                            .Select(static profile => new JumpListProfile(
                                profile.Name,
                                profile.Guid ?? string.Empty,
                                profile.Icon)));
                    if (!shellResult.Succeeded)
                    {
                        Console.Error.WriteLine($"dt: settings saved; jump-list refresh unavailable: {shellResult.Diagnostic}");
                    }
                }
                return 0;
            }
            catch (Exception ex) when (ex is
                ArgumentException or
                IOException or
                UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"dt: {ex.Message}");
                return 1;
            }
        }

        TerminalApp.InitialInvocation = invocation;
        TerminalApp.BrokerHandler = deferredHandler;
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime([]);
            return 0;
        }
        finally
        {
            broker.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<TerminalApp>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
                RenderingMode =
                [
                    X11RenderingMode.Vulkan,
                    X11RenderingMode.Egl,
                    X11RenderingMode.Glx,
                    X11RenderingMode.Software,
                ],
                OverlayPopups = true,
                UseGLibMainLoop = true,
                UseDBusFilePicker = true,
                EnableIme = true,
                EnableSessionManagement = true,
                WmClass = "com.devolutions.Terminal",
            })
            .With(new SkiaOptions
            {
                MaxGpuResourceSizeBytes = 1024L * 1024 * 1024,
            });
#if DEBUG
        builder = builder.WithDeveloperTools();
#endif
        return builder.LogToTrace();
    }

    private static async ValueTask<BrokerResponse> ForwardToPrimaryAsync(CliInvocation invocation)
    {
        var client = new BrokerClient();
        BrokerResponse response = BrokerResponse.Unavailable("Broker endpoint is not ready.");
        for (var attempt = 0; attempt < 10; attempt++)
        {
            response = await client.SendAsync(
                invocation.TargetWindow,
                CliInvocationSerializer.Serialize(invocation),
                TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
            if (response.Status != BrokerStatus.Unavailable)
            {
                return response;
            }

            await Task.Delay(50).ConfigureAwait(false);
        }

        return response;
    }
}
