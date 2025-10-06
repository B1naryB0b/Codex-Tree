using System.Text.RegularExpressions;

namespace Codex_Tree.Syntax;

/// <summary>
/// C++ syntax highlighter implementation
/// </summary>
public class CppHighlighter : ISyntaxHighlighter
{
    public string Language => "C++";
    public string[] FileExtensions => new[] { ".cpp", ".h", ".hpp", ".cc", ".cxx" };

    public string HighlightLine(string line)
    {
        // Escape markup first
        line = line.Replace("[", "[[").Replace("]", "]]");

        // Highlight preprocessor directives
        line = Regex.Replace(line,
            @"^\s*(#\s*(?:include|define|ifndef|ifdef|endif|pragma|undef|if|else|elif))",
            "[purple]$1[/]",
            RegexOptions.None);

        // Highlight class/struct declarations
        line = Regex.Replace(line,
            @"\b(class|struct|enum)\s+(\w+)",
            "[yellow]$1[/] [bold yellow]$2[/]",
            RegexOptions.None);

        // Highlight function declarations/definitions
        line = Regex.Replace(line,
            @"\b(public|private|protected|virtual|static|inline|constexpr|explicit|friend)\s+",
            "[dim]$1[/] ",
            RegexOptions.None);

        // Highlight method names (simplified pattern)
        line = Regex.Replace(line,
            @"\b(\w+)\s*\(",
            "[bold cyan]$1[/](",
            RegexOptions.None);

        // Highlight keywords
        line = Regex.Replace(line,
            @"\b(namespace|using|return|if|else|for|while|switch|case|break|continue|new|delete|const|auto|void|int|char|bool|float|double|long|short|unsigned|signed|typedef|template|typename|nullptr|override|final|try|catch|throw|public|private|protected)\b",
            "[blue]$1[/]",
            RegexOptions.None);

        // Highlight pointers and references
        line = Regex.Replace(line,
            @"(\*|&)(\w+)",
            "$1[cyan]$2[/]",
            RegexOptions.None);

        return line;
    }
}