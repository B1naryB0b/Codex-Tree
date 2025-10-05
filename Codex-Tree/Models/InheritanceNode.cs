namespace Codex_Tree.Models;

/// <summary>
/// Represents a node in the inheritance tree
/// </summary>
public class InheritanceNode
{
    public ClassInfo ClassInfo { get; set; } = null!;
    public InheritanceNode? Parent { get; set; }
    public List<InheritanceNode> Children { get; set; } = new();
    public List<InheritanceNode> NestedClasses { get; set; } = new();

    /// <summary>
    /// Depth in the inheritance hierarchy (0 for root)
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Calculate maximum depth from this node downward
    /// </summary>
    public int MaxDepthBelow()
    {
        if (Children.Count == 0)
            return 0;

        return 1 + Children.Max(c => c.MaxDepthBelow());
    }

    /// <summary>
    /// Get all descendants recursively
    /// </summary>
    public IEnumerable<InheritanceNode> GetAllDescendants()
    {
        foreach (InheritanceNode child in Children)
        {
            yield return child;
            foreach (var descendant in child.GetAllDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Get the full inheritance chain from root to this node
    /// </summary>
    public List<InheritanceNode> GetInheritanceChain()
    {
        var chain = new List<InheritanceNode>();
        var current = this;

        while (current != null)
        {
            chain.Insert(0, current);
            current = current.Parent;
        }

        return chain;
    }
}