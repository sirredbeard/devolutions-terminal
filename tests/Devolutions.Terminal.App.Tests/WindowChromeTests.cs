using Avalonia;
using Avalonia.Controls;
using Devolutions.Terminal.App.Platform;
using Devolutions.Terminal.Settings;
using Xunit;

namespace Devolutions.Terminal.App.Tests;

public sealed class WindowChromeTests
{
    [Fact]
    public void HidesSingleTabWhenAlwaysShowTabsIsFalse()
    {
        var settings = new AppSettings { AlwaysShowTabs = false };
        Assert.False(WindowChrome.ShouldShowTabRow(settings, tabCount: 1, fullscreen: false));
        Assert.True(WindowChrome.ShouldShowTabRow(settings, tabCount: 2, fullscreen: false));
    }

    [Fact]
    public void AlwaysShowTabsKeepsSingleTabVisible()
    {
        var settings = new AppSettings { AlwaysShowTabs = true };
        Assert.True(WindowChrome.ShouldShowTabRow(settings, tabCount: 1, fullscreen: false));
    }

    [Fact]
    public void HidesTabsInFullscreenUnlessEnabled()
    {
        var settings = new AppSettings { AlwaysShowTabs = true, ShowTabsFullscreen = false };
        Assert.False(WindowChrome.ShouldShowTabRow(settings, tabCount: 2, fullscreen: true));
        settings.ShowTabsFullscreen = true;
        Assert.True(WindowChrome.ShouldShowTabRow(settings, tabCount: 2, fullscreen: true));
    }

    [Fact]
    public void EmbeddedWindowsDoNotUseCustomTitlebar()
    {
        var settings = new AppSettings { ShowTabsInTitlebar = true };
        Assert.False(WindowChrome.ShouldUseCustomTitlebar(settings, embedded: true));
        Assert.True(WindowChrome.ShouldUseCustomTitlebar(settings, embedded: false));
        settings.ShowTabsInTitlebar = false;
        Assert.False(WindowChrome.ShouldUseCustomTitlebar(settings, embedded: false));
        settings.ShowTabsInTitlebar = true;
        Assert.True(WindowChrome.ShouldUseCustomTitlebar(settings, embedded: false, macOS: true));
    }

    [Fact]
    public void MacOsOverlayTitleBarUsesSnugTrafficLightInset()
    {
        var margin = WindowChrome.TitleBarContentMargin(
            fullscreen: false,
            macOS: true,
            windows: false,
            offScreenMargin: default);

        Assert.Equal(WindowChrome.MacOsTrafficLightFallback, margin.Left);
        Assert.Equal(8, margin.Right);
    }

    [Fact]
    public void MacOsKeepsOriginalLayoutWhenWindowControlsAreOnTheRight()
    {
        var margin = WindowChrome.TitleBarContentMargin(
            fullscreen: false,
            macOS: true,
            windows: false,
            offScreenMargin: new Thickness(0, 0, 78, 0),
            rightToLeft: false);

        Assert.True(WindowChrome.MacOsWindowControlsOnRight(new Thickness(0, 0, 78, 0), rightToLeft: false));
        Assert.Equal(8, margin.Left);
        Assert.Equal(WindowChrome.WindowsCaptionFallback, margin.Right);
    }

    [Fact]
    public void MacOsRtlLayoutUsesOriginalCaptionReserve()
    {
        Assert.True(WindowChrome.MacOsWindowControlsOnRight(default, rightToLeft: true));
        var margin = WindowChrome.TitleBarContentMargin(
            fullscreen: false,
            macOS: true,
            windows: false,
            offScreenMargin: default,
            rightToLeft: true);

        Assert.Equal(8, margin.Left);
        Assert.Equal(WindowChrome.WindowsCaptionFallback, margin.Right);
    }

    [Fact]
    public void WindowsTitleBarLeavesRoomForCaptionButtons()
    {
        var margin = WindowChrome.TitleBarContentMargin(
            fullscreen: false,
            macOS: false,
            windows: true,
            offScreenMargin: default);

        Assert.Equal(8, margin.Left);
        Assert.Equal(WindowChrome.WindowsCaptionFallback, margin.Right);
    }

