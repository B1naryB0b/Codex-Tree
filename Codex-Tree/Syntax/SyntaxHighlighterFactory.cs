namespace Codex_Tree.Syntax;

/// <summary>
/// Factory for creating syntax highlighters based on file extensions
/// </summary>
public static class SyntaxHighlighterFactory
{
    private static readonly Dictionary<string, ISyntaxHighlighter> _highlighters = new()
    {
        { ".cs", new CSharpHighlighter() },
        { ".cpp", new CppHighlighter() },
        { ".h", new CppHighlighter() },
        { ".hpp", new CppHighlighter() },
        { ".cc", new CppHighlighter() },
        { ".cxx", new CppHighlighter() },
        { ".py", new PythonHighlighter() }
        // Add more highlighters here as they're implemented:
        // { ".js", new JavaScriptHighlighter() },
        // { ".ts", new TypeScriptHighlighter() },
    };

    /// <summary>
    /// Get a highlighter for the given file extension
    /// </summary>
    /// <param name="fileExtension">File extension including the dot (e.g., ".cs")</param>
    /// <returns>Highlighter for the language, or null if no highlighter is registered</returns>
    public static ISyntaxHighlighter? GetHighlighter(string fileExtension)
    {
        return _highlighters.GetValueOrDefault(fileExtension.ToLowerInvariant());
    }

    /// <summary>
    /// Get a highlighter for the given file path
    /// </summary>
    /// <param name="filePath">Full file path</param>
    /// <returns>Highlighter for the language, or null if no highlighter is registered</returns>
    public static ISyntaxHighlighter? GetHighlighterForFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return GetHighlighter(extension);
    }

    /// <summary>
    /// Register a new highlighter
    /// </summary>
    public static void RegisterHighlighter(ISyntaxHighlighter highlighter)
    {
        foreach (var ext in highlighter.FileExtensions)
        {
            _highlighters[ext.ToLowerInvariant()] = highlighter;
        }
    }
}