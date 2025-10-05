using Codex_Tree.Models;
using Spectre.Console;

namespace Codex_Tree.Analysis;

/// <summary>
/// Calculates and displays statistics about the inheritance tree
/// </summary>
public class TreeStatistics
{
    /// <summary>
    /// Calculate statistics for the entire tree
    /// </summary>
    public TreeStats CalculateStatistics(List<InheritanceNode> roots)
    {
        var stats = new TreeStats();

        foreach (var root in roots)
        {
            CalculateNodeStatistics(root, stats);
        }

        return stats;
    }

    private void CalculateNodeStatistics(InheritanceNode node, TreeStats stats)
    {
        stats.TotalClasses++;

        if (node.ClassInfo.IsAbstract)
            stats.AbstractClasses++;

        if (node.Depth > stats.MaxDepth)
        {
            stats.MaxDepth = node.Depth;
        }

        // Check for deep inheritance (3+ levels)
        if (node.Depth >= 3)
        {
            stats.DeepInheritanceClasses.Add(node.ClassInfo);
        }

        // Find deepest chain(s) - only for non-nested classes
        if (!node.ClassInfo.IsNested)
        {
            var chainDepth = node.Depth + node.MaxDepthBelow();
            if (chainDepth > stats.DeepestChains.Count)
            {
                stats.DeepestChains.Clear();
                stats.DeepestChains.Add(node.GetInheritanceChain());
            }
            else if (chainDepth == stats.DeepestChains.Count && stats.DeepestChains.Count > 0)
            {
                // Add to the list if same length
                stats.DeepestChains.Add(node.GetInheritanceChain());
            }
        }

        // Find largest class
        if (node.ClassInfo.LineCount > stats.LargestClassLines)
        {
            stats.LargestClassLines = node.ClassInfo.LineCount;
            stats.LargestClass = node.ClassInfo;
        }

        // Recurse to inherited children
        foreach (var child in node.Children)
        {
            CalculateNodeStatistics(child, stats);
        }

        // Also process nested classes for statistics (but they won't affect chain depth)
        foreach (var nested in node.NestedClasses)
        {
            CalculateNodeStatistics(nested, stats);
        }
    }

    /// <summary>
    /// Build statistics grid
    /// </summary>
    public Grid BuildStatisticsGrid(TreeStats stats)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow("[bold]Total classes:[/]", $"{stats.TotalClasses}");
        grid.AddRow("[bold]Abstract classes:[/]", $"{stats.AbstractClasses}");
        grid.AddRow("[bold]Max depth:[/]", $"{stats.MaxDepth} levels");

        if (stats.LargestClass != null)
        {
            grid.AddRow("[bold]Largest class:[/]", $"{stats.LargestClass.Name} ({stats.LargestClassLines} lines)");
        }

        // Add warnings
        if (stats.DeepInheritanceClasses.Count > 0)
        {
            grid.AddEmptyRow();
            grid.AddRow($"[yellow]Deep inheritance:[/]", $"[yellow]{stats.DeepInheritanceClasses.Count} classes (3+ levels)[/]");
        }

        var panel = new Panel(grid)
        {
            Header = new PanelHeader("Statistics", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Expand = false
        };

        var containerGrid = new Grid();
        containerGrid.AddColumn();
        containerGrid.AddRow(panel);

        return containerGrid;
    }

    /// <summary>
    /// Render statistics panel
    /// </summary>
    public void RenderStatistics(TreeStats stats)
    {
        var grid = BuildStatisticsGrid(stats);
        AnsiConsole.Write(grid);

        // Show detailed warnings
        if (stats.DeepInheritanceClasses.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]Classes with deep inheritance (3+ levels):[/]");

            if (stats.DeepInheritanceClasses.Count <= 10)
            {
                foreach (var classInfo in stats.DeepInheritanceClasses)
                {
                    AnsiConsole.MarkupLine($"   [dim]- {classInfo.FullName}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"   [dim](showing first 10)[/]");
                foreach (var classInfo in stats.DeepInheritanceClasses.Take(10))
                {
                    AnsiConsole.MarkupLine($"   [dim]- {classInfo.FullName}[/]");
                }
            }
        }
    }
}

/// <summary>
/// Container for tree statistics
/// </summary>
public class TreeStats
{
    public int TotalClasses { get; set; }
    public int AbstractClasses { get; set; }
    public int MaxDepth { get; set; }
    public ClassInfo? LargestClass { get; set; }
    public int LargestClassLines { get; set; }
    public List<List<InheritanceNode>> DeepestChains { get; set; } = new();
    public List<ClassInfo> DeepInheritanceClasses { get; set; } = new();
}