    [Fact]
    public void FullscreenTitleBarDropsCaptionInsets()
    {
        var margin = WindowChrome.TitleBarContentMargin(
            fullscreen: true,
            macOS: true,
            windows: true,
            offScreenMargin: new Thickness(78, 0, 138, 0));

        Assert.Equal(new Thickness(8, 0, 8, 0), margin);
    }

    [Fact]
    public void MacOsTabStripReserveIsInsetPlusTrailingControls()
    {
        Assert.Equal(
            WindowChrome.MacOsTrafficLightFallback + 72,
            WindowChrome.TabStripTrailingReserve(macOS: true, windows: false));
        Assert.Equal(
            253,
            WindowChrome.TabStripTrailingReserve(macOS: true, windows: false, macOsControlsOnRight: true));
        Assert.Equal(253, WindowChrome.TabStripTrailingReserve(macOS: false, windows: true));
    }

    [Fact]
    public void LinuxClientHeaderOwnsDecorationsWithoutExtendClientArea()
    {
        var settings = new AppSettings { AlwaysShowTabs = true, ShowTabsInTitlebar = true };
        var layout = WindowChrome.Resolve(
            settings,
            WindowChromeHost.Linux,
            tabCount: 2,
            windowState: WindowState.Normal,
            focusMode: false,
            embedded: false,
            offScreenMargin: default);

        Assert.True(layout.ShowTitleBar);
        Assert.True(layout.ShowTabStrip);
        Assert.True(layout.ShowNewTabButton);
        Assert.True(layout.ShowNewTabMenuButton);
        Assert.True(layout.ShowMenuButton);
        Assert.True(layout.ShowClientCaptionButtons);
        Assert.False(layout.ShowMinimizeCaption);
        Assert.False(layout.ShowMaximizeCaption);
        Assert.True(layout.ShowWindowTitle);
        Assert.True(layout.ShowHeaderFind);
        Assert.True(layout.TabsBelowHeader);
        Assert.False(layout.ExtendClientAreaToDecorations);
        Assert.Equal(WindowDecorations.None, layout.WindowDecorations);
        Assert.Equal(47, layout.TitleBarHeight);
        Assert.Equal(WindowChrome.LinuxHeaderHeight, layout.TitleBarHeight);
        Assert.Equal(WindowChrome.LinuxCornerRadius, layout.CornerRadius);
        Assert.Equal(new Thickness(10, 0, 10, 0), layout.TitleBarMargin);
        Assert.True(layout.CanResize);
    }

    [Fact]
    public void LinuxSingleTabKeepsHeaderWithoutTabRow()
    {
        var settings = new AppSettings { AlwaysShowTabs = false };
        var layout = WindowChrome.Resolve(
            settings,
            WindowChromeHost.Linux,
            tabCount: 1,
            windowState: WindowState.Normal,
            focusMode: false,
            embedded: false,
            offScreenMargin: default);

        Assert.True(layout.ShowTitleBar);
        Assert.False(layout.ShowTabStrip);
        Assert.True(layout.ShowWindowTitle);
        Assert.True(layout.ShowHeaderFind);
        Assert.True(layout.ShowNewTabButton);
        Assert.True(layout.ShowNewTabMenuButton);
        Assert.True(layout.ShowMenuButton);
        Assert.True(layout.ShowClientCaptionButtons);
        Assert.False(layout.ShowMinimizeCaption);
        Assert.False(layout.ShowMaximizeCaption);
        Assert.True(layout.TabsBelowHeader);
    }

    [Fact]
    public void LinuxFullscreenHidesCaptionsShowsExitControl()
    {
        var settings = new AppSettings { AlwaysShowTabs = true, ShowTabsFullscreen = true };
        var layout = WindowChrome.Resolve(
            settings,
            WindowChromeHost.Linux,
            tabCount: 2,
            windowState: WindowState.FullScreen,
            focusMode: false,
            embedded: false,
            offScreenMargin: default);

        Assert.True(layout.ShowExitFullscreenButton);
        Assert.False(layout.ShowClientCaptionButtons);
        Assert.False(layout.CanResize);
        Assert.Equal(0, layout.CornerRadius);
        Assert.Equal(new Thickness(8, 0, 8, 0), layout.TitleBarMargin);
    }

