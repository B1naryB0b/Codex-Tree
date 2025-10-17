using SkiaSharp;
using System.Text.RegularExpressions;

namespace Codex_Tree.Visualization;

/// <summary>
/// Exports inheritance tree visualizations as images
/// </summary>
public class ImageExporter
{
    private static readonly Regex MarkupRegex = new(@"\[(?<tag>/?[^\]]+)\]", RegexOptions.Compiled);

    private const int DefaultFontSize = 14;
    private const int LineHeight = 18;
    private const int Padding = 40;
    private const int HeaderHeight = 60;

    /// <summary>
    /// Configuration for image export
    /// </summary>
    public class ExportConfig
    {
        public int FontSize { get; set; } = DefaultFontSize;
        public int ImageWidth { get; set; } = 1200;
        // Nord Light theme defaults (nord6 Snow Storm background)
        public SKColor BackgroundColor { get; set; } = SKColor.Parse("#eceff4");  // nord6
        public SKColor TextColor { get; set; } = SKColor.Parse("#2e3440");        // nord0
        public SKColor HeaderColor { get; set; } = SKColor.Parse("#4c566a");      // nord3
        public SKColor HighlightColor { get; set; } = SKColor.Parse("#ebcb8b");   // nord13
        public bool IncludeHeader { get; set; } = true;
        public string Title { get; set; } = "Codex Tree - Inheritance Diagram";
        public bool IsDarkTheme { get; set; } = false;  // Track theme for color selection
    }

    /// <summary>
    /// Export tree lines to a PNG image
    /// </summary>
    public void ExportToPng(List<string> treeLines, string outputPath, ExportConfig? config = null)
    {
        config ??= new ExportConfig();

        // Parse lines and extract plain text
        var parsedLines = treeLines.Select(line => ParseLine(line, config.IsDarkTheme)).ToList();

        // Calculate dimensions
        int maxWidth = parsedLines.Max(l => l.Sum(s => s.Text.Length));
        int charWidth = config.FontSize * 6 / 10; // Approximate monospace character width
        int contentWidth = Math.Max(config.ImageWidth - 2 * Padding, maxWidth * charWidth);
        int contentHeight = parsedLines.Count * LineHeight;
        int headerOffset = config.IncludeHeader ? HeaderHeight : 0;
        int imageHeight = contentHeight + 2 * Padding + headerOffset;

        // Create bitmap
        using var surface = SKSurface.Create(new SKImageInfo(contentWidth + 2 * Padding, imageHeight));
        var canvas = surface.Canvas;

        // Clear background
        canvas.Clear(config.BackgroundColor);

        // Draw header if enabled
        if (config.IncludeHeader)
        {
            DrawHeader(canvas, config, contentWidth + 2 * Padding);
        }

        // Create font
        using var typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal)
                          ?? SKTypeface.FromFamilyName("Courier New", SKFontStyle.Normal)
                          ?? SKTypeface.Default;
        using var font = new SKFont(typeface, config.FontSize);

        // Draw each line
        int yOffset = Padding + headerOffset;
        foreach (var line in parsedLines)
        {
            int xOffset = Padding;
            foreach (var segment in line)
            {
                // Measure text first to get bounds
                using var measurePaint = new SKPaint
                {
                    Typeface = typeface,
                    TextSize = config.FontSize
                };
                var bounds = new SKRect();
                measurePaint.MeasureText(segment.Text, ref bounds);

                // Draw background rectangle if present
                if (segment.BackgroundColor.HasValue)
                {
                    using var bgPaint = new SKPaint
                    {
                        Color = segment.BackgroundColor.Value,
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill
                    };
                    // Draw rectangle slightly larger than text for padding
                    var bgRect = new SKRect(
                        xOffset,
                        yOffset - config.FontSize,
                        xOffset + bounds.Width,
                        yOffset + 4  // Small padding below baseline
                    );
                    canvas.DrawRect(bgRect, bgPaint);
                }

                // Draw text
                var color = GetColorForSegment(segment, config);
                using var textPaint = new SKPaint
                {
                    Color = color,
                    IsAntialias = true,
                    Typeface = typeface,
                    TextSize = config.FontSize
                };

                canvas.DrawText(segment.Text, xOffset, yOffset, font, textPaint);

                // Move to next segment position
                xOffset += (int)bounds.Width;
            }
            yOffset += LineHeight;
        }

