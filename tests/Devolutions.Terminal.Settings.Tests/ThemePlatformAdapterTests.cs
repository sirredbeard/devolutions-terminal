using Devolutions.Terminal.Settings;
using Xunit;

namespace Devolutions.Terminal.Settings.Tests;

public sealed class ThemePlatformAdapterTests
{
    [Fact]
    public void LinuxDefaultFontReplacesBareCascadia()
    {
        Assert.Equal(
            ThemePlatformAdapter.LinuxDefaultFontFace,
            ThemePlatformAdapter.ResolveProfileFontFace("Cascadia Mono", isLinux: true));
        Assert.Equal(
            "JetBrains Mono",
            ThemePlatformAdapter.ResolveProfileFontFace("JetBrains Mono", isLinux: true));
        Assert.Equal(
            "Cascadia Mono",
            ThemePlatformAdapter.ResolveProfileFontFace("Cascadia Mono", isLinux: false));
    }

    [Fact]
    public void LinuxFallbackListLeadsWithAdwaitaMono()
    {
        var fonts = ThemePlatformAdapter.ResolveFallbackFonts(isLinux: true);
        Assert.Equal("Adwaita Mono", fonts[0]);
        Assert.Contains("Noto Color Emoji", fonts);
    }

    [Fact]
    public void LinuxDoesNotPretendAcrylicIsAMaterial()
    {
        Assert.False(ThemePlatformAdapter.SupportsBackdropMaterials(isWindows: false));
        Assert.True(ThemePlatformAdapter.SupportsBackdropMaterials(isWindows: true));

        var profile = new ProfileSettings { UseAcrylic = true, Opacity = 100 };
        Assert.Equal(100, ThemePlatformAdapter.ResolveEffectiveOpacity(profile, isLinux: true));
        Assert.False(ThemePlatformAdapter.ShouldRequestWindowTransparency(profile, isLinux: true));

        profile.Opacity = 85;
        Assert.True(ThemePlatformAdapter.ShouldRequestWindowTransparency(profile, isLinux: true));
    }

    [Fact]
    public void ApplyLinuxGeneratedProfileDefaultsIsIdempotent()
    {
        var profile = new ProfileSettings();
        ThemePlatformAdapter.ApplyLinuxGeneratedProfileDefaults(profile, isLinux: true);
        Assert.Equal(ThemePlatformAdapter.LinuxDefaultFontFace, profile.FontFace);
        Assert.Equal(ThemePlatformAdapter.LinuxDefaultBackground, profile.Background);
        ThemePlatformAdapter.ApplyLinuxGeneratedProfileDefaults(profile, isLinux: true);
        Assert.Equal(ThemePlatformAdapter.LinuxDefaultFontFace, profile.FontFace);
    }
}
