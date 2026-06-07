namespace FitLife.Maui.Helpers;

// Single source of truth for resolving a workout hex color string (from the database)
// to a MAUI Color object. All views that need a workout color must call Resolve() here
// so that the color shown always matches what is stored in the database via the Blazor admin panel.
//
// Resolution strategy (in order):
//   1. Parse the hex string from WorkoutColor (e.g. "#5B6636") via raw byte arithmetic —
//      more reliable than Color.FromArgb(string) on some MAUI platform/version combinations.
//   2. Fall back to the DB default color (#5B6636) if parsing fails or the value is empty.
//
// There is intentionally NO name-based fallback (no "crossfit → green" mapping).
// The color is always the one the admin set in the Blazor webapp, nothing else.
public static class WorkoutColorHelper
{
    // The same default that the API uses via COALESCE(w.color, '#5B6636')
    private static readonly Color DefaultColor = ParseHex("#5B6636")!;

    // Resolves a workout color string to a MAUI Color.
    // Pass LessonResponse.WorkoutColor directly — the value is always a hex string from the API.
    public static Color Resolve(string? workoutColor)
    {
        if (!string.IsNullOrEmpty(workoutColor) && workoutColor.StartsWith('#'))
        {
            var parsed = ParseHex(workoutColor);
            if (parsed is not null) return parsed;
        }

        return DefaultColor;
    }

    // Parses a #RRGGBB or #AARRGGBB hex string to a MAUI Color using raw byte arithmetic.
    // Returns null on any parse failure so the caller can fall through to the next strategy.
    private static Color? ParseHex(string hex)
    {
        try
        {
            var h = hex.TrimStart('#');
            if (h.Length == 6)
            {
                byte r = Convert.ToByte(h.Substring(0, 2), 16);
                byte g = Convert.ToByte(h.Substring(2, 2), 16);
                byte b = Convert.ToByte(h.Substring(4, 2), 16);
                return Color.FromRgb(r / 255f, g / 255f, b / 255f);
            }
            if (h.Length == 8)
            {
                byte a = Convert.ToByte(h.Substring(0, 2), 16);
                byte r = Convert.ToByte(h.Substring(2, 2), 16);
                byte g = Convert.ToByte(h.Substring(4, 2), 16);
                byte b = Convert.ToByte(h.Substring(6, 2), 16);
                return Color.FromRgba(r / 255f, g / 255f, b / 255f, a / 255f);
            }
        }
        catch { }
        return null;
    }
}