    [Fact]
    public void LinuxFocusModeHidesHeaderChrome()
    {
        var settings = new AppSettings { AlwaysShowTabs = true };
        var layout = WindowChrome.Resolve(
            settings,
            WindowChromeHost.Linux,
            tabCount: 2,
            windowState: WindowState.Normal,
            focusMode: true,
            embedded: false,
            offScreenMargin: default);

        Assert.False(layout.ShowTitleBar);
        Assert.False(layout.ShowTabStrip);
        Assert.False(layout.ShowClientCaptionButtons);
        Assert.False(layout.ShowNewTabButton);
    }

    [Fact]
    public void LinuxSingleTabHidesStripUnlessAlwaysShowTabs()
    {
        var settings = new AppSettings { AlwaysShowTabs = false, ShowTabsInTitlebar = true };
        var hidden = WindowChrome.Resolve(
            settings,
            WindowChromeHost.Linux,
            tabCount: 1,
            windowState: WindowState.Normal,
            focusMode: false,
            embedded: false,
            offScreenMargin: default);
        Assert.False(hidden.ShowTabStrip);
        Assert.True(hidden.ShowTitleBar);
        Assert.True(hidden.ShowClientCaptionButtons);

        settings.AlwaysShowTabs = true;
        var shown = WindowChrome.Resolve(
            settings,
            WindowChromeHost.Linux,
            tabCount: 1,
            windowState: WindowState.Normal,
            focusMode: false,
            embedded: false,
            offScreenMargin: default);
        Assert.True(shown.ShowTabStrip);
    }

    [Fact]
    public void WindowsLayoutStillExtendsClientAreaForCustomTitlebar()
    {
        var settings = new AppSettings { AlwaysShowTabs = true, ShowTabsInTitlebar = true };
        var layout = WindowChrome.Resolve(
            settings,
            WindowChromeHost.Windows,
            tabCount: 2,
            windowState: WindowState.Normal,
            focusMode: false,
            embedded: false,
            offScreenMargin: default);

        Assert.True(layout.ExtendClientAreaToDecorations);
        Assert.Equal(WindowDecorations.Full, layout.WindowDecorations);
        Assert.False(layout.ShowClientCaptionButtons);
        Assert.True(layout.ShowNewTabButton);
        Assert.True(layout.ShowNewTabMenuButton);
        Assert.False(layout.ShowMenuButton);
        Assert.False(layout.TabsBelowHeader);
        Assert.Equal(WindowChrome.WindowsCaptionFallback, layout.TitleBarMargin.Right);
    }

    [Theory]
    [InlineData(WindowState.Normal, WindowState.FullScreen)]
    [InlineData(WindowState.FullScreen, WindowState.Normal)]
    [InlineData(WindowState.Maximized, WindowState.FullScreen)]
    public void ToggleFullscreenUsesExplicitTransitions(WindowState current, WindowState expected)
    {
        Assert.Equal(expected, WindowStateTransitions.ToggleFullscreen(current));
    }

    [Theory]
    [InlineData(WindowState.Normal, WindowState.Maximized)]
    [InlineData(WindowState.Maximized, WindowState.Normal)]
    [InlineData(WindowState.FullScreen, WindowState.Maximized)]
    public void ToggleMaximizedUsesExplicitTransitions(WindowState current, WindowState expected)
    {
        Assert.Equal(expected, WindowStateTransitions.ToggleMaximized(current));
    }

    [Fact]
    public void SetFullscreenAndMaximizedArgsMapCleanly()
    {
        Assert.Equal(WindowState.FullScreen, WindowStateTransitions.SetFullscreen(true));
        Assert.Equal(WindowState.Normal, WindowStateTransitions.SetFullscreen(false));
        Assert.Equal(WindowState.Maximized, WindowStateTransitions.SetMaximized(true));
        Assert.Equal(WindowState.Normal, WindowStateTransitions.SetMaximized(false));
    }
}
