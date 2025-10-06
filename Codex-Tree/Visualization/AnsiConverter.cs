using System.Text.RegularExpressions;

namespace Codex_Tree.Visualization;

/// <summary>
/// Handles conversion between ANSI escape codes and Spectre.Console markup
/// </summary>
public static class AnsiConverter
{
    private static readonly Regex AnsiCodeRegex = new(@"\x1B\[[^@-~]*[@-~]", RegexOptions.Compiled);

    /// <summary>
    /// Remove ANSI escape codes from a string
    /// </summary>
    public static string RemoveAnsiCodes(string text)
    {
        return AnsiCodeRegex.Replace(text, "");
    }

    /// <summary>
    /// Convert ANSI color codes to Spectre.Console markup
    /// </summary>
    public static string ConvertAnsiToMarkup(string text)
    {
        // Map common ANSI color codes to Spectre markup
        var result = text;

        // TrueColor RGB codes (ESC[38;2;R;G;Bm for foreground, ESC[48;2;R;G;Bm for background)
        // Black on yellow badge (foreground 0,0,0 background ~255,255,0)
        result = Regex.Replace(result, @"\x1B\[38;2;0;0;0;48;2;\d+;255;0m(.*?)\x1B\[0m", "[black on yellow]$1[/]");
        result = Regex.Replace(result, @"\x1B\[38;2;0;0;0m\x1B\[48;2;\d+;255;0m(.*?)\x1B\[0m", "[black on yellow]$1[/]");

        // Black on blue badge (foreground 0,0,0 background ~0,0,255)
        result = Regex.Replace(result, @"\x1B\[38;2;0;0;0;48;2;0;0;\d+m(.*?)\x1B\[0m", "[black on blue]$1[/]");
        result = Regex.Replace(result, @"\x1B\[38;2;0;0;0m\x1B\[48;2;0;0;\d+m(.*?)\x1B\[0m", "[black on blue]$1[/]");

        // Black on cyan badge (foreground 0,0,0 background ~0,255,255)
        result = Regex.Replace(result, @"\x1B\[38;2;0;0;0;48;2;0;\d+;\d+m(.*?)\x1B\[0m", "[black on cyan]$1[/]");
        result = Regex.Replace(result, @"\x1B\[38;2;0;0;0m\x1B\[48;2;0;\d+;\d+m(.*?)\x1B\[0m", "[black on cyan]$1[/]");

        // Background colors with foreground (badges) - 8-bit colors
        // Black on yellow (30;43m) - Abstract badge
        result = Regex.Replace(result, @"\x1B\[30;43m(.*?)\x1B\[0m", "[black on yellow]$1[/]");
        result = Regex.Replace(result, @"\x1B\[30;103m(.*?)\x1B\[0m", "[black on yellow]$1[/]");

        // Black on blue (30;44m) - Sealed badge
        result = Regex.Replace(result, @"\x1B\[30;44m(.*?)\x1B\[0m", "[black on blue]$1[/]");
        result = Regex.Replace(result, @"\x1B\[30;104m(.*?)\x1B\[0m", "[black on blue]$1[/]");

        // Black on cyan (30;46m) - Static badge
        result = Regex.Replace(result, @"\x1B\[30;46m(.*?)\x1B\[0m", "[black on cyan]$1[/]");
        result = Regex.Replace(result, @"\x1B\[30;106m(.*?)\x1B\[0m", "[black on cyan]$1[/]");

        // TrueColor foreground colors - Yellow (~255,255,0)
        result = Regex.Replace(result, @"\x1B\[38;2;255;255;0m(.*?)\x1B\[0m", "[yellow]$1[/]");

        // TrueColor foreground colors - Blue (~0,0,255)
        result = Regex.Replace(result, @"\x1B\[38;2;0;0;255m(.*?)\x1B\[0m", "[blue]$1[/]");

        // TrueColor foreground colors - Cyan (~0,255,255)
        result = Regex.Replace(result, @"\x1B\[38;2;0;255;255m(.*?)\x1B\[0m", "[cyan]$1[/]");

        // Foreground colors only - 8-bit
        // Yellow (33m)
        result = Regex.Replace(result, @"\x1B\[33m(.*?)\x1B\[0m", "[yellow]$1[/]");
        result = Regex.Replace(result, @"\x1B\[93m(.*?)\x1B\[0m", "[yellow]$1[/]");

        // Blue (34m)
        result = Regex.Replace(result, @"\x1B\[34m(.*?)\x1B\[0m", "[blue]$1[/]");
        result = Regex.Replace(result, @"\x1B\[94m(.*?)\x1B\[0m", "[blue]$1[/]");

        // Cyan (36m)
        result = Regex.Replace(result, @"\x1B\[36m(.*?)\x1B\[0m", "[cyan]$1[/]");
        result = Regex.Replace(result, @"\x1B\[96m(.*?)\x1B\[0m", "[cyan]$1[/]");

        // White (37m) - keep as is or remove
        result = Regex.Replace(result, @"\x1B\[37m(.*?)\x1B\[0m", "$1");
        result = Regex.Replace(result, @"\x1B\[97m(.*?)\x1B\[0m", "$1");
        result = Regex.Replace(result, @"\x1B\[38;2;255;255;255m(.*?)\x1B\[0m", "$1");

        // Dim (2m)
        result = Regex.Replace(result, @"\x1B\[2m(.*?)\x1B\[0m", "[dim]$1[/]");

        // Remove any remaining ANSI codes
        result = RemoveAnsiCodes(result);

        return result;
    }
}