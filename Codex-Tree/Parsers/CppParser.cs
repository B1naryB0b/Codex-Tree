using System.Text.RegularExpressions;
using Codex_Tree.Models;

namespace Codex_Tree.Parsers;

/// <summary>
/// Parses C++ files to extract class information using regex patterns
/// </summary>
public class CppParser : ILanguageParser
{
    public string Language => "C++";
    public string[] FileExtensions => new[] { ".cpp", ".h", ".hpp", ".cc", ".cxx" };

    private static readonly Regex NamespaceRegex = new(@"namespace\s+(\w+)", RegexOptions.Compiled);

    private static readonly Regex ClassRegex = new(
        @"(?<modifiers>(?:class|struct))\s+(?<name>\w+)(?:\s*:\s*(?:public|private|protected)?\s*(?<inheritance>[\w\s,:<>]+))?\s*\{",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex MethodRegex = new(
        @"(?:public|private|protected)?\s*(?:virtual|static|inline|constexpr)?\s*[\w<>:\*&]+\s+(\w+)\s*\([^)]*\)\s*(?:const)?\s*(?:override)?\s*(?:final)?\s*[{;]",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Parse all C++ files in a directory
    /// </summary>
    public List<ClassInfo> ParseDirectory(string directoryPath, bool recursive = true)
    {
        var classes = new List<ClassInfo>();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var cppFiles = new List<string>();
        foreach (var ext in FileExtensions)
        {
            cppFiles.AddRange(Directory.GetFiles(directoryPath, $"*{ext}", searchOption));
        }

        cppFiles = cppFiles
            .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\") &&
                       !f.Contains("\\build\\") && !f.Contains("\\Debug\\") &&
                       !f.Contains("\\Release\\"))
            .ToList();

        foreach (var file in cppFiles)
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
    /// Parse a single C++ file
    /// </summary>
    public List<ClassInfo> ParseFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var classes = new List<ClassInfo>();

        // Remove preprocessor directives and comments for cleaner parsing
        content = RemovePreprocessorDirectives(content);
        content = RemoveComments(content);

        // Extract namespace (simplified - only handles first namespace)
        var namespaceMatch = NamespaceRegex.Match(content);
        var defaultNamespace = namespaceMatch.Success ? namespaceMatch.Groups[1].Value : null;

        // Find all classes/structs
        var classMatches = ClassRegex.Matches(content);

        foreach (Match match in classMatches)
        {
            var classInfo = new ClassInfo
            {
                Name = match.Groups["name"].Value,
                Namespace = defaultNamespace,
                FilePath = filePath,
                LineCount = CountLinesInClass(content, match.Index)
            };

            // In C++, struct is like a public class
            var modifier = match.Groups["modifiers"].Value;
            classInfo.IsStatic = false; // C++ doesn't have static classes like C#

            // Detect if this is a nested class
            classInfo.ParentClassName = FindParentClass(content, match.Index, classMatches);

            // Parse inheritance
            if (match.Groups["inheritance"].Success)
            {
                var inheritance = match.Groups["inheritance"].Value
                    .Split(',')
                    .Select(s => s.Trim())
                    .Select(s => Regex.Replace(s, @"(public|private|protected)\s+", "")) // Remove access specifiers
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                // C++ doesn't distinguish between classes and interfaces like C#
                // First item is base class, rest are additional bases (multiple inheritance)
                if (inheritance.Count > 0)
                {
                    classInfo.BaseClass = inheritance[0].Trim();
                    // Additional base classes go to interfaces list for compatibility
                    if (inheritance.Count > 1)
                    {
                        classInfo.Interfaces.AddRange(inheritance.Skip(1).Select(s => s.Trim()));
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
    /// Remove single-line and multi-line comments
    /// </summary>
    private string RemoveComments(string content)
    {
        // Remove multi-line comments /* */
        content = Regex.Replace(content, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Remove single-line comments //
        content = Regex.Replace(content, @"//.*?$", "", RegexOptions.Multiline);

        return content;
    }

    /// <summary>
    /// Remove preprocessor directives (#include, #define, etc.)
    /// </summary>
    private string RemovePreprocessorDirectives(string content)
    {
        return Regex.Replace(content, @"^\s*#.*?$", "", RegexOptions.Multiline);
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
        // Find the class body
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

        var bodyText = classBody.ToString();

        // Filter out constructors, destructors, and non-method declarations
        var methodMatches = MethodRegex.Matches(bodyText);
        int count = 0;

        foreach (Match match in methodMatches)
        {
            var methodName = match.Groups[1].Value;
            // Skip constructors and destructors (basic heuristic)
            if (!methodName.StartsWith("~") && methodName != "operator")
            {
                count++;
            }
        }

        return count;
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