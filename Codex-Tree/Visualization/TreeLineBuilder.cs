using Codex_Tree.Models;
using Spectre.Console;

namespace Codex_Tree.Visualization;

/// <summary>
/// Builds Spectre.Console Tree from inheritance node hierarchy
/// </summary>
public class TreeLineBuilder
{
    /// <summary>
    /// Build a Spectre.Console Tree and track nodes for navigation
    /// </summary>
    public Tree BuildSpectreTree(InheritanceNode node, List<InheritanceNode> nodeList)
    {
        var rootLabel = BuildNodeLabel(node);
        var tree = new Tree(rootLabel);
        nodeList.Add(node);

        AddChildrenToTree(tree, node, nodeList);

        return tree;
    }

    /// <summary>
    /// Add children recursively to a tree node
    /// </summary>
    private void AddChildrenToTree(IHasTreeNodes parentTree, InheritanceNode parentNode, List<InheritanceNode> nodeList)
    {
        var allChildren = CombineChildren(parentNode);

        foreach (var (child, isNested) in allChildren)
        {
            var childLabel = BuildNodeLabel(child, isNested);
            var childTreeNode = parentTree.AddNode(childLabel);
            nodeList.Add(child);

            if (isNested)
            {
                // Add nested children recursively
                AddChildrenToTree(childTreeNode, child, nodeList);
            }
            else
            {
                // Add inherited children recursively
                AddChildrenToTree(childTreeNode, child, nodeList);
            }
        }
    }

    /// <summary>
    /// Build the label markup for a node
    /// </summary>
    private static string BuildNodeLabel(InheritanceNode node, bool isNested = false)
    {
        var color = GetClassColor(node.ClassInfo);
        var modifier = FormatModifier(node.ClassInfo.IsAbstract);
        var nestedIndicator = FormatNestedIndicator(isNested);

        return $"[{color}]{node.ClassInfo.Name}[/]{modifier}{nestedIndicator}";
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
    /// Get color based on class properties
    /// </summary>
    public static string GetClassColor(ClassInfo classInfo)
    {
        if (classInfo.IsAbstract) return "yellow";
        if (classInfo.IsSealed) return "blue";
        if (classInfo.IsStatic) return "cyan";
        return "white";
    }

    /// <summary>
    /// Format modifier text for abstract classes
    /// </summary>
    public static string FormatModifier(bool isAbstract) =>
        isAbstract ? " (abstract)" : "";

    /// <summary>
    /// Format nested indicator text
    /// </summary>
    public static string FormatNestedIndicator(bool isNested) =>
        isNested ? "[dim] (nested)[/]" : "";
}