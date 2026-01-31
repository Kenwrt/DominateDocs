using MudBlazor;
using MudBlazor.Extensions.Core.Css;


namespace DominateDocsSite.Components.Layout;

public static class DominateDocsTheme
{
    // Brand tokens (keep these in one place)
    //private const string Navy = "#0D1C2E";         // Deep Finance Navy
    //private const string Slate = "#253F59";        // Slate Navy Blue
    //private const string DeepSlate = "#223447";    // Deep Slate Blue
    //private const string HoverSteel = "#3E566E";   // Muted Steel Hover

    //private const string SlateBlack = "#1E1E1E";
    //private const string Charcoal = "#333333";
    //private const string Carbon = "#5C5C5C";
    //private const string MidGray = "#B0B0B0";
    //private const string LightGray = "#E6E6E6";
    //private const string White = "#FFFFFF";

    public static readonly MudTheme Light = new()
    {
        PaletteLight = new PaletteLight
        {
            // Brand
            Primary = "#2FB6B8",          // Turquoise / Teal
            Secondary = "#1C2F6E",        // Navy Blue
            Tertiary = "#7ADDD8",         // Aqua / Light Teal

            // Neutrals / surfaces
            Background = "#FFFFFF",
            Surface = "#FFFFFF",

            // Text
            TextPrimary = "#1C2F6E",
            TextSecondary = "#5E6778",

            // Lines / subtle UI
            Divider = "rgba(28,47,110,0.18)",

            // Optional accents from kit
            Info = "#2FB6B8",
            Success = "#2FB6B8",
            Warning = "#C6B8A2",          // Warm Tan / Sand
            Error = "#B00020",

            // AppBar / Drawer vibes
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1C2F6E",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#1C2F6E"
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px"
        }
    };

    public static readonly MudTheme Dark = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#2FB6B8",
            Secondary = "#7ADDD8",
            Tertiary = "#C6B8A2",

            Background = "#0F172A",
            Surface = "#111C33",

            TextPrimary = "#FFFFFF",
            TextSecondary = "rgba(255,255,255,0.75)",
            Divider = "rgba(255,255,255,0.12)",

            AppbarBackground = "#111C33",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#111C33",
            DrawerText = "#FFFFFF"
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px"
        }
    };
}
