namespace Codex_Tree.Parsers;

/// <summary>
/// Factory for creating language parsers based on language name
/// </summary>
public static class ParserFactory
{
    private static readonly Dictionary<string, ILanguageParser> _parsers = new()
    {
        { "C#", new CSharpParser() },
        { "C++", new CppParser() }
        // Add more parsers here as they're implemented:
        // { "Python", new PythonParser() },
        // { "JavaScript", new JavaScriptParser() },
        // { "TypeScript", new TypeScriptParser() },
    };

    /// <summary>
    /// Get a parser for the given language
    /// </summary>
    /// <param name="language">Language name (e.g., "C#", "Python")</param>
    /// <returns>Parser for the language, or null if no parser is registered</returns>
    public static ILanguageParser? GetParser(string language)
    {
        return _parsers.GetValueOrDefault(language);
    }

    /// <summary>
    /// Get a parser for the given file extension
    /// </summary>
    /// <param name="fileExtension">File extension including the dot (e.g., ".cs")</param>
    /// <returns>Parser for the language, or null if no parser is registered</returns>
    public static ILanguageParser? GetParserByExtension(string fileExtension)
    {
        return _parsers.Values.FirstOrDefault(p =>
            p.FileExtensions.Contains(fileExtension.ToLowerInvariant()));
    }

    /// <summary>
    /// Register a new parser
    /// </summary>
    public static void RegisterParser(ILanguageParser parser)
    {
        _parsers[parser.Language] = parser;
    }

    /// <summary>
    /// Get all registered language names
    /// </summary>
    public static IEnumerable<string> GetSupportedLanguages()
    {
        return _parsers.Keys;
    }
}