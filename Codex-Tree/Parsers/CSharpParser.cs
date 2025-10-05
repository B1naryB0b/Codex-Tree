using System.Text.RegularExpressions;
using Codex_Tree.Models;

namespace Codex_Tree.Parsers;

/// <summary>
/// Parses C# files to extract class information using regex patterns
/// </summary>
public class CSharpParser
{
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
    public List<ClassInfo> ParseDirectory(string directoryPath, bool recursive = true)
    {
        var classes = new List<ClassInfo>();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var csFiles = Directory.GetFiles(directoryPath, "*.cs", searchOption)
            .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"));

        foreach (var file in csFiles)
        {
            try
            {
                classes.AddRange(ParseFile(file));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing {file}: {ex.Message}");
            }
        }

        return classes;
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
                LineCount = CountLinesInClass(content, match.Index)
            };

            // Parse modifiers
            var modifiers = match.Groups["modifiers"].Value;
            classInfo.IsAbstract = modifiers.Contains("abstract");
            classInfo.IsSealed = modifiers.Contains("sealed");
            classInfo.IsStatic = modifiers.Contains("static");

            // Detect if this is a nested class
            classInfo.ParentClassName = FindParentClass(content, match.Index, classMatches);

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
    /// Find the parent class if this class is nested inside another
    /// </summary>
    private string? FindParentClass(string content, int classStartIndex, MatchCollection allMatches)
    {
        // Count braces backwards from this class to see if we're inside another class
        var precedingContent = content.Substring(0, classStartIndex);
        var braceDepth = 0;

        // Count open braces from start to this class
        foreach (var ch in precedingContent)
        {
            if (ch == '{') braceDepth++;
            else if (ch == '}') braceDepth--;
        }

        // If braceDepth > 0, we might be inside another class
        if (braceDepth > 0)
        {
            // Find the most recent class definition before this one
            foreach (Match potentialParent in allMatches)
            {
                if (potentialParent.Index < classStartIndex)
                {
                    var parentEnd = FindClassEndPosition(content, potentialParent.Index);
                    // Check if current class is within the parent's braces
                    if (parentEnd > classStartIndex)
                    {
                        return potentialParent.Groups["name"].Value;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Find the end position of a class (closing brace)
    /// </summary>
    private int FindClassEndPosition(string content, int classStartIndex)
    {
        var remainingContent = content.Substring(classStartIndex);
        var braceCount = 0;
        var inClass = false;
        var position = classStartIndex;

        foreach (var ch in remainingContent)
        {
            if (ch == '{')
            {
                braceCount++;
                inClass = true;
            }
            else if (ch == '}')
            {
                braceCount--;
                if (braceCount == 0 && inClass)
                    return position;
            }
            position++;
        }

        return position;
    }

    /// <summary>
    /// Count methods within a class definition
    /// </summary>
    private int CountMethodsInClass(string content, int classStartIndex)
    {
        // Find the class body (simplified - counts methods until next class or end)
        var remainingContent = content.Substring(classStartIndex);
        var braceCount = 0;
        var inClass = false;
        var classBody = new System.Text.StringBuilder();

        foreach (var ch in remainingContent)
        {
            if (ch == '{')
            {
                braceCount++;
                inClass = true;
            }
            else if (ch == '}')
            {
                braceCount--;
                if (braceCount == 0 && inClass)
                    break;
            }

            if (inClass)
                classBody.Append(ch);
        }

        return MethodRegex.Matches(classBody.ToString()).Count;
    }

    /// <summary>
    /// Estimate line count for a class
    /// </summary>
    private int CountLinesInClass(string content, int classStartIndex)
    {
        var remainingContent = content.Substring(classStartIndex);
        var braceCount = 0;
        var inClass = false;
        var lineCount = 0;

        foreach (var ch in remainingContent)
        {
            if (ch == '{')
            {
                braceCount++;
                inClass = true;
            }
            else if (ch == '}')
            {
                braceCount--;
                if (braceCount == 0 && inClass)
                    break;
            }

            if (ch == '\n' && inClass)
                lineCount++;
        }

        return lineCount;
    }
}