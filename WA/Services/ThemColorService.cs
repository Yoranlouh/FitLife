using MudBlazor.Utilities;
using WA.Theme;

namespace WA.Services;

public static class ThemeColorService
{
    private static string ToHex(MudColor color)
    {
        return color.ToString(MudColorOutputFormats.Hex);
    }

    public static Dictionary<string, string> MapLightThemeToHex()
    {
        var p = AppTheme.Light.PaletteLight;

        return new Dictionary<string, string>
        {
            { "Primary", ToHex(p.Primary) },
            { "PrimaryContrastText", ToHex(p.PrimaryContrastText) },
            { "Secondary", ToHex(p.Secondary) },
            { "SecondaryContrastText", ToHex(p.SecondaryContrastText) },
            { "Tertiary", ToHex(p.Tertiary) },
            { "TertiaryContrastText", ToHex(p.TertiaryContrastText) },
            { "Background", ToHex(p.Background) },
            { "Surface", ToHex(p.Surface) },
            { "AppbarBackground", ToHex(p.AppbarBackground) },
            { "AppbarText", ToHex(p.AppbarText) },
            { "DrawerBackground", ToHex(p.DrawerBackground) },
            { "DrawerText", ToHex(p.DrawerText) },
            { "DrawerIcon", ToHex(p.DrawerIcon) },
            { "TextPrimary", ToHex(p.TextPrimary) },
            { "TextSecondary", ToHex(p.TextSecondary) },
            { "Divider", ToHex(p.Divider) },
            { "Success", ToHex(p.Success) },
            { "Error", ToHex(p.Error) },
            { "Warning", ToHex(p.Warning) },
            { "Info", ToHex(p.Info) }
        };
    }

    public static Dictionary<string, string> MapDarkThemeToHex()
    {
        var p = AppTheme.Dark.PaletteDark;

        return new Dictionary<string, string>
        {
            { "Primary", ToHex(p.Primary) },
            { "PrimaryContrastText", ToHex(p.PrimaryContrastText) },
            { "Secondary", ToHex(p.Secondary) },
            { "SecondaryContrastText", ToHex(p.SecondaryContrastText) },
            { "Tertiary", ToHex(p.Tertiary) },
            { "TertiaryContrastText", ToHex(p.TertiaryContrastText) },
            { "Background", ToHex(p.Background) },
            { "Surface", ToHex(p.Surface) },
            { "AppbarBackground", ToHex(p.AppbarBackground) },
            { "AppbarText", ToHex(p.AppbarText) },
            { "DrawerBackground", ToHex(p.DrawerBackground) },
            { "DrawerText", ToHex(p.DrawerText) },
            { "DrawerIcon", ToHex(p.DrawerIcon) },
            { "TextPrimary", ToHex(p.TextPrimary) },
            { "TextSecondary", ToHex(p.TextSecondary) },
            { "Divider", ToHex(p.Divider) },
            { "Success", ToHex(p.Success) },
            { "Error", ToHex(p.Error) },
            { "Warning", ToHex(p.Warning) },
            { "Info", ToHex(p.Info) }
        };
    }
}