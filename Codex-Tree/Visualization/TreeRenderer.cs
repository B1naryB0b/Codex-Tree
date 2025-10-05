using Codex_Tree.Models;
using Spectre.Console;
using System.Text;
using System.Text.RegularExpressions;

namespace Codex_Tree.Visualization;

/// <summary>
/// Renders inheritance trees using Spectre.Console
/// </summary>
public class TreeRenderer
{
    private const int ViewportHeight = 16;
    private const int PreviewViewportHeight = 16;
    private const int PreviewMaxWidth = 25;
    private static readonly Regex MarkupRegex = new(@"\[.*?\]", RegexOptions.Compiled);

    private string? _baseDirectory;

    /// <summary>
    /// Render the entire inheritance forest
    /// </summary>
    public void RenderTree(List<InheritanceNode> roots, string title = "Inheritance Tree", string? baseDirectory = null, Grid? statsGrid = null)
    {
        _baseDirectory = baseDirectory;

        var treeLines = new List<string>();
        var nodeList = new List<InheritanceNode>();

        foreach (var root in roots)
        {
            BuildTreeLines(root, "", true, treeLines, nodeList);
        }

        RenderInteractive(treeLines, nodeList, title, statsGrid);
    }

    /// <summary>
    /// Render interactive tree with selection
    /// </summary>
    private void RenderInteractive(List<string> treeLines, List<InheritanceNode> nodeList, string title, Grid? statsGrid)
    {
        int selectedIndex = 0;
        int scrollOffset = 0;
        int previewScrollOffset = 0;
        bool previewMode = false;
        int maxTreeWidth = CalculateMaxTreeWidth(treeLines);

        while (true)
        {
            AnsiConsole.Clear();

            scrollOffset = CalculateScrollOffset(selectedIndex, scrollOffset, treeLines.Count);
            var treeText = BuildVisibleTreeText(treeLines, selectedIndex, scrollOffset);
            var detailsGrid = BuildDetailsGrid(nodeList[selectedIndex]);
            var previewText = BuildPreviewText(nodeList[selectedIndex], previewScrollOffset);

            var table = CreateTableWithPreview(title, maxTreeWidth, treeText, detailsGrid, previewText, previewMode);
            table.Border(TableBorder.Rounded);
            table.LeftAligned();

            AnsiConsole.Write(table);

            // Render stats below the tree if provided
            if (statsGrid != null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(statsGrid);
            }

            if (!HandleInput(ref selectedIndex, ref previewScrollOffset, ref previewMode, treeLines.Count, nodeList))
                break;
        }
    }

    private static int CalculateMaxTreeWidth(List<string> treeLines)
    {
        int maxWidth = 0;
        foreach (var line in treeLines)
        {
            var plainText = MarkupRegex.Replace(line, "");
            maxWidth = Math.Max(maxWidth, plainText.Length);
        }
        return maxWidth;
    }

    private static int CalculateScrollOffset(int selectedIndex, int scrollOffset, int totalLines)
    {
        if (selectedIndex < scrollOffset)
            return selectedIndex;

        if (selectedIndex >= scrollOffset + ViewportHeight)
            return selectedIndex - ViewportHeight + 1;

        return scrollOffset;
    }

    private static StringBuilder BuildVisibleTreeText(List<string> treeLines, int selectedIndex, int scrollOffset)
    {
        var treeText = new StringBuilder();
        int endIndex = Math.Min(scrollOffset + ViewportHeight, treeLines.Count);

        for (int i = scrollOffset; i < endIndex; i++)
        {
            if (i == selectedIndex)
            {
                var plainText = MarkupRegex.Replace(treeLines[i], "");
                treeText.AppendLine($"[black on white]{plainText}[/]");
            }
            else
            {
                treeText.AppendLine(treeLines[i]);
            }
        }

        return treeText;
    }

