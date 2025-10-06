using System.Text.RegularExpressions;
using Codex_Tree.Models;

namespace Codex_Tree.Parsers;

/// <summary>
/// Parses Python files to extract class information using regex patterns
/// </summary>
public class PythonParser : ILanguageParser
{
    public string Language => "Python";
    public string[] FileExtensions => new[] { ".py" };

    private static readonly Regex ClassRegex = new(
        @"^(?<indent>[ \t]*)class\s+(?<name>\w+)(?:\((?<inheritance>[^)]+)\))?:",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex MethodRegex = new(
        @"^\s+def\s+(\w+)\s*\(",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Parse all Python files in a directory
    /// </summary>
    public List<ClassInfo> ParseDirectory(string directoryPath, bool recursive = true)
    {
        var classes = new List<ClassInfo>();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var pyFiles = Directory.GetFiles(directoryPath, "*.py", searchOption)
            .Where(f => !f.Contains("\\__pycache__\\") && !f.Contains("\\.venv\\") &&
                       !f.Contains("\\venv\\") && !f.Contains("\\.tox\\") &&
                       !f.Contains("\\build\\") && !f.Contains("\\dist\\"))
            .ToList();

        foreach (var file in pyFiles)
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
    /// Parse a single Python file
    /// </summary>
    public List<ClassInfo> ParseFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var classes = new List<ClassInfo>();

        // Remove comments (but not docstrings, as they're part of the structure)
        content = RemoveComments(content);

        // Find all classes
        var classMatches = ClassRegex.Matches(content);
        var lines = content.Split('\n');

        foreach (Match match in classMatches)
        {
            var classInfo = new ClassInfo
            {
                Name = match.Groups["name"].Value,
                Namespace = null, // Python uses modules, not namespaces
                FilePath = filePath
            };

            // Get indentation level to determine if it's nested
            var indent = match.Groups["indent"].Value;
            var indentLevel = GetIndentLevel(indent);

            // Find class end by tracking indentation
            var classStartLine = GetLineNumber(content, match.Index);
            var classEndLine = FindClassEndLine(lines, classStartLine, indentLevel);

            classInfo.LineCount = classEndLine - classStartLine + 1;

            // Check if nested class
            if (indentLevel > 0)
            {
                classInfo.ParentClassName = FindParentClass(content, match.Index, classMatches);
            }

            // Parse inheritance
            if (match.Groups["inheritance"].Success)
            {
                var inheritance = match.Groups["inheritance"].Value
                    .Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                // Python supports multiple inheritance
                if (inheritance.Count > 0)
                {
                    classInfo.BaseClass = inheritance[0];
                    if (inheritance.Count > 1)
                    {
                        classInfo.Interfaces.AddRange(inheritance.Skip(1));
                    }
                }
            }

            // Count methods
            classInfo.MethodCount = CountMethodsInClass(content, match.Index, classStartLine, classEndLine);

            // Python doesn't have these modifiers
            classInfo.IsAbstract = false;
            classInfo.IsSealed = false;
            classInfo.IsStatic = false;

            classes.Add(classInfo);
        }

        return classes;
    }

    /// <summary>
    /// Remove comments (lines starting with #)
    /// </summary>
    private string RemoveComments(string content)
    {
        // Remove single-line comments starting with #
        // Don't remove docstrings (they're structural)
        var lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var hashIndex = line.IndexOf('#');
            if (hashIndex >= 0)
            {
                // Check if it's not inside a string
                var beforeHash = line.Substring(0, hashIndex);
                var singleQuotes = beforeHash.Count(c => c == '\'');
                var doubleQuotes = beforeHash.Count(c => c == '"');

                // Simple heuristic: if even quotes, # is outside string
                if (singleQuotes % 2 == 0 && doubleQuotes % 2 == 0)
                {
                    lines[i] = line.Substring(0, hashIndex);
                }
            }
        }
        return string.Join('\n', lines);
    }

    /// <summary>
    /// Get indentation level (number of spaces/tabs)
    /// </summary>
    private int GetIndentLevel(string indent)
    {
        // Convert tabs to 4 spaces for consistency
        return indent.Replace("\t", "    ").Length;
    }

    /// <summary>
    /// Get line number from character position
    /// </summary>
    private int GetLineNumber(string content, int position)
    {
        return content.Substring(0, position).Count(c => c == '\n');
    }

    /// <summary>
    /// Find the end line of a class by tracking indentation
    /// </summary>
    private int FindClassEndLine(string[] lines, int startLine, int classIndentLevel)
    {
        for (int i = startLine + 1; i < lines.Length; i++)
        {
            var line = lines[i];

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                continue;

            // Check if we've returned to same or lower indentation level
            var currentIndent = GetIndentLevel(line.Substring(0, line.Length - line.TrimStart().Length));

            if (currentIndent <= classIndentLevel)
            {
                return i - 1;
            }
        }

        return lines.Length - 1;
    }

    /// <summary>
    /// Find the parent class if this class is nested inside another
    /// </summary>
    private string? FindParentClass(string content, int classStartIndex, MatchCollection allMatches)
    {
        var lines = content.Split('\n');
        var currentLine = GetLineNumber(content, classStartIndex);
        var currentIndent = GetIndentLevel(content.Substring(0, classStartIndex).Split('\n').Last());

        // Look backwards for a class with less indentation
        foreach (Match potentialParent in allMatches)
        {
            if (potentialParent.Index >= classStartIndex)
                continue;

            var parentLine = GetLineNumber(content, potentialParent.Index);
            var parentIndent = GetIndentLevel(potentialParent.Groups["indent"].Value);

            if (parentIndent < currentIndent && parentLine < currentLine)
            {
                // Check if current class is within parent's scope
                var parentEndLine = FindClassEndLine(lines, parentLine, parentIndent);
                if (currentLine <= parentEndLine)
                {
                    return potentialParent.Groups["name"].Value;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Count methods within a class definition
    /// </summary>
    private int CountMethodsInClass(string content, int classStartIndex, int startLine, int endLine)
    {
        var lines = content.Split('\n');
        var classLines = lines.Skip(startLine).Take(endLine - startLine + 1);
        var classContent = string.Join('\n', classLines);

        var methodMatches = MethodRegex.Matches(classContent);
        int count = 0;

        foreach (Match match in methodMatches)
        {
            var methodName = match.Groups[1].Value;
            // Exclude special methods like __init__, __str__, etc. from public method count
            // (though you might want to include them depending on requirements)
            if (!methodName.StartsWith("_"))
            {
                count++;
            }
        }

        return count;
    }
}