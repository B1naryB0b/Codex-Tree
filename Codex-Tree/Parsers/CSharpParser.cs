using System.Text.RegularExpressions;
using Codex_Tree.Models;

namespace Codex_Tree.Parsers;

/// <summary>
/// Parses C# files to extract class information using regex patterns
/// </summary>
public class CSharpParser : BaseParser, ILanguageParser
{
    public string Language => "C#";
    public string[] FileExtensions => new[] { ".cs" };

    private static readonly Regex NamespaceRegex = new(@"namespace\s+([\w\.]+)", RegexOptions.Compiled);

    private static readonly Regex ClassRegex = new(
        @"(?<modifiers>(?:public|private|protected|internal|abstract|sealed|static)\s+)*class\s+(?<name>\w+)(?:\s*:\s*(?<inheritance>[\w\s,\.]+))?",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex MethodRegex = new(
        @"(?:public|private|protected|internal|static|virtual|override|async)\s+(?:\w+\s+)+\w+\s*\(",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Parse all C# files in a directory
    /// </summary>
    public List<ClassInfo> ParseDirectory(string directoryPath, bool recursive = true, Action<int, int>? progressCallback = null)
    {
        return ParseDirectoryWithExtensions(
            directoryPath,
            FileExtensions,
            new[] { @"\obj\", @"\bin\" },
            ParseFile,
            recursive,
            progressCallback);
    }

    /// <summary>
    /// Parse a single C# file
    /// </summary>
    public List<ClassInfo> ParseFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var classes = new List<ClassInfo>();

        // Extract namespace
        var namespaceMatch = NamespaceRegex.Match(content);
        var defaultNamespace = namespaceMatch.Success ? namespaceMatch.Groups[1].Value : null;

        // Find all classes
        var classMatches = ClassRegex.Matches(content);

        foreach (Match match in classMatches)
        {
            // Skip if this match is inside a comment
            if (IsInComment(content, match.Index))
                continue;

            var classInfo = new ClassInfo
            {
                Name = match.Groups["name"].Value,
                Namespace = defaultNamespace,
                FilePath = filePath,
                LineCount = CountLinesInClassBraceBased(content, match.Index)
            };

            // Parse modifiers
            var modifiers = match.Groups["modifiers"].Value;
            classInfo.IsAbstract = modifiers.Contains("abstract");
            classInfo.IsSealed = modifiers.Contains("sealed");
            classInfo.IsStatic = modifiers.Contains("static");

            // Detect if this is a nested class
            classInfo.ParentClassName = FindParentClassBraceBased(content, match.Index, classMatches);

            // Parse inheritance
            if (match.Groups["inheritance"].Success)
            {
                var inheritance = match.Groups["inheritance"].Value
                    .Split(',')
                    .Select(s => s.Trim())
                    .ToList();

                // First item is typically the base class, rest are interfaces
                if (inheritance.Count > 0)
                {
                    var first = inheritance[0];
                    // Simple heuristic: interfaces often start with 'I' or contain known interface patterns
                    if (first.StartsWith("I") && first.Length > 1 && char.IsUpper(first[1]))
                    {
                        classInfo.Interfaces.AddRange(inheritance);
                    }
                    else
                    {
                        classInfo.BaseClass = first;
                        classInfo.Interfaces.AddRange(inheritance.Skip(1));
                    }
                }
            }

            // Count methods
            classInfo.MethodCount = CountMethodsInClass(content, match.Index);

            classes.Add(classInfo);
        }

        return classes;
    }

    /// <summary>
    /// Check if a position in the content is inside a comment
    /// </summary>
    private bool IsInComment(string content, int position)
    {
        // Find the start of the line containing this position
        var lineStart = content.LastIndexOf('\n', position) + 1;
        var lineContent = content.Substring(lineStart, position - lineStart);

        // Check for single-line comment (// or ///)
        if (lineContent.Contains("//"))
        {
            var commentStart = lineContent.IndexOf("//");
            // If the match position is after the //, it's in a comment
            if (position - lineStart > commentStart)
                return true;
        }

        // Check for multi-line comment (/* */)
        var lastBlockCommentStart = content.LastIndexOf("/*", position);
        if (lastBlockCommentStart != -1)
        {
            var lastBlockCommentEnd = content.LastIndexOf("*/", position);
            // If we found a /* before this position and no */ between them, we're in a comment
            if (lastBlockCommentEnd < lastBlockCommentStart)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Count methods within a class definition
    /// </summary>
    private int CountMethodsInClass(string content, int classStartIndex)
    {
        var classBody = ExtractClassBodyBraceBased(content, classStartIndex);
        return MethodRegex.Matches(classBody).Count;
    }
}