    private static Table CreateTableWithPreview(string title, int maxTreeWidth, StringBuilder treeText, Grid detailsGrid, StringBuilder previewText, bool previewMode)
    {
        var modeIndicator = previewMode ? "[yellow](Preview Mode)[/]" : "[green](Tree Mode)[/]";
        var table = new Table
        {
            Border = TableBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Title = new TableTitle($"{title} - ↑/↓ arrows, Enter to toggle mode, Q to quit {modeIndicator}"),
            Expand = true
        };

        table.AddColumn(new TableColumn("[green]Inheritance Tree[/]").Width(maxTreeWidth).NoWrap());
        table.AddColumn(new TableColumn("[blue]Details[/]").NoWrap());
        table.AddColumn(new TableColumn("[cyan]File Preview[/]").Width(PreviewMaxWidth + 7).NoWrap()); // +7 for line number and separator
        table.AddRow(new Markup(treeText.ToString().TrimEnd()), detailsGrid, new Markup(previewText.ToString().TrimEnd()));

        return table;
    }

    private static bool HandleInput(ref int selectedIndex, ref int previewScrollOffset, ref bool previewMode, int totalLines, List<InheritanceNode> nodeList)
    {
        var key = Console.ReadKey(true);

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (previewMode)
                {
                    if (previewScrollOffset > 0)
                        previewScrollOffset--;
                }
                else
                {
                    if (selectedIndex > 0)
                    {
                        selectedIndex--;
                        previewScrollOffset = 0; // Reset preview scroll when changing selection
                    }
                }
                break;
            case ConsoleKey.DownArrow:
                if (previewMode)
                {
                    previewScrollOffset++;
                }
                else
                {
                    if (selectedIndex < totalLines - 1)
                    {
                        selectedIndex++;
                        previewScrollOffset = 0; // Reset preview scroll when changing selection
                    }
                }
                break;
            case ConsoleKey.Enter:
                previewMode = !previewMode;
                break;
            case ConsoleKey.Q:
            case ConsoleKey.Escape:
                return false;
        }

