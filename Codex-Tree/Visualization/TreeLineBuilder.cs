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
    public Tree BuildSpectreTree(InheritanceNode node, List<InheritanceNode> nodeList, InheritanceNode? selectedNode = null)
    {
        var isSelected = node == selectedNode;
        var rootLabel = BuildNodeLabel(node, false, isSelected);
        var tree = new Tree(rootLabel);
        nodeList.Add(node);

        AddChildrenToTree(tree, node, nodeList, selectedNode);

        return tree;
    }

    /// <summary>
    /// Add children recursively to a tree node
    /// </summary>
    private void AddChildrenToTree(IHasTreeNodes parentTree, InheritanceNode parentNode, List<InheritanceNode> nodeList, InheritanceNode? selectedNode = null)
    {
        var allChildren = CombineChildren(parentNode);

        foreach (var (child, isNested) in allChildren)
        {
            var isSelected = child == selectedNode;
            var childLabel = BuildNodeLabel(child, isNested, isSelected);
            var childTreeNode = parentTree.AddNode(childLabel);
            nodeList.Add(child);

            // Add children recursively
            AddChildrenToTree(childTreeNode, child, nodeList, selectedNode);
        }
    }

    /// <summary>
    /// Build the label markup for a node
    /// </summary>
    private static string BuildNodeLabel(InheritanceNode node, bool isNested = false, bool isSelected = false)
    {
        var color = GetClassColor(node.ClassInfo);
        var badges = BuildModifierBadges(node.ClassInfo);
        var nestedIndicator = FormatNestedIndicator(isNested);

        // Build label with optional color markup
        string label;
        if (!string.IsNullOrEmpty(color))
        {
            label = $"[[{color}]]{node.ClassInfo.Name}[[/]]{badges}{nestedIndicator}";
        }
        else
        {
            // No color markup - use default text color
            label = $"{node.ClassInfo.Name}{badges}{nestedIndicator}";
        }

        // Wrap in selection styling if selected
        if (isSelected)
        {
            label = $"[[black on white]]{node.ClassInfo.Name}{nestedIndicator}[[/]]";
        }

        return label;
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
        return ""; // No color markup for normal classes - use default text color
    }

    /// <summary>
    /// Build modifier badges for class type
    /// </summary>
    private static string BuildModifierBadges(ClassInfo classInfo)
    {
        var badges = new List<string>();

        if (classInfo.IsAbstract)
            badges.Add("[[black on yellow]] A [[/]]");
        if (classInfo.IsSealed)
            badges.Add("[[black on blue]] S [[/]]");
        if (classInfo.IsStatic)
            badges.Add("[[black on cyan]] ST [[/]]");

        return badges.Count > 0 ? " " + string.Join(" ", badges) : "";
    }

    /// <summary>
    /// Format modifier text for abstract classes (kept for backwards compatibility)
    /// </summary>
    public static string FormatModifier(bool isAbstract) =>
        isAbstract ? " (abstract)" : "";

    /// <summary>
    /// Format nested indicator text
    /// </summary>
    public static string FormatNestedIndicator(bool isNested) =>
        isNested ? "[[dim]] (nested)[[/]]" : "";
}