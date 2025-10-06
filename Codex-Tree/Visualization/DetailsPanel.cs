using Codex_Tree.Models;
using Spectre.Console;

namespace Codex_Tree.Visualization;

/// <summary>
/// Builds details panel for selected class
/// </summary>
public class DetailsPanel
{
    private const int MaxSubtreeDepth = 12;
    private const int MaxPathWidth = 80;
    private readonly string? _baseDirectory;

    public DetailsPanel(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory;
    }

    /// <summary>
    /// Build details grid for selected class
    /// </summary>
    public Grid BuildDetails(InheritanceNode node)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(MaxPathWidth));

        var classInfo = node.ClassInfo;
        var color = TreeLineBuilder.GetClassColor(classInfo);
        var modifier = TreeLineBuilder.FormatModifier(classInfo.IsAbstract);

        // Namespace (dimmed, above name)
        if (!string.IsNullOrEmpty(classInfo.Namespace))
            grid.AddRow($"[dim]{classInfo.Namespace}[/]");

        // Name
        grid.AddRow($"[bold][{color}]{classInfo.Name}[/]{modifier}[/]");
        grid.AddEmptyRow();

        // Base class
        if (!string.IsNullOrEmpty(classInfo.BaseClass))
        {
            grid.AddRow($"[bold]Base:[/] {classInfo.BaseClass}");
            grid.AddEmptyRow();
        }

        // Interfaces
        if (classInfo.Interfaces.Count > 0)
        {
            grid.AddRow($"[bold]Interfaces:[/]");
            foreach (var iface in classInfo.Interfaces)
            {
                var escapedInterface = iface.Replace("[", "[[").Replace("]", "]]");
                grid.AddRow($"  - {escapedInterface}");
            }
            grid.AddEmptyRow();
        }

        // Subtree
        if (node.Children.Count > 0 || node.NestedClasses.Count > 0)
        {
            grid.AddRow("[bold]Subtree:[/]");
            AddSubtree(grid, node, "");
            grid.AddEmptyRow();
        }

        // Methods count
        if (classInfo.MethodCount > 0)
        {
            grid.AddRow($"[bold]Methods:[/] {classInfo.MethodCount}");
            grid.AddEmptyRow();
        }

        // Lines count
        if (classInfo.LineCount > 0)
        {
            grid.AddRow($"[bold]Lines:[/] {classInfo.LineCount}");
            grid.AddEmptyRow();
        }

        // Path pinned to bottom
        grid.AddRow("─".PadRight(MaxPathWidth, '─')); // Separator line
        var displayPath = GetRelativePath(classInfo.FilePath);
        if (!string.IsNullOrEmpty(displayPath))
        {
            // Use Spectre's Text with Overflow.Fold for automatic wrapping
            var pathText = new Text(displayPath, new Style(Color.Grey))
            {
                Overflow = Overflow.Fold
            };
            grid.AddRow(pathText);
        }

        return grid;
    }

    /// <summary>
    /// Add subtree to the details grid
    /// </summary>
    private void AddSubtree(Grid grid, InheritanceNode node, string indent)
    {
        var allChildren = CombineChildren(node);

        foreach (var (child, isNested) in allChildren)
        {
            var color = TreeLineBuilder.GetClassColor(child.ClassInfo);
            var modifier = TreeLineBuilder.FormatModifier(child.ClassInfo.IsAbstract);
            var nestedIndicator = TreeLineBuilder.FormatNestedIndicator(isNested);

            grid.AddRow($"{indent}├─ [{color}]{child.ClassInfo.Name}[/]{modifier}{nestedIndicator}");

            // Recursively add children up to a reasonable depth
            if ((child.Children.Count > 0 || child.NestedClasses.Count > 0) && indent.Length < MaxSubtreeDepth)
            {
                AddSubtree(grid, child, indent + "│  ");
            }
        }
    }

    private static List<(InheritanceNode node, bool isNested)> CombineChildren(InheritanceNode node)
    {
        var allChildren = new List<(InheritanceNode node, bool isNested)>();

        foreach (var child in node.Children)
            allChildren.Add((child, false));

        foreach (var nested in node.NestedClasses)
            allChildren.Add((nested, true));

        return allChildren;
    }

    /// <summary>
    /// Get relative path from base directory
    /// </summary>
    private string GetRelativePath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return "";

        if (string.IsNullOrEmpty(_baseDirectory))
            return filePath;

        try
        {
            var fullBasePath = Path.GetFullPath(_baseDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullFilePath = Path.GetFullPath(filePath);

            if (fullFilePath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
            {
                var relative = fullFilePath[fullBasePath.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return ".../" + relative.Replace('\\', '/');
            }

            return filePath;
        }
        catch
        {
            return filePath;
        }
    }

}