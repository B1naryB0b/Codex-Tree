// See https://aka.ms/new-console-template for more information

using Codex_Tree.Analysis;
using Codex_Tree.Models;
using Codex_Tree.Parsers;
using Codex_Tree.UI;
using Codex_Tree.Visualization;
using Spectre.Console;

// Try different built-in fonts
AnsiConsole.Write(
    new FigletText("Codex Tree")
        .Centered()
        .Color(Color.White));

AnsiConsole.WriteLine();

// Get directory to analyze using DirectoryManager
var directoryManager = new DirectoryManager();
var directory = directoryManager.SelectDirectory();

if (string.IsNullOrEmpty(directory))
{
    AnsiConsole.MarkupLine("[yellow]No directory selected. Exiting...[/]");
    return;
}

AnsiConsole.Clear();
AnsiConsole.Write(
    new FigletText("Codex Tree")
        .Centered()
        .Color(Color.White));
AnsiConsole.WriteLine();

// Analyze directory for languages
var languageManager = new LanguageManager();
Dictionary<string, int>? languageStats = null;

AnsiConsole.Status()
    .Start("Analyzing directory...", ctx =>
    {
        ctx.Spinner(Spinner.Known.Dots);
        ctx.SpinnerStyle(Style.Parse("cyan"));

        languageStats = languageManager.AnalyzeDirectoryStats(directory);
    });

// Clear and show language selection
AnsiConsole.Clear();
AnsiConsole.Write(
    new FigletText("Codex Tree")
        .Centered()
        .Color(Color.White));
AnsiConsole.WriteLine();

var selectedLanguage = languageManager.PromptLanguageSelection(languageStats!);

if (string.IsNullOrEmpty(selectedLanguage))
{
    AnsiConsole.MarkupLine("[yellow]No language selected. Exiting...[/]");
    return;
}

AnsiConsole.Clear();
AnsiConsole.Write(
    new FigletText("Codex Tree")
        .Centered()
        .Color(Color.White));
AnsiConsole.WriteLine();

// Get parser for selected language
var parser = ParserFactory.GetParser(selectedLanguage);
if (parser == null)
{
    AnsiConsole.MarkupLine($"[red]No parser available for {selectedLanguage}![/]");
    return;
}

// Parse the directory
List<InheritanceNode>? roots = null;

AnsiConsole.Status()
    .Start($"Parsing {selectedLanguage} files...", ctx =>
    {
        ctx.Spinner(Spinner.Known.Dots);
        ctx.SpinnerStyle(Style.Parse("green"));

        var classes = parser.ParseDirectory(directory, recursive: true, progressCallback: (current, total) =>
        {
            ctx.Status($"Parsing {selectedLanguage} files... ({current}/{total})");
        });

        if (classes.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No {selectedLanguage} classes found in the directory![/]");
            return;
        }

        AnsiConsole.MarkupLine($"[green]Found {classes.Count} classes[/]");

        // Build inheritance tree
        ctx.Status("Building inheritance tree...");
        var treeBuilder = new InheritanceTreeBuilder();
        roots = treeBuilder.BuildTree(classes);
    });

// Check if we have roots to display
if (roots == null || roots.Count == 0)
{
    AnsiConsole.MarkupLine("[yellow]No inheritance trees to display.[/]");
    return;
}

// Calculate statistics
var statistics = new TreeStatistics();
var stats = statistics.CalculateStatistics(roots);
var statsGrid = statistics.BuildStatisticsGrid(stats);

// Render the tree with statistics (interactive, outside of status block)
AnsiConsole.Clear();
AnsiConsole.Write(
    new FigletText("Codex Tree")
        .Centered()
        .Color(Color.White));
AnsiConsole.WriteLine();

var renderer = new TreeRenderer();
renderer.RenderTree(roots, baseDirectory: directory, statsGrid: statsGrid);

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[dim]Press any key to exit...[/]");
Console.ReadKey();