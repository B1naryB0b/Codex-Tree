// See https://aka.ms/new-console-template for more information

using Codex_Tree.Analysis;
using Codex_Tree.Models;
using Codex_Tree.Parsers;
using Codex_Tree.UI;
using Codex_Tree.Visualization;
using Spectre.Console;

AnsiConsole.Write(
    new FigletText("Codex Tree")
        .Centered()
        .Color(Color.Green));

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
        .Color(Color.Green));
AnsiConsole.WriteLine();

// Parse the directory
List<InheritanceNode>? roots = null;

AnsiConsole.Status()
    .Start("Parsing C# files...", ctx =>
    {
        ctx.Spinner(Spinner.Known.Dots);
        ctx.SpinnerStyle(Style.Parse("green"));

        var parser = new CSharpParser();
        var classes = parser.ParseDirectory(directory);

        if (classes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No C# classes found in the directory![/]");
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
        .Color(Color.Green));
AnsiConsole.WriteLine();

var renderer = new TreeRenderer();
renderer.RenderTree(roots, baseDirectory: directory, statsGrid: statsGrid);

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[dim]Press any key to exit...[/]");
Console.ReadKey();