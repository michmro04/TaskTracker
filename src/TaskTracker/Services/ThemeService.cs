using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace TaskTracker.Services;

public sealed record ThemePalette(
    string Background,
    string Header,
    string Card,
    string Button,
    string Border,
    string Accent,
    string AccentForeground = "#FFFFFF");

public sealed class AccentOption(string name, ThemePalette dark, ThemePalette light)
{
    public string Name { get; } = name;
    public ThemePalette Dark { get; } = dark;
    public ThemePalette Light { get; } = light;
    public IBrush Brush { get; } = new SolidColorBrush(Color.Parse(dark.Accent));
}

public static class ThemeService
{
    public static IReadOnlyList<AccentOption> AccentOptions { get; } =
    [
        new("Mono",
            dark:  new("#000000", "#0D0D0D", "#141414", "#1F1F1F", "#333333", "#FFFFFF", "#000000"),
            light: new("#FAFAFA", "#F0F0F0", "#FFFFFF", "#E6E6E6", "#CCCCCC", "#111111", "#FFFFFF")),

        new("Niebieski",
            dark:  new("#0B1220", "#111B2E", "#162238", "#1E3252", "#28416B", "#3B82F6"),
            light: new("#EFF6FF", "#DBEAFE", "#FFFFFF", "#BFDBFE", "#93C5FD", "#2563EB")),

        new("Zielony",
            dark:  new("#091710", "#0F241A", "#142F22", "#1D4230", "#285941", "#22C55E"),
            light: new("#F0FDF4", "#DCFCE7", "#FFFFFF", "#BBF7D0", "#86EFAC", "#16A34A")),

        new("Czerwony",
            dark:  new("#1A0B0E", "#261116", "#33171E", "#4A202A", "#662B39", "#EF4444"),
            light: new("#FEF2F2", "#FEE2E2", "#FFFFFF", "#FECACA", "#FCA5A5", "#DC2626")),

        new("Fioletowy",
            dark:  new("#120D1D", "#1B132B", "#241A38", "#332450", "#47336E", "#8B5CF6"),
            light: new("#F5F3FF", "#EDE9FE", "#FFFFFF", "#DDD6FE", "#C4B5FD", "#7C3AED")),

        new("Szary",
            dark:  new("#111318", "#1A1D24", "#222630", "#2F3542", "#3F4756", "#64748B"),
            light: new("#F8FAFC", "#F1F5F9", "#FFFFFF", "#E2E8F0", "#CBD5E1", "#475569"))
    ];

    public static void ApplyTheme(AccentOption option, bool isDark)
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        var palette = isDark ? option.Dark : option.Light;

        app.Resources["AppBackgroundBrush"] = new SolidColorBrush(Color.Parse(palette.Background));
        app.Resources["AppHeaderBrush"] = new SolidColorBrush(Color.Parse(palette.Header));
        app.Resources["AppCardBrush"] = new SolidColorBrush(Color.Parse(palette.Card));
        app.Resources["AppButtonBrush"] = new SolidColorBrush(Color.Parse(palette.Button));
        app.Resources["AppBorderBrush"] = new SolidColorBrush(Color.Parse(palette.Border));
        app.Resources["AppAccentBrush"] = new SolidColorBrush(Color.Parse(palette.Accent));
        var accentFgBrush = new SolidColorBrush(Color.Parse(palette.AccentForeground));
        app.Resources["AppAccentForegroundBrush"] = accentFgBrush;

        app.Resources["AccentButtonForeground"] = accentFgBrush;
        app.Resources["AccentButtonForegroundPointerOver"] = accentFgBrush;
        app.Resources["AccentButtonForegroundPressed"] = accentFgBrush;
        var fluentTheme = app.Styles.OfType<FluentTheme>().FirstOrDefault();
        
        if (fluentTheme is not null)
        {
            fluentTheme.Palettes[ThemeVariant.Dark] = new ColorPaletteResources
            {
                Accent = Color.Parse(option.Dark.Accent),
                RegionColor = Color.Parse(option.Dark.Background)
            };

            fluentTheme.Palettes[ThemeVariant.Light] = new ColorPaletteResources
            {
                Accent = Color.Parse(option.Light.Accent),
                RegionColor = Color.Parse(option.Light.Background)
            };
        }

        var targetVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        var oppositeVariant = isDark ? ThemeVariant.Light : ThemeVariant.Dark;

        app.RequestedThemeVariant = oppositeVariant;
        app.RequestedThemeVariant = targetVariant;
    }
}