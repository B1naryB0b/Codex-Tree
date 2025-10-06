using System.Text;
using System.Text.RegularExpressions;

namespace Codex_Tree.Parsers;

/// <summary>
/// Base class providing common parsing utilities for all language parsers
/// </summary>
public abstract class BaseParser
{
    /// <summary>
    /// Parse directory with common error handling and filtering
    /// </summary>
    protected List<T> ParseDirectoryWithExtensions<T>(
        string directoryPath,
        string[] extensions,
        string[] excludeDirs,
        Func<string, List<T>> parseFileFunc,
        bool recursive = true,
        Action<int, int>? progressCallback = null)
    {
        var results = new List<T>();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var files = new List<string>();
        foreach (var ext in extensions)
        {
            files.AddRange(Directory.GetFiles(directoryPath, $"*{ext}", searchOption));
        }

        // Filter out excluded directories
        files = files.Where(f => !excludeDirs.Any(dir => f.Contains(dir))).ToList();

        var totalFiles = files.Count;
        var processedFiles = 0;

        foreach (var file in files)
        {
            try
            {
                results.AddRange(parseFileFunc(file));
                processedFiles++;
                progressCallback?.Invoke(processedFiles, totalFiles);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing {file}: {ex.Message}");
                processedFiles++;
                progressCallback?.Invoke(processedFiles, totalFiles);
            }
        }

        return results;
    }

    /// <summary>
    /// Remove single-line and multi-line comments (C/C++/C# style)
    /// </summary>
    protected string RemoveCStyleComments(string content)
    {
        // Remove multi-line comments /* */
        content = Regex.Replace(content, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Remove single-line comments //
        content = Regex.Replace(content, @"//.*?$", "", RegexOptions.Multiline);

        return content;
    }

    #region Brace-based language utilities (C#, C++, Java, etc.)

    /// <summary>
    /// Find the parent class if this class is nested inside another (brace-based)
    /// </summary>
    protected string? FindParentClassBraceBased(string content, int classStartIndex, MatchCollection allMatches)
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
                    var parentEnd = FindClassEndPositionBraceBased(content, potentialParent.Index);
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
    protected int FindClassEndPositionBraceBased(string content, int classStartIndex)
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
    /// Extract the class body as a string (brace-based)
    /// </summary>
    protected string ExtractClassBodyBraceBased(string content, int classStartIndex)
    {
        var remainingContent = content.Substring(classStartIndex);
        var braceCount = 0;
        var inClass = false;
        var classBody = new StringBuilder();

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

        return classBody.ToString();
    }

    /// <summary>
    /// Count lines within a class definition (brace-based)
    /// </summary>
    protected int CountLinesInClassBraceBased(string content, int classStartIndex)
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

    #endregion

    #region Indentation-based language utilities (Python, etc.)

    /// <summary>
    /// Get indentation level (number of spaces, tabs converted to 4 spaces)
    /// </summary>
    protected int GetIndentLevel(string indent)
    {
        return indent.Replace("\t", "    ").Length;
    }

    /// <summary>
    /// Get line number from character position
    /// </summary>
    protected int GetLineNumber(string content, int position)
    {
        return content.Substring(0, position).Count(c => c == '\n');
    }

    /// <summary>
    /// Find the end line of a class by tracking indentation
    /// </summary>
    protected int FindClassEndLineIndentBased(string[] lines, int startLine, int classIndentLevel)
    {
        for (int i = startLine + 1; i < lines.Length; i++)
        {
            var line = lines[i];

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                continue;

            // Check if we've returned to same or lower indentation level
            var lineBeforeTrimmingLength = line.Length - line.TrimStart().Length;
            var lineIndent = line.Substring(0, lineBeforeTrimmingLength);
            var currentIndent = GetIndentLevel(lineIndent);

            if (currentIndent <= classIndentLevel)
            {
                return i - 1;
            }
        }

        return lines.Length - 1;
    }

    /// <summary>
    /// Find the parent class if this class is nested inside another (indent-based)
    /// </summary>
    protected string? FindParentClassIndentBased(string content, int classStartIndex, MatchCollection allMatches)
    {
        var lines = content.Split('\n');
        var currentLine = GetLineNumber(content, classStartIndex);

        // Get current indentation
        var precedingLines = content.Substring(0, classStartIndex).Split('\n');
        var lastLine = precedingLines.Length > 0 ? precedingLines.Last() : "";
        var currentIndent = GetIndentLevel(lastLine);

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
                var parentEndLine = FindClassEndLineIndentBased(lines, parentLine, parentIndent);
                if (currentLine <= parentEndLine)
                {
                    return potentialParent.Groups["name"].Value;
                }
            }
        }

        return null;
    }

    #endregion
}