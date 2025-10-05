// See https://aka.ms/new-console-template for more information

using Codex_Tree.Analysis;
using Codex_Tree.Parsers;
using Codex_Tree.Visualization;
using Spectre.Console;

AnsiConsole.Write(
    new FigletText("Codex Tree")
        .Centered()
        .Color(Color.Green));

AnsiConsole.WriteLine();

// Get directory to analyze
var directory = AnsiConsole.Ask<string>(
    "Enter directory path to analyze (or press Enter for current directory):",
    ".");

if (!Directory.Exists(directory))
{
    AnsiConsole.MarkupLine("[red]Error: Directory not found![/]");
    return;
}

// Parse the directory
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
        AnsiConsole.WriteLine();

        // Build inheritance tree
        ctx.Status("Building inheritance tree...");
        var treeBuilder = new InheritanceTreeBuilder();
        var roots = treeBuilder.BuildTree(classes);

        // Render the tree
        AnsiConsole.Clear();
        AnsiConsole.Write(
            new FigletText("Codex Tree")
                .Centered()
                .Color(Color.Green));
        AnsiConsole.WriteLine();

        var renderer = new TreeRenderer();
        renderer.RenderTree(roots, baseDirectory: directory);

        AnsiConsole.WriteLine();

        // Calculate and display statistics
        var statistics = new TreeStatistics();
        var stats = statistics.CalculateStatistics(roots);
        statistics.RenderStatistics(stats);
    });

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[dim]Press any key to exit...[/]");
Console.ReadKey();