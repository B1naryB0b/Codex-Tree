using Codex_Tree.Models;

namespace Codex_Tree.Analysis;

/// <summary>
/// Builds an inheritance tree from a list of parsed classes
/// </summary>
public class InheritanceTreeBuilder
{
    /// <summary>
    /// Build inheritance tree from class list
    /// </summary>
    public List<InheritanceNode> BuildTree(List<ClassInfo> classes)
    {
        var nodeMap = new Dictionary<string, InheritanceNode>();

        // Create nodes for all classes
        foreach (var classInfo in classes)
        {
            nodeMap[classInfo.FullName] = new InheritanceNode
            {
                ClassInfo = classInfo
            };
        }

        // Build parent-child relationships and nested class relationships
        var roots = new List<InheritanceNode>();

        foreach (var node in nodeMap.Values)
        {
            // Handle nested classes first
            if (node.ClassInfo.IsNested)
            {
                var parentClassName = node.ClassInfo.ParentClassName;
                var parentNode = nodeMap.Values.FirstOrDefault(n => n.ClassInfo.Name == parentClassName);

                if (parentNode != null)
                {
                    parentNode.NestedClasses.Add(node);
                    continue; // Don't process as inheritance relationship
                }
            }

            var baseClass = node.ClassInfo.BaseClass;

            if (string.IsNullOrEmpty(baseClass))
            {
                // No base class - this is a root
                node.Depth = 0;
                roots.Add(node);
            }
            else
            {
                // Try to find parent in the map
                // Check with and without namespace
                InheritanceNode? parent = null;

                if (nodeMap.TryGetValue(baseClass, out parent))
                {
                    // Found by exact name
                }
                else
                {
                    // Try with namespace
                    var withNamespace = $"{node.ClassInfo.Namespace}.{baseClass}";
                    nodeMap.TryGetValue(withNamespace, out parent);
                }

                if (parent == null)
                {
                    // Try to find by simple name match
                    parent = nodeMap.Values.FirstOrDefault(n =>
                        n.ClassInfo.Name == baseClass);
                }

                if (parent != null)
                {
                    node.Parent = parent;
                    node.Depth = parent.Depth + 1;
                    parent.Children.Add(node);
                }
                else
                {
                    // Parent not found in our class list - treat as root
                    node.Depth = 0;
                    roots.Add(node);
                }
            }
        }

        // Sort roots, children, and nested classes alphabetically
        roots = roots.OrderBy(r => r.ClassInfo.Name).ToList();
        foreach (var node in nodeMap.Values)
        {
            node.Children = node.Children.OrderBy(c => c.ClassInfo.Name).ToList();
            node.NestedClasses = node.NestedClasses.OrderBy(n => n.ClassInfo.Name).ToList();
        }

        return roots;
    }

    /// <summary>
    /// Find a specific node by class name
    /// </summary>
    public InheritanceNode? FindNode(List<InheritanceNode> roots, string className)
    {
        foreach (var root in roots)
        {
            if (root.ClassInfo.Name == className || root.ClassInfo.FullName == className)
                return root;

            var found = FindNodeRecursive(root, className);
            if (found != null)
                return found;
        }

        return null;
    }

    private InheritanceNode? FindNodeRecursive(InheritanceNode node, string className)
    {
        foreach (var child in node.Children)
        {
            if (child.ClassInfo.Name == className || child.ClassInfo.FullName == className)
                return child;

            var found = FindNodeRecursive(child, className);
            if (found != null)
                return found;
        }

        return null;
    }
}