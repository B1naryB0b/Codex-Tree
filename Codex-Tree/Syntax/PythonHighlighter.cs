using System.Text.RegularExpressions;

namespace Codex_Tree.Syntax;

/// <summary>
/// Python syntax highlighter implementation
/// </summary>
public class PythonHighlighter : ISyntaxHighlighter
{
    public string Language => "Python";
    public string[] FileExtensions => new[] { ".py" };

    public string HighlightLine(string line)
    {
        // Escape markup first
        line = line.Replace("[", "[[").Replace("]", "]]");

        // Highlight class declarations
        line = Regex.Replace(line,
            @"\bclass\s+(\w+)",
            "[yellow]class[/] [bold yellow]$1[/]",
            RegexOptions.None);

        // Highlight function/method definitions
        line = Regex.Replace(line,
            @"\bdef\s+(\w+)",
            "[blue]def[/] [bold cyan]$1[/]",
            RegexOptions.None);

        // Highlight decorators
        line = Regex.Replace(line,
            @"(@\w+)",
            "[purple]$1[/]",
            RegexOptions.None);

        // Highlight keywords
        line = Regex.Replace(line,
            @"\b(import|from|as|return|if|elif|else|for|while|break|continue|pass|yield|raise|try|except|finally|with|assert|lambda|None|True|False|and|or|not|in|is|async|await|global|nonlocal)\b",
            "[blue]$1[/]",
            RegexOptions.None);

        // Highlight self/cls
        line = Regex.Replace(line,
            @"\b(self|cls)\b",
            "[cyan]$1[/]",
            RegexOptions.None);

        // Highlight common types
        line = Regex.Replace(line,
            @"\b(int|str|float|bool|list|dict|tuple|set|None)\b",
            "[green]$1[/]",
            RegexOptions.None);

        return line;
    }
}