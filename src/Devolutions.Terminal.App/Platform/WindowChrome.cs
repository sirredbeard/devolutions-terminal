using Avalonia;
using Avalonia.Controls;
using Devolutions.Terminal.Settings;

namespace Devolutions.Terminal.App.Platform;

/// <summary>
/// Host family for window chrome policy. Linux is the GNOME-shaped path:
/// client header, tabs below the header, no Win caption reserve.
/// </summary>
public enum WindowChromeHost
{
    Windows,
    MacOS,
    Linux,
}

/// <summary>
/// Resolved chrome layout for one ApplyWindowChrome pass. Pure data so tests
/// can lock GNOME and WT behavior without spinning a window.
/// </summary>
public sealed record WindowChromeLayout(
    bool ShowTitleBar,
    bool ShowTabStrip,
    bool ShowNewTabButton,
    bool ShowNewTabMenuButton,
    bool ShowMenuButton,
    bool ShowExitFullscreenButton,
    bool ShowClientCaptionButtons,
    bool ShowMinimizeCaption,
    bool ShowMaximizeCaption,
    bool ShowWindowTitle,
    bool ShowHeaderFind,
    bool TabsBelowHeader,
    bool ExtendClientAreaToDecorations,
    WindowDecorations WindowDecorations,
    double TitleBarHeight,
    Thickness TitleBarMargin,
    double TabStripTrailingReserve,
    double CornerRadius,
    bool CanResize,
    bool ClipToBounds,
    Thickness BorderThickness);

public static class WindowStateTransitions
{
    public static WindowState ToggleFullscreen(WindowState current) =>
        current == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;

    public static WindowState SetFullscreen(bool isFullscreen) =>
        isFullscreen ? WindowState.FullScreen : WindowState.Normal;

    public static WindowState ToggleMaximized(WindowState current) =>
        current switch
        {
            WindowState.Maximized => WindowState.Normal,
            WindowState.FullScreen => WindowState.Maximized,
            _ => WindowState.Maximized,
        };

    public static WindowState SetMaximized(bool isMaximized) =>
        isMaximized ? WindowState.Maximized : WindowState.Normal;
}

public static class WindowChrome
{
    public const double MacOsTrafficLightFallback = 70;
    public const double WindowsCaptionFallback = 138;
    public const double LinuxClientCaptionReserve = 120;
    // libadwaita HeaderBar is ~47px; keep a single CSD band, not a Win strip.
    public const double LinuxHeaderHeight = 47;
    public const double LinuxTabRowHeight = 38;
    public const double DefaultHeaderHeight = 40;
    // Mutter CSD default radius sits around here for normal windows.
    public const double LinuxCornerRadius = 12;
    // Real libadwaita dark tokens. Prefer AdwaitaChrome for brushes/shadows.
    public const string LinuxHeaderBackground = AdwaitaChrome.HeaderBg;
    public const string LinuxTabRowBackground = AdwaitaChrome.HeaderBg;

    // GNOME CSD default: close only. Maximize is double-click / Super+Up.

    public static WindowChromeHost DetectHost()
    {
        if (OperatingSystem.IsWindows())
        {
            return WindowChromeHost.Windows;
        }

        if (OperatingSystem.IsMacOS())
        {
            return WindowChromeHost.MacOS;
        }

        if (OperatingSystem.IsLinux())
        {
            return WindowChromeHost.Linux;
        }

        return WindowChromeHost.Windows;
    }

    public static bool ShouldShowTabRow(
        AppSettings settings,
        int tabCount,
        bool fullscreen)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (fullscreen && !settings.ShowTabsFullscreen)
        {
            return false;
        }

