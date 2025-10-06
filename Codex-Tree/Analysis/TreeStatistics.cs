using Codex_Tree.Models;
using Spectre.Console;

namespace Codex_Tree.Analysis;

/// <summary>
/// Calculates and displays statistics about the inheritance tree
/// </summary>
public class TreeStatistics
{
    private const int DeepInheritanceThreshold = 3;
    private const int MaxDetailedWarnings = 10;

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

    /// <summary>
    /// Build statistics grid with breakdown chart
    /// </summary>
    public Grid BuildStatisticsGrid(TreeStats stats)
    {
        var statsPanel = BuildStatisticsPanel(stats);
        var chartPanel = BuildBreakdownChartPanel(stats);

        var containerGrid = new Grid();
        containerGrid.AddColumn();
        containerGrid.AddColumn();
        containerGrid.AddRow(statsPanel, chartPanel);

        return containerGrid;
    }

    private void CalculateNodeStatistics(InheritanceNode node, TreeStats stats)
    {
        stats.TotalClasses++;

        if (node.ClassInfo.IsNested)
            stats.NestedClasses++;

        CountClassType(node, stats);
        UpdateMaxDepth(node, stats);
        TrackDeepInheritance(node, stats);
        UpdateDeepestChain(node, stats);
        UpdateLargestClass(node, stats);

        // Recurse to children
        foreach (var child in node.Children)
            CalculateNodeStatistics(child, stats);

        foreach (var nested in node.NestedClasses)
            CalculateNodeStatistics(nested, stats);
    }

    private static void CountClassType(InheritanceNode node, TreeStats stats)
    {
        if (node.ClassInfo.IsAbstract)
        {
            stats.AbstractClasses++;
            stats.AbstractCount++;
        }
        else if (node.ClassInfo.IsSealed)
            stats.SealedCount++;
        else if (node.ClassInfo.IsStatic)
            stats.StaticCount++;
        else
            stats.NormalCount++;
    }

    private static void UpdateMaxDepth(InheritanceNode node, TreeStats stats)
    {
        if (node.Depth > stats.MaxDepth)
            stats.MaxDepth = node.Depth;
    }

    private static void TrackDeepInheritance(InheritanceNode node, TreeStats stats)
    {
        if (node.Depth >= DeepInheritanceThreshold)
            stats.DeepInheritanceClasses.Add(node.ClassInfo);
    }

    private static void UpdateDeepestChain(InheritanceNode node, TreeStats stats)
    {
        if (node.ClassInfo.IsNested)
            return;

        var chainDepth = node.Depth + node.MaxDepthBelow();

        if (chainDepth > stats.DeepestChains.Count)
        {
            stats.DeepestChains.Clear();
            stats.DeepestChains.Add(node.GetInheritanceChain());
        }
        else if (chainDepth == stats.DeepestChains.Count && stats.DeepestChains.Count > 0)
        {
            stats.DeepestChains.Add(node.GetInheritanceChain());
        }
    }

    private static void UpdateLargestClass(InheritanceNode node, TreeStats stats)
    {
        if (node.ClassInfo.LineCount > stats.LargestClassLines)
        {
            stats.LargestClassLines = node.ClassInfo.LineCount;
            stats.LargestClass = node.ClassInfo;
        }
    }

    private static Panel BuildStatisticsPanel(TreeStats stats)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow("[bold]Total classes:[/]", $"{stats.TotalClasses}");
        grid.AddRow("[bold]Nested classes:[/]", $"{stats.NestedClasses}");
        grid.AddRow("[bold]Max depth:[/]", $"{stats.MaxDepth} levels");

        if (stats.LargestClass != null)
        {
            grid.AddRow("[bold]Largest class:[/]",
                $"{stats.LargestClass.Name} ({stats.LargestClassLines} lines)");
        }

        if (stats.DeepInheritanceClasses.Count > 0)
        {
            grid.AddEmptyRow();
            grid.AddRow("[yellow]Deep inheritance:[/]",
                $"[yellow]{stats.DeepInheritanceClasses.Count} classes ({DeepInheritanceThreshold}+ levels)[/]");
        }

        return new Panel(grid)
        {
            Header = new PanelHeader("Statistics", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Expand = false
        };
    }

    private static Panel BuildBreakdownChartPanel(TreeStats stats)
    {
        var chart = new BreakdownChart()
            .Width(40)
            .HideTagValues();

        AddChartItem(chart, "Normal", stats.NormalCount, stats.TotalClasses, Color.White);
        AddChartItem(chart, "Abstract", stats.AbstractCount, stats.TotalClasses, Color.Yellow);
        AddChartItem(chart, "Sealed", stats.SealedCount, stats.TotalClasses, Color.Blue);
        AddChartItem(chart, "Static", stats.StaticCount, stats.TotalClasses, Color.Cyan1);

        return new Panel(chart)
        {
            Header = new PanelHeader("Class Types", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Expand = false
        };
    }

    private static void AddChartItem(BreakdownChart chart, string label, int count, int total, Color color)
    {
        if (count <= 0)
            return;

        double percentage = (count / (double)total) * 100;
        chart.AddItem($"{label} ({count}) {percentage:F1}%", percentage, color);
    }

    private static void RenderDeepInheritanceWarnings(TreeStats stats)
    {
        if (stats.DeepInheritanceClasses.Count == 0)
            return;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[yellow]Classes with deep inheritance ({DeepInheritanceThreshold}+ levels):[/]");

        var classesToShow = stats.DeepInheritanceClasses.Take(MaxDetailedWarnings);

        foreach (var classInfo in classesToShow)
        {
            AnsiConsole.MarkupLine($"   [dim]- {classInfo.FullName}[/]");
        }

        if (stats.DeepInheritanceClasses.Count > MaxDetailedWarnings)
        {
            AnsiConsole.MarkupLine($"   [dim](showing first {MaxDetailedWarnings})[/]");
        }
    }
}

/// <summary>
/// Container for tree statistics
/// </summary>
public class TreeStats
{
    public int TotalClasses { get; set; }
    public int NestedClasses { get; set; }
    public int AbstractClasses { get; set; }
    public int MaxDepth { get; set; }
    public ClassInfo? LargestClass { get; set; }
    public int LargestClassLines { get; set; }
    public List<List<InheritanceNode>> DeepestChains { get; set; } = new();
    public List<ClassInfo> DeepInheritanceClasses { get; set; } = new();

    // Class type counts for breakdown chart
    public int NormalCount { get; set; }
    public int AbstractCount { get; set; }
    public int SealedCount { get; set; }
    public int StaticCount { get; set; }
}