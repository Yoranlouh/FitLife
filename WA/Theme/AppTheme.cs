using MudBlazor;

namespace WA.Theme;

public static class AppTheme
{
    public static MudTheme Light = new MudTheme
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#2B2B2B",              // Anthracite
            PrimaryContrastText = "#FFFFFF", // White

            Secondary = "#D4AF37",            // Muted gold
            SecondaryContrastText = "#1A1A1A",// Near black

            Tertiary = "#5A4A7A",             // Muted royal purple
            TertiaryContrastText = "#FFFFFF", // White

            Background = "#F4F4F4",           // Light gray
            Surface = "#FFFFFF",              // White

            AppbarBackground = "#2B2B2B",     // Anthracite
            AppbarText = "#FFFFFF",           // White

            DrawerBackground = "#1F1F1F",     // Dark charcoal
            DrawerText = "#E0E0E0",           // Light gray
            DrawerIcon = "#D4AF37",           // Muted gold

            TextPrimary = "#1A1A1A",          // Near black
            TextSecondary = "#555555",        // Medium gray

            Divider = "#D6D6D6",              // Soft gray

            Success = "#2E7D32",              // Dark green
            Error = "#C62828",                // Dark red
            Warning = "#ED6C02",              // Dark orange
            Info = "#0288D1"                  // Strong blue
        }
    };

    public static MudTheme Dark = new MudTheme
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#FB6F17",               // Bright orange
            PrimaryContrastText = "#FFFFFF",   // White

            Secondary = "#7927FE",             // Vivid purple / violet
            SecondaryContrastText = "#FFFFFF", // White

            Tertiary = "#2B2B2B",              // Anthracite / dark gray
            TertiaryContrastText = "#FFFFFF",  // White

            Background = "#111424",            // Very dark navy blue
            Surface = "#080A11",               // Near-black with slight blue tint

            AppbarBackground = "#080A11",      // Near-black (same as surface)
            AppbarText = "#FFFFFF",            // White

            DrawerBackground = "#181818",      // Deep charcoal gray
            DrawerText = "#E0E0E0",            // Light gray
            DrawerIcon = "#D4AF37",            // Muted gold

            TextPrimary = "#FFFFFF",           // White
            TextSecondary = "#7927FE",         // Purple / violet

            Divider = "#2C2C2C",               // Dark neutral gray

            Success = "#66BB6A",               // Soft green
            Error = "#EF5350",                 // Soft red
            Warning = "#FFA726",               // Warm orange
            Info = "#29B6F6"                   // Light blue / sky blue
        }
    };
}