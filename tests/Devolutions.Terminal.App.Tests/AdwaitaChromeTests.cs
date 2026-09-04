using Avalonia.Media;
using Devolutions.Terminal.App.Platform;
using Xunit;

namespace Devolutions.Terminal.App.Tests;

public sealed class AdwaitaChromeTests
{
    [Fact]
    public void DarkTokensMatchLibadwaitaDefaults()
    {
        // prefers-color-scheme: dark from libadwaita default.css
        Assert.Equal("#2E2E32", AdwaitaChrome.HeaderBg);
        Assert.Equal("#222226", AdwaitaChrome.WindowBg);
        Assert.Equal("#1D1D20", AdwaitaChrome.ViewBg);
        Assert.Equal("#36363A", AdwaitaChrome.PopoverBg);
        Assert.Equal(WindowChrome.LinuxHeaderBackground, AdwaitaChrome.HeaderBg);
    }

    [Fact]
    public void HeaderBrushIsVerticalGradient()
    {
        var brush = Assert.IsType<LinearGradientBrush>(AdwaitaChrome.HeaderBackgroundBrush());
        Assert.Equal(2, brush.GradientStops.Count);
        Assert.Equal(0, brush.GradientStops[0].Offset);
        Assert.Equal(1, brush.GradientStops[1].Offset);
    }

    [Fact]
    public void ChromeStackShadeHasInsetAndDrop()
    {
        var shadows = AdwaitaChrome.ChromeStackShade();
        Assert.True(shadows.Count >= 2);
        Assert.True(shadows.HasInsetShadows);
    }
}