        return settings.AlwaysShowTabs || tabCount > 1;
    }

    /// <summary>
    /// GNOME HIG: tab bar can stay hidden for a single tab unless the user
    /// asked for always-on tabs. Fullscreen still respects showTabsFullscreen.
    /// </summary>
    public static bool ShouldShowTabStrip(
        AppSettings settings,
        int tabCount,
        bool fullscreen,
        WindowChromeHost host)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!ShouldShowTabRow(settings, tabCount, fullscreen))
        {
            return false;
        }

        _ = host;
        return true;
    }

    public static bool ShouldUseCustomTitlebar(
        AppSettings settings,
        bool embedded,
        bool macOS = false)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _ = macOS;
        return !embedded && settings.ShowTabsInTitlebar;
    }

    public static Thickness TitleBarContentMargin(
        bool fullscreen,
        bool macOS,
        bool windows,
        Thickness offScreenMargin,
        bool rightToLeft = false,
        bool linux = false,
        bool clientCaptions = false)
    {
        if (fullscreen)
        {
            return new Thickness(8, 0, 8, 0);
        }

        if (linux)
        {
            // GNOME header: start/end padding only. Captions and actions live in
            // the end cluster, not as a fake Win caption reserve on the margin.
            _ = clientCaptions;
            return new Thickness(10, 0, 10, 0);
        }

        if (macOS && !MacOsWindowControlsOnRight(offScreenMargin, rightToLeft))
        {
            return new Thickness(
                Math.Max(offScreenMargin.Left, MacOsTrafficLightFallback),
                0,
                8,
                0);
        }

        var captionRight = windows || macOS
            ? Math.Max(offScreenMargin.Right, WindowsCaptionFallback)
            : WindowsCaptionFallback;
        return new Thickness(8, 0, captionRight, 0);
    }

    public static bool MacOsWindowControlsOnRight(
        Thickness decorationMargin,
        bool rightToLeft)
    {
        if (rightToLeft)
        {
            return true;
        }

        return decorationMargin.Right - decorationMargin.Left >= 24 &&
               decorationMargin.Right >= 40;
    }

    public static double TabStripTrailingReserve(
        bool macOS,
        bool windows,
        bool macOsControlsOnRight = false,
        bool linux = false,
        bool clientCaptions = false)
    {
        if (linux)
        {
            // Tab row sits under the header. Reserve is for the tab scroller only.
            _ = clientCaptions;
            return 24;
        }

        _ = windows;
        return macOS && !macOsControlsOnRight
            ? MacOsTrafficLightFallback + 72
            : 253;
    }

    public static WindowChromeLayout Resolve(
        AppSettings settings,
        WindowChromeHost host,
        int tabCount,
        WindowState windowState,
        bool focusMode,
        bool embedded,
        Thickness offScreenMargin,
        bool rightToLeft = false)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var fullscreen = windowState == WindowState.FullScreen;
        var maximized = windowState == WindowState.Maximized;
        var customTitlebar = ShouldUseCustomTitlebar(settings, embedded);
        var showTabs = !focusMode &&
                       ShouldShowTabStrip(settings, tabCount, fullscreen, host);
        var clientCaptions = host == WindowChromeHost.Linux && !embedded && !focusMode && !fullscreen;
        var showTitleBar = !focusMode && (
            showTabs ||
            customTitlebar ||
            host == WindowChromeHost.Linux ||
            fullscreen);

        if (embedded)
        {
            // Embedded host: + and shell chevron only. No app hamburger strip.
            return new WindowChromeLayout(
                ShowTitleBar: showTabs,
                ShowTabStrip: showTabs,
                ShowNewTabButton: showTabs,
                ShowNewTabMenuButton: showTabs,
                ShowMenuButton: false,
                ShowExitFullscreenButton: false,
                ShowClientCaptionButtons: false,
                ShowMinimizeCaption: false,
                ShowMaximizeCaption: false,
                ShowWindowTitle: false,
                ShowHeaderFind: false,
                TabsBelowHeader: false,
                ExtendClientAreaToDecorations: false,
                WindowDecorations: WindowDecorations.None,
                TitleBarHeight: DefaultHeaderHeight,
                TitleBarMargin: new Thickness(8, 0),
                TabStripTrailingReserve: 120,
                CornerRadius: 0,
                CanResize: true,
                ClipToBounds: false,
                BorderThickness: default);
        }

        // Linux: GNOME CSD header (title + start/end actions + captions).
        // Tabs sit on a second row under the header when visible. That is the
        // good-style screenshot shape: WT tab muscle memory, Adwaita chrome.
        if (host == WindowChromeHost.Linux)
        {
            var squared = fullscreen || maximized;
            return new WindowChromeLayout(
                ShowTitleBar: showTitleBar,
                ShowTabStrip: showTabs,
                ShowNewTabButton: !focusMode && !fullscreen,
                ShowNewTabMenuButton: !focusMode && !fullscreen,
                ShowMenuButton: !focusMode && !fullscreen,
                ShowExitFullscreenButton: fullscreen,
                ShowClientCaptionButtons: clientCaptions,
                ShowMinimizeCaption: false,
                ShowMaximizeCaption: false,
                ShowWindowTitle: showTitleBar && !fullscreen,
                ShowHeaderFind: showTitleBar && !fullscreen,
                TabsBelowHeader: true,
                ExtendClientAreaToDecorations: false,
                WindowDecorations: WindowDecorations.None,
                TitleBarHeight: LinuxHeaderHeight,
                TitleBarMargin: TitleBarContentMargin(
                    fullscreen: fullscreen,
                    macOS: false,
                    windows: false,
                    offScreenMargin: offScreenMargin,
                    rightToLeft: rightToLeft,
                    linux: true,
                    clientCaptions: clientCaptions),
                TabStripTrailingReserve: TabStripTrailingReserve(
                    macOS: false,
                    windows: false,
                    linux: true,
                    clientCaptions: clientCaptions),
                CornerRadius: squared ? 0 : LinuxCornerRadius,
                CanResize: !fullscreen,
                ClipToBounds: true,
                BorderThickness: default);
        }

        // Windows/macOS: WT titlebar. Chevron next to + owns the full new-tab menu.
        // No separate hamburger; system captions stay with the OS frame.
        var extend = customTitlebar && !focusMode;
        return new WindowChromeLayout(
            ShowTitleBar: !focusMode && (showTabs || customTitlebar),
            ShowTabStrip: showTabs,
            ShowNewTabButton: showTabs,
            ShowNewTabMenuButton: showTabs,
            ShowMenuButton: false,
            ShowExitFullscreenButton: fullscreen,
            ShowClientCaptionButtons: false,
            ShowMinimizeCaption: false,
            ShowMaximizeCaption: false,
            ShowWindowTitle: false,
            ShowHeaderFind: false,
            TabsBelowHeader: false,
            ExtendClientAreaToDecorations: extend,
            WindowDecorations: WindowDecorations.Full,
            TitleBarHeight: DefaultHeaderHeight,
            TitleBarMargin: TitleBarContentMargin(
                fullscreen: fullscreen,
                macOS: host == WindowChromeHost.MacOS,
                windows: host == WindowChromeHost.Windows,
                offScreenMargin: offScreenMargin,
                rightToLeft: rightToLeft),
            TabStripTrailingReserve: TabStripTrailingReserve(
                macOS: host == WindowChromeHost.MacOS,
                windows: host == WindowChromeHost.Windows,
                macOsControlsOnRight: host == WindowChromeHost.MacOS &&
                    MacOsWindowControlsOnRight(offScreenMargin, rightToLeft)),
            CornerRadius: 0,
            CanResize: !fullscreen,
            ClipToBounds: false,
            BorderThickness: default);
    }
}
