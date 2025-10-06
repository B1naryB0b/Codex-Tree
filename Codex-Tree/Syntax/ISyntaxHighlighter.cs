namespace Codex_Tree.Syntax;

/// <summary>
/// Interface for language-specific syntax highlighting
/// </summary>
public interface ISyntaxHighlighter
{
    /// <summary>
    /// Apply syntax highlighting to a line of code
    /// </summary>
    /// <param name="line">The line to highlight</param>
    /// <returns>The highlighted line with Spectre.Console markup</returns>
    string HighlightLine(string line);

    /// <summary>
    /// The language this highlighter supports
    /// </summary>
    string Language { get; }

    /// <summary>
    /// File extensions this highlighter supports (e.g., ".cs", ".py")
    /// </summary>
    string[] FileExtensions { get; }
}