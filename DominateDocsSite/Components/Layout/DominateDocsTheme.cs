using MudBlazor;
using MudBlazor.Extensions.Core.Css;


namespace DominateDocsSite.Components.Layout;

public static class DominateDocsTheme
{
    // Brand tokens (keep these in one place)
    private const string Navy = "#0D1C2E";         // Deep Finance Navy
    private const string Slate = "#253F59";        // Slate Navy Blue
    private const string DeepSlate = "#223447";    // Deep Slate Blue
    private const string HoverSteel = "#3E566E";   // Muted Steel Hover

    private const string SlateBlack = "#1E1E1E";
    private const string Charcoal = "#333333";
    private const string Carbon = "#5C5C5C";
    private const string MidGray = "#B0B0B0";
    private const string LightGray = "#E6E6E6";
    private const string White = "#FFFFFF";

    public static readonly MudTheme Light = new()
    {
        PaletteLight = new PaletteLight
        {
            // Brand
            Primary = Slate,
            Secondary = Navy,
            Tertiary = DeepSlate,

            // Layout
            Background = LightGray,        // app background
            Surface = White,               // cards/papers
            AppbarBackground = Navy,
            DrawerBackground = White,

            // Text
            TextPrimary = SlateBlack,
            TextSecondary = Charcoal,
            TextDisabled = MidGray,

            // Borders/dividers
            Divider = MidGray,
            LinesDefault = LightGray,
            LinesInputs = MidGray,

            // States (Mud uses these widely)
            ActionDefault = Slate,
            ActionDisabled = MidGray,
            ActionDisabledBackground = LightGray,

            // Feedback colors (not in the brand kit; keep muted and consistent)
            Success = "#2E7D32",
            Warning = "#ED6C02",
            Error = "#D32F2F",
            Info = Slate,
        },


        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Poppins", "Segoe UI", "Arial", "sans-serif" },
                FontSize = ".95rem",
                FontWeight = "400",
                LineHeight = "1.45"
            },

            H1 = new H1Typography
            {
                FontFamily = new[] { "Allrounder Monument Test", "Poppins", "serif" },
                FontWeight = "600",
                FontSize = "2.25rem",
                LineHeight = "1.10"
            },

            H2 = new H2Typography
            {
                FontFamily = new[] { "Allrounder Monument Test", "Poppins", "serif" },
                FontWeight = "600",
                FontSize = "1.85rem",
                LineHeight = "1.12"
            },

            H3 = new H3Typography
            {
                FontFamily = new[] { "Poppins", "sans-serif" },
                FontWeight = "600",
                FontSize = "1.35rem"
            },

            Body1 = new Body1Typography
            {
                FontFamily = new[] { "Poppins", "sans-serif" },
                FontWeight = "400"
            },

            Button = new ButtonTypography
            {
                FontFamily = new[] { "Poppins", "sans-serif" },
                FontWeight = "600",
                TextTransform = "none"
            }
        },



        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "14px"
        }
    };

    public static readonly MudTheme Dark = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = HoverSteel,       // reads better on dark
            Secondary = Slate,
            Tertiary = DeepSlate,

            Background = SlateBlack,
            Surface = DeepSlate,
            AppbarBackground = Navy,
            DrawerBackground = Navy,

            TextPrimary = White,
            TextSecondary = LightGray,
            TextDisabled = Carbon,

            Divider = Carbon,
            LinesDefault = Carbon,
            LinesInputs = Carbon,

            ActionDefault = LightGray,
            ActionDisabled = Carbon,
            ActionDisabledBackground = Charcoal,

            Success = "#2E7D32",
            Warning = "#ED6C02",
            Error = "#D32F2F",
            Info = Slate,
        },
        Typography = Light.Typography,
        LayoutProperties = Light.LayoutProperties
    };
}
