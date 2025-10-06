using Spectre.Console;

namespace Codex_Tree.UI;

/// <summary>
/// Manages language detection and selection for code analysis
/// </summary>
public class LanguageManager
{
    private static readonly Dictionary<string, string> LanguageExtensions = new()
    {
        { "C#", ".cs" },
        { "Python", ".py" },
        { "JavaScript", ".js" },
        { "TypeScript", ".ts" },
        { "Java", ".java" },
        { "C++", ".cpp" },
        { "C", ".c" },
        { "Go", ".go" },
        { "Rust", ".rs" },
        { "Ruby", ".rb" },
        { "PHP", ".php" },
        { "Swift", ".swift" },
        { "Kotlin", ".kt" }
    };

    /// <summary>
    /// Analyze directory and prompt user to select a language
    /// </summary>
    public string? SelectLanguage(string directoryPath)
    {
        var languageStats = AnalyzeDirectory(directoryPath);
        return PromptLanguageSelection(languageStats);
    }

    /// <summary>
    /// Analyze directory and return language statistics
    /// </summary>
    public Dictionary<string, int> AnalyzeDirectoryStats(string directoryPath)
    {
        return AnalyzeDirectory(directoryPath);
    }

    /// <summary>
    /// Prompt user to select a language from pre-analyzed stats
    /// </summary>
    public string? PromptLanguageSelection(Dictionary<string, int> languageStats)
    {
        if (languageStats.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No supported language files found in directory![/]");
            Console.ReadKey();
            return null;
        }

        // Display breakdown chart
        DisplayLanguageBreakdown(languageStats);

        // Create selection prompt with languages sorted by file count
        var sortedLanguages = languageStats
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        sortedLanguages.Add("[dim]Cancel[/]");

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("\n[bold]Select a language to analyze:[/]")
                .PageSize(15)
                .AddChoices(sortedLanguages));

        if (selection.Contains("Cancel"))
            return null;

        return selection;
    }

    /// <summary>
    /// Analyze directory and count files by language
    /// </summary>
    private Dictionary<string, int> AnalyzeDirectory(string directoryPath)
    {
        var languageCounts = new Dictionary<string, int>();

        foreach (var kvp in LanguageExtensions)
        {
            var language = kvp.Key;
            var extension = kvp.Value;

            var fileCount = Directory
                .GetFiles(directoryPath, $"*{extension}", SearchOption.AllDirectories)
                .Count(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\") &&
                            !f.Contains(@"\node_modules\") && !f.Contains(@"\.git\"));

            if (fileCount > 0)
            {
                languageCounts[language] = fileCount;
            }
        }

        return languageCounts;
    }

    /// <summary>
    /// Display a breakdown chart of languages found
    /// </summary>
    private void DisplayLanguageBreakdown(Dictionary<string, int> languageStats)
    {
        var totalFiles = languageStats.Sum(kvp => kvp.Value);

        var chart = new BreakdownChart()
            .Width(60)
            .HideTagValues();

        foreach (var kvp in languageStats.OrderByDescending(x => x.Value))
        {
            var percentage = (kvp.Value * 100.0 / totalFiles);
            var color = GetLanguageColor(kvp.Key);
            var label = $"{kvp.Key} ({kvp.Value}) {percentage:F1}%";
            chart.AddItem(label, percentage, color);
        }

        AnsiConsole.Write(new Panel(chart)
            .Header("[bold underline]Language Distribution[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.White));

        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Get a color for language visualization
    /// </summary>
    private Color GetLanguageColor(string language)
    {
        return language switch
        {
            "C#" => Color.Green,
            "Python" => Color.Blue,
            "JavaScript" => Color.Yellow,
            "TypeScript" => Color.Cyan1,
            "Java" => Color.Red,
            "C++" => Color.Purple,
            "C" => Color.Grey,
            "Go" => Color.Cyan3,
            "Rust" => Color.Orange1,
            "Ruby" => Color.Red1,
            "PHP" => Color.Purple_1,
            "Swift" => Color.Orange3,
            "Kotlin" => Color.Purple_1,
            _ => Color.White
        };
    }
}