        return true;
    }

    /// <summary>
    /// Build details grid for selected class
    /// </summary>
    private Grid BuildDetailsGrid(InheritanceNode node)
    {
        var grid = new Grid();
        grid.AddColumn();

        var classInfo = node.ClassInfo;
        var color = GetClassColor(classInfo);
        var modifier = classInfo.IsAbstract ? " (abstract)" : "";

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
        grid.AddRow("─".PadRight(50, '─')); // Separator line
        var relativePath = GetRelativePath(classInfo.FilePath);
        var displayPath = !string.IsNullOrEmpty(relativePath) ? relativePath : classInfo.FilePath;
        if (!string.IsNullOrEmpty(displayPath))
            grid.AddRow($"[dim]{displayPath}[/]");

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
            var color = GetClassColor(child.ClassInfo);
            var modifier = child.ClassInfo.IsAbstract ? " (abstract)" : "";
            var nestedIndicator = isNested ? "[dim] (nested)[/]" : "";

            grid.AddRow($"{indent}├─ [{color}]{child.ClassInfo.Name}[/]{modifier}{nestedIndicator}");

            // Recursively add children up to a reasonable depth
            if ((child.Children.Count > 0 || child.NestedClasses.Count > 0) && indent.Length < 12)
            {
                AddSubtree(grid, child, indent + "│  ");
            }
        }
    }

    private static void AddGridRowIfNotEmpty(Grid grid, string label, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            grid.AddRow($"[dim]{label}[/] {value}");
    }


    /// <summary>
    /// Build tree lines with proper indentation and connectors
    /// </summary>
    private void BuildTreeLines(InheritanceNode node, string indent, bool isLast, List<string> lines, List<InheritanceNode> nodeList)
    {
        AddNodeToTree(node, indent, isLast, lines, nodeList, isNested: false);

        string childIndent = indent + (isLast ? "    " : "│   ");
        var allChildren = CombineChildren(node);

        for (int i = 0; i < allChildren.Count; i++)
        {
            bool isLastChild = i == allChildren.Count - 1;
            var (child, isNested) = allChildren[i];

            if (isNested)
            {
                AddNodeToTree(child, childIndent, isLastChild, lines, nodeList, isNested: true);
                BuildNestedChildren(child, childIndent, isLastChild, lines, nodeList);
            }
            else
            {
                BuildTreeLines(child, childIndent, isLastChild, lines, nodeList);
            }
        }
    }

    private void AddNodeToTree(InheritanceNode node, string indent, bool isLast, List<string> lines, List<InheritanceNode> nodeList, bool isNested)
    {
        var color = GetClassColor(node.ClassInfo);
        var modifier = node.ClassInfo.IsAbstract ? " (abstract)" : "";
        var connector = isLast ? "└── " : "├── ";
        var nestedIndicator = isNested ? "[dim] (nested) [/]" : "";

        var text = $"{indent}{connector}[{color}]{node.ClassInfo.Name}[/]{modifier}{nestedIndicator}";

        lines.Add(text);
        nodeList.Add(node);
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

    private void BuildNestedChildren(InheritanceNode nested, string parentIndent, bool isLast, List<string> lines, List<InheritanceNode> nodeList)
    {
        string nestedChildIndent = parentIndent + (isLast ? "    " : "│   ");
        var nestedChildren = CombineChildren(nested);

        for (int j = 0; j < nestedChildren.Count; j++)
        {
            BuildTreeLines(nestedChildren[j].node, nestedChildIndent, j == nestedChildren.Count - 1, lines, nodeList);
        }
    }


    /// <summary>
    /// Get relative path from base directory
    /// </summary>
    private string GetRelativePath(string? filePath)
    {
        if (string.IsNullOrEmpty(_baseDirectory) || string.IsNullOrEmpty(filePath))
            return "";

        try
        {
            var fullBasePath = Path.GetFullPath(_baseDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullFilePath = Path.GetFullPath(filePath);

            if (fullFilePath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
            {
                var relative = fullFilePath[fullBasePath.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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
    private static string GetClassColor(ClassInfo classInfo)
    {
        if (classInfo.IsAbstract) return "yellow";
        if (classInfo.IsSealed) return "blue";
        if (classInfo.IsStatic) return "cyan";
        return "white";
    }

    /// <summary>
    /// Render a filtered subtree starting from a specific class
    /// </summary>
    public void RenderSubtree(InheritanceNode node) =>
        RenderTree([node], $"Subtree: {node.ClassInfo.Name}");

    /// <summary>
    /// Build file preview text with scrolling support
    /// </summary>
    private StringBuilder BuildPreviewText(InheritanceNode node, int scrollOffset)
    {
        var preview = new StringBuilder();
        var filePath = node.ClassInfo.FilePath;

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            preview.AppendLine("[dim]File not found[/]");
            return preview;
        }

        try
        {
            var lines = File.ReadAllLines(filePath);
            int startLine = Math.Max(0, Math.Min(scrollOffset, Math.Max(0, lines.Length - PreviewViewportHeight)));
            int endLine = Math.Min(startLine + PreviewViewportHeight, lines.Length);

            for (int i = startLine; i < endLine; i++)
            {
                var lineNumber = $"[dim]{(i + 1),4}[/]";
                var content = lines[i].Replace("[", "[[").Replace("]", "]]"); // Escape markup

                // Truncate line if it exceeds max width
                if (content.Length > PreviewMaxWidth)
                {
                    content = content.Substring(0, PreviewMaxWidth - 3) + "...";
                }

                preview.AppendLine($"{lineNumber} │ {content}");
            }

            // Show scroll indicator if there's more content below
            if (endLine < lines.Length)
            {
                preview.AppendLine($"[dim]... ({lines.Length - endLine} more lines below)[/]");
            }
        }
        catch (Exception ex)
        {
            preview.AppendLine($"[red]Error reading file: {ex.Message}[/]");
        }

        return preview;
    }
}