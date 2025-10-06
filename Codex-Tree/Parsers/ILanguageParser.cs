using Codex_Tree.Models;

namespace Codex_Tree.Parsers;

/// <summary>
/// Interface for language-specific code parsers
/// </summary>
public interface ILanguageParser
{
    /// <summary>
    /// Parse all files in a directory
    /// </summary>
    /// <param name="directoryPath">Directory to parse</param>
    /// <param name="recursive">Whether to search subdirectories</param>
    /// <param name="progressCallback">Optional callback for reporting progress (current, total)</param>
    /// <returns>List of parsed class information</returns>
    List<ClassInfo> ParseDirectory(string directoryPath, bool recursive = true, Action<int, int>? progressCallback = null);

    /// <summary>
    /// Parse a single file
    /// </summary>
    /// <param name="filePath">File to parse</param>
    /// <returns>List of classes found in the file</returns>
    List<ClassInfo> ParseFile(string filePath);

    /// <summary>
    /// The language this parser supports
    /// </summary>
    string Language { get; }

    /// <summary>
    /// File extensions this parser supports (e.g., ".cs", ".py")
    /// </summary>
    string[] FileExtensions { get; }
}