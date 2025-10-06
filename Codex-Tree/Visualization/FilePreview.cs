using Codex_Tree.Models;
using Codex_Tree.Syntax;
using System.Text;

namespace Codex_Tree.Visualization;

/// <summary>
/// Handles file preview rendering with syntax highlighting
/// </summary>
public class FilePreview
{
    private const int ViewportHeight = 16;
    private const int MaxWidth = 70;
    private readonly ISyntaxHighlighter? _highlighter;

    /// <summary>
    /// Create a FilePreview with automatic highlighter detection
    /// </summary>
    public FilePreview()
    {
        _highlighter = null; // Will be determined per-file
    }

    /// <summary>
    /// Create a FilePreview with a specific highlighter
    /// </summary>
    public FilePreview(ISyntaxHighlighter highlighter)
    {
        _highlighter = highlighter;
    }

    /// <summary>
    /// Build file preview text with scrolling support
    /// </summary>
    public StringBuilder BuildPreview(InheritanceNode node, int scrollOffset)
    {
        var preview = new StringBuilder();
        var filePath = node.ClassInfo.FilePath;

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            preview.AppendLine("[dim]File not found[/]");
            return preview;
        }

        try
        {
            var lines = File.ReadAllLines(filePath);
            int startLine = Math.Max(0, Math.Min(scrollOffset, Math.Max(0, lines.Length - ViewportHeight)));
            int endLine = Math.Min(startLine + ViewportHeight, lines.Length);

            for (int i = startLine; i < endLine; i++)
            {
                var lineNumber = $"[dim]{(i + 1),4}[/]";
                var content = lines[i];

                // Truncate line if it exceeds max width (before highlighting to get accurate length)
                if (content.Length > MaxWidth)
                {
                    content = content.Substring(0, MaxWidth - 3) + "...";
                }

                // Apply syntax highlighting
                var highlighter = _highlighter ?? SyntaxHighlighterFactory.GetHighlighterForFile(filePath);
                content = HighlightSyntax(content, highlighter);

                preview.AppendLine($"{lineNumber} │ {content}");
            }

            // Show scroll indicator if there's more content below
            if (endLine < lines.Length)
            {
                preview.AppendLine($"[dim]... ({lines.Length - endLine} more lines below)[/]");
            }
        }
        catch (Exception ex)
        {
            preview.AppendLine($"[red]Error reading file: {ex.Message}[/]");
        }

        return preview;
    }

    /// <summary>
    /// Calculate maximum scroll offset for file preview
    /// </summary>
    public int GetMaxScrollOffset(InheritanceNode node)
    {
        var filePath = node.ClassInfo.FilePath;

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return 0;

        try
        {
            var lineCount = File.ReadAllLines(filePath).Length;
            return Math.Max(0, lineCount - ViewportHeight);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Get the column width for preview display
    /// </summary>
    public int GetColumnWidth() => MaxWidth + 7; // +7 for line number and separator

    /// <summary>
    /// Apply syntax highlighting to a line of code using the provided highlighter
    /// </summary>
    private static string HighlightSyntax(string line, ISyntaxHighlighter? highlighter)
    {
        if (highlighter == null)
        {
            // No highlighter available - just escape markup
            return line.Replace("[", "[[").Replace("]", "]]");
        }

        return highlighter.HighlightLine(line);
    }
}