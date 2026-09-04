using Avalonia;
using Avalonia.Media;

namespace Devolutions.Terminal.App.Platform;

/// <summary>
/// Materials lifted from libadwaita dark tokens (default.css). Solid paint, inset
/// shade, top rim highlight, and a short drop shadow under the chrome. Flat
/// fills without these read as mockups.
/// </summary>
public static class AdwaitaChrome
{
    // prefers-color-scheme: dark from libadwaita _colors.scss / default.css
    public const string WindowBg = "#222226";
    public const string ViewBg = "#1D1D20";
    public const string HeaderBg = "#2E2E32";
    public const string HeaderBgTop = "#323236";
    public const string PopoverBg = "#36363A";
    public const string DialogBg = "#36363A";
    public const string HeaderFg = "#FFFFFF";
    // headerbar_shade_color: RGB(0 0 6 / 36%)
    public const string HeaderShade = "#5C000006";
    // softer mix used under titlebars: ~50% of shade
    public const string HeaderShadeSoft = "#2E000006";
    // inset top rim: RGB(255 255 255 / 7%)
    public const string TopRim = "#12FFFFFF";
    // card / hover: RGB(255 255 255 / 8%)
    public const string CardFill = "#14FFFFFF";
    public const string HoverFill = "#1AFFFFFF";
    public const string PressedFill = "#0FFFFFFF";
    // headerbar title is bold; keep body at regular with slight dim
    public const double TitleOpacity = 0.9;
    public const double IconOpacity = 0.85;

    public static IBrush HeaderBackgroundBrush() =>
        new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            [
                new GradientStop(Color.Parse(HeaderBgTop), 0),
                new GradientStop(Color.Parse(HeaderBg), 1),
            ],
        };

    public static IBrush SolidHeaderBrush() =>
        new SolidColorBrush(Color.Parse(HeaderBg));

    public static IBrush WindowBrush() =>
        new SolidColorBrush(Color.Parse(WindowBg));

    public static IBrush ViewBrush() =>
        new SolidColorBrush(Color.Parse(ViewBg));

    public static IBrush PopoverBrush() =>
        new SolidColorBrush(Color.Parse(PopoverBg));

    public static BoxShadows HeaderInsetShade() =>
        // inset 0 -1px headerbar-shade: the line Adwaita draws under the bar
        BoxShadows.Parse($"inset 0 -1 0 0 {HeaderShade}");

    public static BoxShadows ChromeDropShade() =>
        // titlebar:not(.flat): 0 1px shade, 0 2px 4px shade
        BoxShadows.Parse($"0 1 0 0 {HeaderShadeSoft}, 0 2 4 0 {HeaderShadeSoft}");

    public static BoxShadows ChromeStackShade() =>
        // inset bottom line + short drop onto the terminal view
        BoxShadows.Parse(
            $"inset 0 -1 0 0 {HeaderShade}, 0 1 0 0 {HeaderShadeSoft}, 0 2 4 0 {HeaderShadeSoft}");

    public static BoxShadows PopoverShade() =>
        BoxShadows.Parse("0 2 8 0 #40000000, 0 0 0 1 #1AFFFFFF");
}
