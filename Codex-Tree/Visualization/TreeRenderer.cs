using Codex_Tree.Models;
using Codex_Tree.UI;
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
    private static readonly Regex MarkupRegex = new(@"\[.*?\]", RegexOptions.Compiled);

    private readonly TreeLineBuilder _lineBuilder;
    private readonly FilePreview _filePreview;
    private readonly InputHandler _inputHandler;
    private DetailsPanel _detailsPanel;

    public TreeRenderer()
    {
        _lineBuilder = new TreeLineBuilder();
        _filePreview = new FilePreview();
        _detailsPanel = new DetailsPanel();
        _inputHandler = new InputHandler(_filePreview);
    }

    /// <summary>
    /// Render the entire inheritance forest
    /// </summary>
    public void RenderTree(List<InheritanceNode> roots, string title = "Inheritance Tree", string? baseDirectory = null, Grid? statsGrid = null)
    {
        // Create details panel with the correct base directory
        _detailsPanel = new DetailsPanel(baseDirectory);

        var nodeList = new List<InheritanceNode>();
        var trees = new List<Tree>();

        foreach (var root in roots)
        {
            var tree = _lineBuilder.BuildSpectreTree(root, nodeList);
            trees.Add(tree);
        }

        RenderInteractive(trees, nodeList, title, statsGrid);
    }

    /// <summary>
    /// Render interactive tree with selection
    /// </summary>
    private void RenderInteractive(List<Tree> trees, List<InheritanceNode> nodeList, string title, Grid? statsGrid)
    {
        int selectedIndex = 0;
        int scrollOffset = 0;
        int previewScrollOffset = 0;
        bool showPreview = false;

        // Render trees to get their string representation and calculate lines
        var treeLines = RenderTreesToLines(trees, out int maxTreeWidth);

        while (true)
        {
            AnsiConsole.Clear();

            scrollOffset = CalculateScrollOffset(selectedIndex, scrollOffset, treeLines.Count);
            var treeText = BuildVisibleTreeText(treeLines, selectedIndex, scrollOffset);

            var table = showPreview
                ? CreateTableWithPreview(title, maxTreeWidth, treeText, nodeList[selectedIndex], previewScrollOffset)
                : CreateTableWithDetails(title, maxTreeWidth, treeText, nodeList[selectedIndex]);

            AnsiConsole.Write(table);

            // Render stats below the tree if provided
            if (statsGrid != null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(statsGrid);
            }

            if (!_inputHandler.HandleInput(ref selectedIndex, ref previewScrollOffset, ref showPreview, treeLines.Count, nodeList))
                break;
        }
    }

    /// <summary>
    /// Render Spectre Trees to a list of lines
    /// </summary>
    private static List<string> RenderTreesToLines(List<Tree> trees, out int maxTreeWidth)
    {
        var lines = new List<string>();
        // Minimum width to fit "Inheritance Tree" header without wrapping
        const int minTreeWidth = 17;
        maxTreeWidth = minTreeWidth;

        foreach (var tree in trees)
        {
            // Render tree to string using StringWriter with no ANSI codes
            using var writer = new StringWriter();
            var ansiConsole = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Out = new AnsiConsoleOutput(writer),
                Interactive = InteractionSupport.No
            });

            ansiConsole.Write(tree);
            var treeOutput = writer.ToString();

            // Split into lines and add to list
            var treeLines = treeOutput.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in treeLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    // Unescape markup that was escaped for NoColors rendering
                    var unescapedLine = line.Replace("[[", "[").Replace("]]", "]");
                    lines.Add(unescapedLine);
                    var plainText = MarkupRegex.Replace(unescapedLine, "");
                    maxTreeWidth = Math.Max(maxTreeWidth, plainText.Length);
                }
            }
        }

        return lines;
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

    private Table CreateTableWithDetails(string title, int maxTreeWidth, StringBuilder treeText, InheritanceNode node)
    {
        var table = CreateBaseTable(title, "Up/Down keys to navigate, Enter to toggle preview, Q to quit [green](Details)[/]");
        var detailsGrid = _detailsPanel.BuildDetails(node);

        table.AddColumn(new TableColumn("[green]Inheritance Tree[/]").Width(maxTreeWidth).NoWrap());
        table.AddColumn(new TableColumn("[blue]Details[/]").NoWrap());
        table.AddRow(new Markup(treeText.ToString().TrimEnd()), detailsGrid);

        return table;
    }

    private Table CreateTableWithPreview(string title, int maxTreeWidth, StringBuilder treeText, InheritanceNode node, int previewScrollOffset)
    {
        var table = CreateBaseTable(title, "Up/Down keys to scroll preview, Enter to toggle details, Q to quit [yellow](Preview)[/]");
        var previewText = _filePreview.BuildPreview(node, previewScrollOffset);

        table.AddColumn(new TableColumn("[green]Inheritance Tree[/]").Width(maxTreeWidth).NoWrap());
        table.AddColumn(new TableColumn("[cyan]File Preview[/]").Width(_filePreview.GetColumnWidth()).NoWrap());
        table.AddRow(new Markup(treeText.ToString().TrimEnd()), new Markup(previewText.ToString().TrimEnd()));

        return table;
    }

    private static Table CreateBaseTable(string title, string instructions)
    {
        return new Table
        {
            Border = TableBorder.Rounded,
            BorderStyle = new Style(Color.White),
            Title = new TableTitle($"{title} - {instructions}"),
            Expand = true
        };
    }

    /// <summary>
    /// Render a filtered subtree starting from a specific class
    /// </summary>
    public void RenderSubtree(InheritanceNode node) =>
        RenderTree([node], $"Subtree: {node.ClassInfo.Name}");
}