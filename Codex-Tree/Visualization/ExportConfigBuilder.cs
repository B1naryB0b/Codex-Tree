using SkiaSharp;
using Spectre.Console;

namespace Codex_Tree.Visualization;

/// <summary>
/// Interactive builder for image export configuration
/// </summary>
public static class ExportConfigBuilder
{
    /// <summary>
    /// Prompt user to configure export settings
    /// </summary>
    public static ImageExporter.ExportConfig BuildConfig(string defaultTitle)
    {
        var config = new ImageExporter.ExportConfig { Title = defaultTitle };

        AnsiConsole.MarkupLine("[cyan]Configure Export Settings[/]");
        AnsiConsole.WriteLine();

        // Title
        config.Title = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter image title:")
                .DefaultValue(defaultTitle)
        );

        // Include header
        config.IncludeHeader = AnsiConsole.Confirm("Include header with title?", true);

        // Image width
        config.ImageWidth = AnsiConsole.Prompt(
            new TextPrompt<int>("Enter image width (pixels):")
                .DefaultValue(1200)
                .ValidationErrorMessage("[red]Width must be between 400 and 4000[/]")
                .Validate(w => w >= 400 && w <= 4000)
        );

        // Font size
        config.FontSize = AnsiConsole.Prompt(
            new TextPrompt<int>("Enter font size:")
                .DefaultValue(14)
                .ValidationErrorMessage("[red]Font size must be between 8 and 32[/]")
                .Validate(s => s >= 8 && s <= 32)
        );

        // Transparent background
        var transparentBackground = AnsiConsole.Confirm("Use transparent background?", false);

        // Color scheme
        var colorScheme = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select color scheme:")
                .AddChoices("Light (default)", "Dark", "High Contrast")
        );

        ApplyColorScheme(config, colorScheme);

        // Apply transparent background if selected
        if (transparentBackground)
        {
            config.BackgroundColor = SKColors.Transparent;
        }

        return config;
    }

    /// <summary>
    /// Apply predefined color scheme to config
    /// </summary>
    private static void ApplyColorScheme(ImageExporter.ExportConfig config, string scheme)
    {
        switch (scheme)
        {
            case "Dark":
                // Nord Polar Night (dark background)
                config.BackgroundColor = SKColor.Parse("#2e3440");     // nord0
                config.TextColor = SKColor.Parse("#eceff4");           // nord6 (Snow Storm)
                config.HeaderColor = SKColor.Parse("#e5e9f0");         // nord5
                config.HighlightColor = SKColor.Parse("#ebcb8b");      // nord13 (Aurora Yellow)
                config.IsDarkTheme = true;
                break;

            case "High Contrast":
                config.BackgroundColor = SKColors.Black;
                config.TextColor = SKColors.White;
                config.HeaderColor = new SKColor(200, 200, 200);
                config.HighlightColor = new SKColor(255, 255, 0);
                config.IsDarkTheme = true;
                break;

            default: // Light (Nord)
                // Nord Snow Storm (light background)
                config.BackgroundColor = SKColor.Parse("#eceff4");     // nord6
                config.TextColor = SKColor.Parse("#2e3440");           // nord0 (Polar Night)
                config.HeaderColor = SKColor.Parse("#4c566a");         // nord3
                config.HighlightColor = SKColor.Parse("#d08770");      // nord12 (darker for light theme)
                config.IsDarkTheme = false;
                break;
        }
    }

    /// <summary>
    /// Create config from command line arguments
    /// </summary>
    public static ImageExporter.ExportConfig FromArgs(string title, int? width = null, int? fontSize = null, string? colorScheme = null)
    {
        var config = new ImageExporter.ExportConfig { Title = title };

        if (width.HasValue)
            config.ImageWidth = width.Value;

        if (fontSize.HasValue)
            config.FontSize = fontSize.Value;

        if (!string.IsNullOrEmpty(colorScheme))
            ApplyColorScheme(config, colorScheme);

        return config;
    }
}