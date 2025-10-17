using Codex_Tree.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Codex_Tree.Visualization;

/// <summary>
/// Builds details panel for selected class
/// </summary>
public class DetailsPanel
{
    private const int MaxPathWidth = 80;
    private readonly string? _baseDirectory;
    private readonly TreeLineBuilder _treeBuilder;

    public DetailsPanel(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory;
        _treeBuilder = new TreeLineBuilder();
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

        // Namespace (dimmed, above name)
        if (!string.IsNullOrEmpty(classInfo.Namespace))
            grid.AddRow($"[dim]{classInfo.Namespace}[/]");

        // Name with modifiers as badges
        var badges = BuildModifierBadges(classInfo);
        grid.AddRow($"[bold][{color}]{classInfo.Name}[/][/]{badges}");
        grid.AddEmptyRow();

        // Depth in hierarchy
        if (node.Depth > 0)
        {
            grid.AddRow($"[bold]Depth:[/] {node.Depth}");
        }

        // Base class
        if (!string.IsNullOrEmpty(classInfo.BaseClass))
        {
            grid.AddRow($"[bold]Base:[/] {classInfo.BaseClass}");
        }

        // Add empty row after depth/base section if either exists
        if (node.Depth > 0 || !string.IsNullOrEmpty(classInfo.BaseClass))
        {
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

        // Subtree with counts
        if (node.Children.Count > 0 || node.NestedClasses.Count > 0)
        {
            var totalDescendants = CalculateTotalDescendants(node);
            var directChildren = node.Children.Count;
            var nestedClasses = node.NestedClasses.Count;

            grid.AddRow($"[bold]Subtree:[/] {totalDescendants} total ({directChildren} children, {nestedClasses} nested)");
            var subtreeMarkup = BuildSubtreeMarkup(node);
            grid.AddRow(subtreeMarkup);
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
        var displayPath = GetRelativePath(classInfo.FilePath);
        if (!string.IsNullOrEmpty(displayPath))
        {
            // Use Spectre's Text with Overflow.Fold for automatic wrapping
            var pathText = new Text(displayPath, new Style(Color.Grey))
            {
                Overflow = Overflow.Fold
            };
            grid.AddRow(pathText);

            // Add file size below path
            if (!string.IsNullOrEmpty(classInfo.FilePath) && File.Exists(classInfo.FilePath))
            {
                var fileInfo = new FileInfo(classInfo.FilePath);
                var fileSize = FormatFileSize(fileInfo.Length);
                grid.AddRow($"[dim]{fileSize}[/]");
            }
        }

        return grid;
    }

    /// <summary>
    /// Build subtree markup using Spectre.Console Tree
    /// </summary>
    private IRenderable BuildSubtreeMarkup(InheritanceNode node)
    {
        // Build a tree starting from this node's children
        var tempTree = new Tree("");
        BuildSubtreeRecursive(tempTree, node);

        return tempTree;
    }

    /// <summary>
    /// Recursively build subtree structure
    /// </summary>
    private void BuildSubtreeRecursive(IHasTreeNodes parentTree, InheritanceNode parentNode)
    {
        // Add inherited children
        foreach (var child in parentNode.Children)
        {
            var color = TreeLineBuilder.GetClassColor(child.ClassInfo);
            var modifier = TreeLineBuilder.FormatModifier(child.ClassInfo.IsAbstract);
            var label = $"[{color}]{child.ClassInfo.Name}[/]{modifier}";

            var childTreeNode = parentTree.AddNode(label);
            BuildSubtreeRecursive(childTreeNode, child);
        }

        // Add nested classes
        foreach (var nested in parentNode.NestedClasses)
        {
            var color = TreeLineBuilder.GetClassColor(nested.ClassInfo);
            var modifier = TreeLineBuilder.FormatModifier(nested.ClassInfo.IsAbstract);
            var nestedIndicator = TreeLineBuilder.FormatNestedIndicator(true);
            var label = $"[{color}]{nested.ClassInfo.Name}[/]{modifier}{nestedIndicator}";

            var nestedTreeNode = parentTree.AddNode(label);
            BuildSubtreeRecursive(nestedTreeNode, nested);
        }
    }

    /// <summary>
    /// Build modifier badges for class type
    /// </summary>
    private static string BuildModifierBadges(ClassInfo classInfo)
    {
        var badges = new List<string>();

        if (classInfo.IsAbstract)
            badges.Add("[black on yellow] ABSTRACT [/]");
        if (classInfo.IsSealed)
            badges.Add("[black on blue] SEALED [/]");
        if (classInfo.IsStatic)
            badges.Add("[black on cyan] STATIC [/]");

        return badges.Count > 0 ? " " + string.Join(" ", badges) : "";
    }

    /// <summary>
    /// Calculate total descendants recursively (all children and nested classes)
    /// </summary>
    private static int CalculateTotalDescendants(InheritanceNode node)
    {
        int count = node.Children.Count + node.NestedClasses.Count;

        foreach (var child in node.Children)
        {
            count += CalculateTotalDescendants(child);
        }

        foreach (var nested in node.NestedClasses)
        {
            count += CalculateTotalDescendants(nested);
        }

        return count;
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

    /// <summary>
    /// Format file size in human-readable format
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}