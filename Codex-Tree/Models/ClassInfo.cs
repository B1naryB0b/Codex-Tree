namespace Codex_Tree.Models;

/// <summary>
/// Represents information about a parsed class
/// </summary>
public class ClassInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public string? BaseClass { get; set; }
    public List<string> Interfaces { get; set; } = new();
    public bool IsAbstract { get; set; }
    public bool IsSealed { get; set; }
    public bool IsStatic { get; set; }
    public int MethodCount { get; set; }
    public int LineCount { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ParentClassName { get; set; }
    public bool IsNested => !string.IsNullOrEmpty(ParentClassName);

    /// <summary>
    /// Full qualified name including namespace
    /// </summary>
    public string FullName => string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
}