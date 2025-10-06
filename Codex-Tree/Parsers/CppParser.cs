using System.Text.RegularExpressions;
using Codex_Tree.Models;

namespace Codex_Tree.Parsers;

/// <summary>
/// Parses C++ files to extract class information using regex patterns
/// </summary>
public class CppParser : BaseParser, ILanguageParser
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
        return ParseDirectoryWithExtensions(
            directoryPath,
            FileExtensions,
            new[] { @"\obj\", @"\bin\", @"\build\", @"\Debug\", @"\Release\" },
            ParseFile,
            recursive);
    }

    /// <summary>
    /// Parse a single C++ file
    /// </summary>
    public List<ClassInfo> ParseFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var classes = new List<ClassInfo>();

        // DON'T remove comments/preprocessor from original content - preserve positions!
        // We'll clean it when extracting class bodies for method counting

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
                LineCount = CountLinesInClassBraceBased(content, match.Index)
            };

            // In C++, struct is like a public class
            var modifier = match.Groups["modifiers"].Value;
            classInfo.IsStatic = false; // C++ doesn't have static classes like C#

            // Detect if this is a nested class
            classInfo.ParentClassName = FindParentClassBraceBased(content, match.Index, classMatches);

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
    /// Count methods within a class definition
    /// </summary>
    private int CountMethodsInClass(string content, int classStartIndex)
    {
        var classBody = ExtractClassBodyBraceBased(content, classStartIndex);

        // NOW clean the class body for method counting
        classBody = RemoveCStyleComments(classBody);
        classBody = Regex.Replace(classBody, @"^\s*#.*?$", "", RegexOptions.Multiline); // Remove preprocessor

        // Filter out constructors, destructors, and non-method declarations
        var methodMatches = MethodRegex.Matches(classBody);
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
}