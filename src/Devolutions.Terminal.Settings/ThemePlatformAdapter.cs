namespace Devolutions.Terminal.Settings;

/// <summary>
/// Maps WT appearance settings onto host presentation. Behavior keys stay WT
/// shaped. Linux gets Adwaita-oriented defaults and no fake Mica.
/// </summary>
public static class ThemePlatformAdapter
{
    public const string LinuxDefaultFontFace = "Adwaita Mono";
    public const double LinuxDefaultFontSize = 11;
    public const string LinuxDefaultBackground = "#1E1E1E";
    public const string LinuxDefaultForeground = "#F5F5F5";

    public static string ResolveProfileFontFace(string? configuredFace, bool isLinux)
    {
        if (!string.IsNullOrWhiteSpace(configuredFace) &&
            !configuredFace.Equals("Cascadia Mono", StringComparison.OrdinalIgnoreCase))
        {
            return configuredFace;
        }

        return isLinux ? LinuxDefaultFontFace : (configuredFace ?? "Cascadia Mono");
    }

    public static IReadOnlyList<string> ResolveFallbackFonts(bool isLinux) =>
        isLinux
            ?
            [
                "Adwaita Mono",
                "Cascadia Mono",
                "Noto Sans Mono",
                "DejaVu Sans Mono",
                "Noto Color Emoji",
                "Segoe UI Emoji",
            ]
            :
            [
                "Cascadia Mono",
                "Consolas",
                "Noto Color Emoji",
                "Segoe UI Emoji",
            ];

    /// <summary>
    /// Acrylic and Mica are Windows materials. On Linux we only honor opacity.
    /// </summary>
    public static bool SupportsBackdropMaterials(bool isWindows) => isWindows;

    public static int ResolveEffectiveOpacity(ProfileSettings profile, bool isLinux)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var opacity = profile.Opacity;
        if (opacity is < 0 or > 100)
        {
            opacity = 100;
        }

        if (isLinux && profile.UseAcrylic && opacity >= 100)
        {
            // WT often pairs acrylic with 100 opacity and lets the material do
            // the work. Linux has no acrylic, so keep solid unless the user
            // set a real opacity.
            return 100;
        }

        return opacity;
    }

    public static bool ShouldRequestWindowTransparency(ProfileSettings profile, bool isLinux)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!isLinux)
        {
            return profile.UseAcrylic || profile.Opacity is > 0 and < 100;
        }

        return ResolveEffectiveOpacity(profile, isLinux) < 100;
    }

    public static void ApplyLinuxGeneratedProfileDefaults(ProfileSettings profile, bool isLinux)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!isLinux)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(profile.FontFace) ||
            profile.FontFace.Equals("Cascadia Mono", StringComparison.OrdinalIgnoreCase))
        {
            profile.Font = new FontSettings
            {
                Face = LinuxDefaultFontFace,
                Size = profile.Font.Size <= 0 ? LinuxDefaultFontSize : profile.Font.Size,
            };
        }

        profile.Background ??= LinuxDefaultBackground;
        profile.Foreground ??= LinuxDefaultForeground;
    }
}
