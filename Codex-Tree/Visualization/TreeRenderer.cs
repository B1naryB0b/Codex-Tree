using Codex_Tree.Models;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text;

namespace Codex_Tree.Visualization;

/// <summary>
/// Renders inheritance trees using Spectre.Console
/// </summary>
public class TreeRenderer
{
    private const int ClassNameColumnPadding = 4;
    private const int StatsColumnPadding = 4;
    private const int PathColumnPadding = 4;

    private string? _baseDirectory;
    private int _maxClassNameWidth;
    private int _maxStatsWidth;
    private List<string> _detailLines = new();

    /// <summary>
    /// Render the entire inheritance forest
    /// </summary>
    public void RenderTree(List<InheritanceNode> roots, string title = "Inheritance Tree", string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory;
        _detailLines.Clear();

        // Calculate maximum widths
        CalculateMaxWidths(roots);

        // Build tree structure and collect lines with node references
        var treeLines = new List<string>();
        var nodeList = new List<ClassInfo>();
        foreach (var root in roots)
        {
            BuildTreeLines(root, "", true, treeLines, nodeList);
        }

        // Start interactive mode
        RenderInteractive(treeLines, nodeList, title);
    }

    /// <summary>
    /// Render interactive tree with selection
    /// </summary>
    private void RenderInteractive(List<string> treeLines, List<ClassInfo> nodeList, string title)
    {
        int selectedIndex = 0;

        // Calculate max tree line width (without markup) - do this once before the loop
        int maxTreeWidth = 0;
        foreach (var line in treeLines)
        {
            var plainText = System.Text.RegularExpressions.Regex.Replace(line, @"\[.*?\]", "");
            maxTreeWidth = Math.Max(maxTreeWidth, plainText.Length);
        }

        int scrollOffset = 0;
        const int viewportHeight = 25; // Number of lines to show at once

        while (true)
        {
            AnsiConsole.Clear();

            // Calculate scroll offset to keep selected item visible
            if (selectedIndex < scrollOffset)
            {
                scrollOffset = selectedIndex;
            }
            else if (selectedIndex >= scrollOffset + viewportHeight)
            {
                scrollOffset = selectedIndex - viewportHeight + 1;
            }

            // Build tree with selection highlight and scrolling
            var treeText = new StringBuilder();
            int endIndex = Math.Min(scrollOffset + viewportHeight, treeLines.Count);
            for (int i = scrollOffset; i < endIndex; i++)
            {
                if (i == selectedIndex)
                {
                    // Strip markup tags for selection to show plain text
                    var plainText = System.Text.RegularExpressions.Regex.Replace(treeLines[i], @"\[.*?\]", "");
                    treeText.AppendLine($"[black on white]{plainText}[/]");
                }
                else
                {
                    treeText.AppendLine(treeLines[i]);
                }
            }

            // Build details grid for selected class
            var selectedClass = nodeList[selectedIndex];
            var detailsGrid = BuildDetailsGrid(selectedClass);

            // Create table with two columns
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.BorderStyle = new Style(Color.Green);
            table.Title = new TableTitle($"{title} - Use ↑/↓ arrows, Q to quit");
            table.Expand = true; // Fill terminal width
            table.AddColumn(new TableColumn("[green]Inheritance Tree[/]").Width(maxTreeWidth).NoWrap());
            table.AddColumn(new TableColumn("[blue]Details[/]").NoWrap());

            table.AddRow(new Markup(treeText.ToString().TrimEnd()), detailsGrid);

            AnsiConsole.Write(table);

            // Handle input
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.UpArrow && selectedIndex > 0)
            {
                selectedIndex--;
            }
            else if (key.Key == ConsoleKey.DownArrow && selectedIndex < treeLines.Count - 1)
            {
                selectedIndex++;
            }
            else if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Build details grid for selected class
    /// </summary>
    private Grid BuildDetailsGrid(ClassInfo classInfo)
    {
        var grid = new Grid();
        grid.AddColumn();

        var color = GetClassColor(classInfo);
        var modifier = classInfo.IsAbstract ? " (abstract)" : "";
        var nameText = classInfo.IsNested ? $"> {classInfo.Name}" : classInfo.Name;

        grid.AddRow($"[bold][{color}]{nameText}[/]{modifier}[/]");
        grid.AddEmptyRow();

        if (!string.IsNullOrEmpty(classInfo.Namespace))
            grid.AddRow($"[dim]Namespace:[/] {classInfo.Namespace}");

        if (!string.IsNullOrEmpty(classInfo.BaseClass))
            grid.AddRow($"[dim]Base Class:[/] {classInfo.BaseClass}");

        if (classInfo.Interfaces.Count > 0)
            grid.AddRow($"[dim]Interfaces:[/] {string.Join(", ", classInfo.Interfaces)}");

        grid.AddEmptyRow();

        if (classInfo.MethodCount > 0)
            grid.AddRow($"[dim]Methods:[/] {classInfo.MethodCount}");

        if (classInfo.LineCount > 0)
            grid.AddRow($"[dim]Lines:[/] {classInfo.LineCount}");

        grid.AddEmptyRow();

        var relativePath = GetRelativePath(classInfo.FilePath);
        if (!string.IsNullOrEmpty(relativePath))
            grid.AddRow($"[dim]Path:[/] {relativePath}");
        else if (!string.IsNullOrEmpty(classInfo.FilePath))
            grid.AddRow($"[dim]Path:[/] {classInfo.FilePath}");

        return grid;
    }

    /// <summary>
    /// Calculate maximum widths for alignment
    /// </summary>
    private void CalculateMaxWidths(List<InheritanceNode> roots)
    {
        _maxClassNameWidth = 0;
        _maxStatsWidth = 0;

        foreach (var root in roots)
        {
            CalculateNodeWidths(root);
        }
    }

    /// <summary>
    /// Calculate widths for a node and its children
    /// </summary>
    private void CalculateNodeWidths(InheritanceNode node)
    {
        var classNameLength = node.ClassInfo.Name.Length + (node.ClassInfo.IsAbstract ? 11 : 0); // " (abstract)"
        if (node.ClassInfo.IsNested)
            classNameLength += 2; // "> "

        _maxClassNameWidth = Math.Max(_maxClassNameWidth, classNameLength);

        var statsParts = new List<string>();
        if (node.ClassInfo.MethodCount > 0)
            statsParts.Add($"{node.ClassInfo.MethodCount} methods");
        if (node.ClassInfo.LineCount > 0)
            statsParts.Add($"{node.ClassInfo.LineCount} lines");

        if (statsParts.Count > 0)
        {
            var statsLength = string.Join(", ", statsParts).Length + 2; // parentheses
            _maxStatsWidth = Math.Max(_maxStatsWidth, statsLength);
        }

        foreach (var child in node.Children)
            CalculateNodeWidths(child);

        foreach (var nested in node.NestedClasses)
            CalculateNodeWidths(nested);
    }

    /// <summary>
    /// Build tree lines with proper indentation and connectors
    /// </summary>
    private void BuildTreeLines(InheritanceNode node, string indent, bool isLast, List<string> lines, List<ClassInfo> nodeList)
    {
        var color = GetClassColor(node.ClassInfo);
        var modifier = node.ClassInfo.IsAbstract ? " (abstract)" : "";

        string connector = isLast ? "└── " : "├── ";
        string text = $"{indent}{connector}[{color}]{node.ClassInfo.Name}[/]{modifier}";

        lines.Add(text);
        nodeList.Add(node.ClassInfo);

        string childIndent = indent + (isLast ? "    " : "│   ");

        // Combine children and nested classes
        var allChildren = new List<(InheritanceNode node, bool isNested)>();
        foreach (var child in node.Children)
            allChildren.Add((child, false));
        foreach (var nested in node.NestedClasses)
            allChildren.Add((nested, true));

        for (int i = 0; i < allChildren.Count; i++)
        {
            bool isLastChild = (i == allChildren.Count - 1);
            var (child, isNested) = allChildren[i];

            if (isNested)
            {
                // Add nested class indicator
                var nestedColor = GetClassColor(child.ClassInfo);
                var nestedModifier = child.ClassInfo.IsAbstract ? " (abstract)" : "";
                string nestedConnector = isLastChild ? "└── " : "├── ";
                string nestedText = $"{childIndent}{nestedConnector}[dim]>[/] [{nestedColor}]{child.ClassInfo.Name}[/]{nestedModifier}";
                lines.Add(nestedText);
                nodeList.Add(child.ClassInfo);

                // Add nested class's children
                string nestedChildIndent = childIndent + (isLastChild ? "    " : "│   ");
                var nestedChildren = new List<(InheritanceNode node, bool isNested)>();
                foreach (var nestedChild in child.Children)
                    nestedChildren.Add((nestedChild, false));
                foreach (var nestedNested in child.NestedClasses)
                    nestedChildren.Add((nestedNested, true));

                for (int j = 0; j < nestedChildren.Count; j++)
                {
                    BuildTreeLines(nestedChildren[j].node, nestedChildIndent, j == nestedChildren.Count - 1, lines, nodeList);
                }
            }
            else
            {
                BuildTreeLines(child, childIndent, isLastChild, lines, nodeList);
            }
        }
    }

    /// <summary>
    /// Format a simple class node (just name and color)
    /// </summary>
    private Markup FormatSimpleClassNode(ClassInfo classInfo)
    {
        var color = GetClassColor(classInfo);
        var modifier = classInfo.IsAbstract ? " (abstract)" : "";
        var text = $"[{color}]{classInfo.Name}[/]{modifier}";

        // Add detail line for right panel
        AddDetailLine(classInfo, false);

        return new Markup(text);
    }

    /// <summary>
    /// Format a simple nested class node
    /// </summary>
    private Markup FormatSimpleNestedClassNode(ClassInfo classInfo)
    {
        var color = GetClassColor(classInfo);
        var modifier = classInfo.IsAbstract ? " (abstract)" : "";
        var text = $"[dim]>[/] [{color}]{classInfo.Name}[/]{modifier}";

        // Add detail line for right panel
        AddDetailLine(classInfo, true);

        return new Markup(text);
    }

    /// <summary>
    /// Add a detail line for the right panel
    /// </summary>
    private void AddDetailLine(ClassInfo classInfo, bool isNested)
    {
        _detailLines.Add("");
    }

    /// <summary>
    /// Recursively add simple children (just names)
    /// </summary>
    private void AddSimpleChildren(TreeNode parentNode, InheritanceNode parent)
    {
        // Add inherited children first
        foreach (var child in parent.Children)
        {
            var childNode = parentNode.AddNode(FormatSimpleClassNode(child.ClassInfo));
            AddSimpleChildren(childNode, child);
        }

        // Add nested classes after inherited children
        foreach (var nested in parent.NestedClasses)
        {
            var nestedNode = parentNode.AddNode(FormatSimpleNestedClassNode(nested.ClassInfo));
            AddSimpleChildren(nestedNode, nested);
        }
    }

    /// <summary>
    /// Recursively add children to tree nodes
    /// </summary>
    private void AddChildren(TreeNode parentNode, InheritanceNode parent)
    {
        // Add inherited children first
        foreach (var child in parent.Children)
        {
            var childNode = parentNode.AddNode(FormatClassNode(child.ClassInfo));
            AddChildren(childNode, child);
        }

        // Add nested classes after inherited children
        foreach (var nested in parent.NestedClasses)
        {
            var nestedNode = parentNode.AddNode(FormatNestedClassNode(nested.ClassInfo));
            AddChildren(nestedNode, nested);
        }
    }

    /// <summary>
    /// Format a class node with colors and metadata
    /// </summary>
    private Markup FormatClassNode(ClassInfo classInfo)
    {
        var color = GetClassColor(classInfo);
        var modifier = classInfo.IsAbstract ? " (abstract)" : "";
        var classNameText = $"{classInfo.Name}{modifier}";
        var classNameLength = classNameText.Length;

        var statsParts = new List<string>();
        if (classInfo.MethodCount > 0)
            statsParts.Add($"{classInfo.MethodCount} methods");
        if (classInfo.LineCount > 0)
            statsParts.Add($"{classInfo.LineCount} lines");

        var statsText = statsParts.Count > 0 ? $"({string.Join(", ", statsParts)})" : "";
        var statsLength = statsText.Length;

        var relativePath = GetRelativePath(classInfo.FilePath);

        // Calculate padding
        var classNamePadding = Math.Max(0, _maxClassNameWidth - classNameLength + ClassNameColumnPadding);
        var statsPadding = Math.Max(0, _maxStatsWidth - statsLength + StatsColumnPadding);

        var formatted = $"[{color}]{classInfo.Name}[/]{modifier}".PadRight(classNameText.Length + classNamePadding);
        if (!string.IsNullOrEmpty(statsText))
            formatted += $"[dim]{statsText}[/]".PadRight(statsText.Length + statsPadding + PathColumnPadding);
        else
            formatted += new string(' ', _maxStatsWidth + StatsColumnPadding + PathColumnPadding);

        if (!string.IsNullOrEmpty(relativePath))
            formatted += $"[dim]{relativePath}[/]";

        return new Markup(formatted);
    }

    /// <summary>
    /// Format a nested class node with > symbol
    /// </summary>
    private Markup FormatNestedClassNode(ClassInfo classInfo)
    {
        var color = GetClassColor(classInfo);
        var modifier = classInfo.IsAbstract ? " (abstract)" : "";
        var classNameText = $"> {classInfo.Name}{modifier}";
        var classNameLength = classNameText.Length;

        var statsParts = new List<string>();
        if (classInfo.MethodCount > 0)
            statsParts.Add($"{classInfo.MethodCount} methods");
        if (classInfo.LineCount > 0)
            statsParts.Add($"{classInfo.LineCount} lines");

        var statsText = statsParts.Count > 0 ? $"({string.Join(", ", statsParts)})" : "";
        var statsLength = statsText.Length;

        var relativePath = GetRelativePath(classInfo.FilePath);

        // Calculate padding
        var classNamePadding = Math.Max(0, _maxClassNameWidth - classNameLength + ClassNameColumnPadding);
        var statsPadding = Math.Max(0, _maxStatsWidth - statsLength + StatsColumnPadding);

        var formatted = $"[dim]>[/] [{color}]{classInfo.Name}[/]{modifier}".PadRight(classNameText.Length + classNamePadding);
        if (!string.IsNullOrEmpty(statsText))
            formatted += $"[dim]{statsText}[/]".PadRight(statsText.Length + statsPadding + PathColumnPadding);
        else
            formatted += new string(' ', _maxStatsWidth + StatsColumnPadding + PathColumnPadding);

        if (!string.IsNullOrEmpty(relativePath))
            formatted += $"[dim]{relativePath}[/]";

        return new Markup(formatted);
    }

    /// <summary>
    /// Get relative path from base directory
    /// </summary>
    private string GetRelativePath(string filePath)
    {
        if (string.IsNullOrEmpty(_baseDirectory) || string.IsNullOrEmpty(filePath))
            return "";

        try
        {
            var fullBasePath = Path.GetFullPath(_baseDirectory);
            var fullFilePath = Path.GetFullPath(filePath);

            // Make sure to normalize path separators
            fullBasePath = fullBasePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (fullFilePath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
            {
                var relative = fullFilePath.Substring(fullBasePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                // Replace backslashes with forward slashes for consistent display
                return ".../" + relative.Replace('\\', '/');
            }

            return "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Get color based on class properties
    /// </summary>
    private string GetClassColor(ClassInfo classInfo)
    {
        if (classInfo.IsAbstract)
            return "yellow";
        if (classInfo.IsSealed)
            return "blue";
        if (classInfo.IsStatic)
            return "cyan";

        return "white";
    }

    /// <summary>
    /// Render a filtered subtree starting from a specific class
    /// </summary>
    public void RenderSubtree(InheritanceNode node)
    {
        var roots = new List<InheritanceNode> { node };
        RenderTree(roots, $"Subtree: {node.ClassInfo.Name}");
    }

    /// <summary>
    /// Render inheritance chain from root to a specific class
    /// </summary>
    public void RenderInheritanceChain(InheritanceNode node)
    {
        var chain = node.GetInheritanceChain();

        AnsiConsole.MarkupLine("\n[bold]Inheritance Chain:[/]");

        for (int i = 0; i < chain.Count; i++)
        {
            var indent = new string(' ', i * 2);
            var arrow = i > 0 ? "↳ " : "";
            var classInfo = chain[i].ClassInfo;
            var color = GetClassColor(classInfo);

            AnsiConsole.MarkupLine($"{indent}{arrow}[{color}]{classInfo.FullName}[/]");
        }

        AnsiConsole.WriteLine();
    }
}