        // Save to file
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);
    }

    /// <summary>
    /// Draw header with title
    /// </summary>
    private void DrawHeader(SKCanvas canvas, ExportConfig config, int width)
    {
        using var headerPaint = new SKPaint
        {
            Color = config.HeaderColor,
            IsAntialias = true,
            TextSize = 24,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default
        };

        using var font = new SKFont(headerPaint.Typeface, 24);
        var bounds = new SKRect();
        headerPaint.MeasureText(config.Title, ref bounds);

        float x = (width - bounds.Width) / 2;
        float y = HeaderHeight / 2 + bounds.Height / 2;

        canvas.DrawText(config.Title, x, y, font, headerPaint);

        // Draw separator line (use nord4 for subtle separator)
        using var linePaint = new SKPaint
        {
            Color = SKColor.Parse("#d8dee9"),  // nord4
            StrokeWidth = 1,
            IsAntialias = true
        };
        canvas.DrawLine(Padding, HeaderHeight - 10, width - Padding, HeaderHeight - 10, linePaint);
    }

    /// <summary>
    /// Parse a line with Spectre.Console markup into segments
    /// </summary>
    private List<TextSegment> ParseLine(string line, bool isDarkTheme = false)
    {
        var segments = new List<TextSegment>();
        int lastIndex = 0;

        foreach (Match match in MarkupRegex.Matches(line))
        {
            // Add text before markup
            if (match.Index > lastIndex)
            {
                segments.Add(new TextSegment
                {
                    Text = line.Substring(lastIndex, match.Index - lastIndex),
                    Color = null
                });
            }

            // Parse markup tag
            string tag = match.Groups["tag"].Value;
            if (!tag.StartsWith("/"))
            {
                // Opening tag - extract color and background
                var (fg, bg) = ParseColorTag(tag, isDarkTheme);
                segments.Add(new TextSegment { Text = "", Color = fg, BackgroundColor = bg });
            }
            else
            {
                // Closing tag - reset color and background
                segments.Add(new TextSegment { Text = "", Color = null, BackgroundColor = null });
            }

            lastIndex = match.Index + match.Length;
        }

        // Add remaining text
        if (lastIndex < line.Length)
        {
            segments.Add(new TextSegment
            {
                Text = line.Substring(lastIndex),
                Color = null
            });
        }

        // Merge consecutive segments with same color and background
        var merged = new List<TextSegment>();
        TextSegment? current = null;
        SKColor? activeColor = null;
        SKColor? activeBackground = null;

        foreach (var segment in segments)
        {
            if (segment.Color.HasValue)
            {
                activeColor = segment.Color.Value;
            }

            if (segment.BackgroundColor.HasValue)
            {
                activeBackground = segment.BackgroundColor.Value;
            }

            if (!string.IsNullOrEmpty(segment.Text))
            {
                if (current == null)
                {
                    current = new TextSegment { Text = segment.Text, Color = activeColor, BackgroundColor = activeBackground };
                }
                else if (current.Color == activeColor && current.BackgroundColor == activeBackground)
                {
                    current.Text += segment.Text;
                }
                else
                {
                    merged.Add(current);
                    current = new TextSegment { Text = segment.Text, Color = activeColor, BackgroundColor = activeBackground };
                }
            }
        }

        if (current != null)
        {
            merged.Add(current);
        }

        return merged;
    }

    /// <summary>
    /// Parse Spectre.Console color name to SKColor
    /// Uses theme-appropriate colors (darker for light bg, lighter for dark bg)
    /// Handles both simple colors ("yellow") and compound patterns ("black on yellow")
    /// </summary>
    private (SKColor? foreground, SKColor? background) ParseColorTag(string colorTag, bool isDarkTheme)
    {
        // Handle "foreground on background" pattern
        var parts = colorTag.Split(new[] { " on " }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2)
        {
            // Compound pattern: "black on yellow"
            var fg = ParseSingleColor(parts[0].Trim(), isDarkTheme);
            var bg = ParseSingleColor(parts[1].Trim(), isDarkTheme);
            return (fg, bg);
        }
        else
        {
            // Simple color
            var color = ParseSingleColor(parts[0].Trim(), isDarkTheme);
            return (color, null);
        }
    }

    /// <summary>
    /// Parse a single color name to SKColor
    /// </summary>
    private SKColor? ParseSingleColor(string colorName, bool isDarkTheme)
    {
        colorName = colorName.ToLowerInvariant();

        if (isDarkTheme)
        {
            // Dark theme - use lighter, more vibrant colors
            return colorName switch
            {
                "yellow" => SKColor.Parse("#ebcb8b"),       // nord13 - Aurora Yellow (lighter)
                "blue" => SKColor.Parse("#81a1c1"),         // nord9 - Frost Blue (lighter)
                "cyan" => SKColor.Parse("#88c0d0"),         // nord8 - Frost Cyan (lighter)
                "green" => SKColor.Parse("#a3be8c"),        // nord14 - Aurora Green (lighter)
                "red" => SKColor.Parse("#bf616a"),          // nord11 - Aurora Red
                "white" => SKColor.Parse("#eceff4"),        // nord6 - Snow Storm
                "grey" or "gray" => SKColor.Parse("#d8dee9"), // nord4 - lighter gray
                "dim" => SKColor.Parse("#4c566a"),          // nord3 - Polar Night
                "black" => SKColor.Parse("#2e3440"),        // nord0 - Polar Night
                "magenta" => SKColor.Parse("#b48ead"),      // nord15 - Aurora Purple
                _ => null
            };
        }
        else
        {
            // Light theme - use darker, more muted colors for better contrast
            return colorName switch
            {
                "yellow" => SKColor.Parse("#d08770"),       // nord12 - darker orange-yellow
                "blue" => SKColor.Parse("#5e81ac"),         // nord10 - darker blue
                "cyan" => SKColor.Parse("#5e81ac"),         // nord10 - darker cyan/blue
                "green" => SKColor.Parse("#8fbcbb"),        // nord7 - darker teal-green
                "red" => SKColor.Parse("#bf616a"),          // nord11 - Aurora Red (works on both)
                "white" => SKColor.Parse("#2e3440"),        // nord0 - use dark for light bg
                "grey" or "gray" => SKColor.Parse("#4c566a"), // nord3 - Polar Night
                "dim" => SKColor.Parse("#4c566a"),          // nord3 - Polar Night
                "black" => SKColor.Parse("#2e3440"),        // nord0 - Polar Night
                "magenta" => SKColor.Parse("#b48ead"),      // nord15 - Aurora Purple (works on both)
                _ => null
            };
        }
    }

    /// <summary>
    /// Get color for a text segment
    /// </summary>
    private SKColor GetColorForSegment(TextSegment segment, ExportConfig config)
    {
        return segment.Color ?? config.TextColor;
    }

    /// <summary>
    /// Represents a segment of text with optional color and background
    /// </summary>
    private class TextSegment
    {
        public string Text { get; set; } = "";
        public SKColor? Color { get; set; }
        public SKColor? BackgroundColor { get; set; }
    }
}