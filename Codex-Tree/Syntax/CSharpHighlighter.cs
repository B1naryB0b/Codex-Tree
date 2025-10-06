using System.Text.RegularExpressions;

namespace Codex_Tree.Syntax;

/// <summary>
/// C# syntax highlighter implementation
/// </summary>
public class CSharpHighlighter : ISyntaxHighlighter
{
    public string Language => "C#";
    public string[] FileExtensions => new[] { ".cs" };

    public string HighlightLine(string line)
    {
        // Escape markup first
        line = line.Replace("[", "[[").Replace("]", "]]");

        // Highlight class declarations (public/private/protected/internal class ClassName)
        line = Regex.Replace(line,
            @"\b(public|private|protected|internal|abstract|sealed|static)\s+(class|interface|struct|enum)\s+(\w+)",
            "[dim]$1[/] [yellow]$2[/] [bold yellow]$3[/]",
            RegexOptions.None);

        // Highlight method declarations - capture modifier, return type, and method name
        line = Regex.Replace(line,
            @"\b(public|private|protected|internal|static|virtual|override|async|abstract)\s+([\w<>\[\]?]+)\s+(\w+)\s*\(",
            "[dim]$1[/] $2 [bold cyan]$3[/](",
            RegexOptions.None);

        // Highlight keywords
        line = Regex.Replace(line,
            @"\b(namespace|using|return|if|else|for|foreach|while|switch|case|break|continue|new|var|const|void)\b",
            "[blue]$1[/]",
            RegexOptions.None);

        return line;
    }
}