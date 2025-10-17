# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Codex-Tree is a C# console application that analyzes C# codebases to visualize class inheritance hierarchies. It parses C# files using regex patterns, builds inheritance trees, and displays them in an interactive terminal UI using Spectre.Console.

## Build and Run Commands

```bash
# Build the solution
dotnet build Codex-Tree.sln

# Build in Release mode
dotnet build Codex-Tree.sln -c Release

# Run the application
dotnet run --project Codex-Tree/Codex-Tree.csproj

# Clean build artifacts
dotnet clean
```

## Architecture

The application follows a layered architecture with distinct phases:

### 1. Parsing Phase (Parsers/)
- **CSharpParser**: Uses compiled regex patterns to extract class information from .cs files
  - Extracts: class names, namespaces, inheritance, modifiers (abstract/sealed/static)
  - Detects nested classes by tracking brace depth
  - Counts methods and lines within class boundaries
  - Filters out bin/obj directories and comments

### 2. Analysis Phase (Analysis/)
- **InheritanceTreeBuilder**: Constructs a tree structure from parsed classes
  - Creates InheritanceNode dictionary keyed by FullName
  - Resolves parent-child relationships (handles with/without namespace)
  - Separates nested classes from inherited classes
  - Calculates depth for each node, roots start at depth 0

- **TreeStatistics**: Calculates metrics across the tree
  - Recursively traverses nodes to count classes, abstract classes, max depth
  - Identifies deep inheritance patterns (3+ levels)
  - Finds largest class by line count
  - Tracks deepest inheritance chains (excluding nested classes)

### 3. Presentation Phase (Visualization/ and UI/)
- **TreeRenderer**: Interactive tree display with keyboard navigation
  - Uses Spectre.Console Tree widget rendered to lines with `NoColors` mode
  - Markup escaping: Tree labels use `[[markup]]` which gets unescaped to `[markup]` after rendering
  - Viewport scrolling (16 lines visible)
  - Real-time details panel showing: namespace (dimmed), name with badges, depth, base class, interfaces, subtree counts, methods, lines, path
  - Statistics panel rendered below tree during navigation
  - Color coding: yellow=abstract, blue=sealed, cyan=static, white=normal
  - Modifier badges: Yellow "A" for abstract, blue "S" for sealed, cyan "ST" for static

- **TreeLineBuilder**: Builds Spectre.Console Tree structures
  - Escapes all markup with double brackets `[[` and `]]` so it survives NoColors rendering
  - Supports selection styling by passing selectedNode parameter
  - Separates nested classes (stored in NestedClasses) from inherited children

- **DetailsPanel**: Right-side panel with class information
  - Shows modifier badges (ABSTRACT, SEALED, STATIC) next to class name
  - Displays depth in hierarchy, base class, interfaces list
  - Enhanced subtree info: "X total (Y children, Z nested)" with recursive count
  - Dynamic separator bar using Spectre.Console Rule widget
  - File path with relative display and file size

- **DirectoryManager**: Persistent directory selection system
  - Stores last viewed and saved directories in JSON config
  - Config location: %AppData%/CodexTree/.codex-tree-config.json
  - Menu allows: use last viewed, select from saved, add new, delete (red), exit

### 4. Data Models (Models/)
- **ClassInfo**: All extracted class metadata (name, namespace, base, interfaces, modifiers, counts, file path)
- **InheritanceNode**: Tree structure with Parent, Children, NestedClasses, Depth
- **TreeStats**: Container for calculated statistics

## Application Flow

```
Program.cs orchestrates:
1. Display banner (FigletText)
2. DirectoryManager.SelectDirectory() → user chooses directory
3. Status block with spinner:
   - CSharpParser.ParseDirectory() → List<ClassInfo>
   - InheritanceTreeBuilder.BuildTree() → List<InheritanceNode>
4. TreeStatistics.CalculateStatistics() → TreeStats
5. TreeStatistics.BuildStatisticsGrid() → Grid
6. TreeRenderer.RenderTree() (interactive, outside status block)
   - Displays tree with stats below
   - User navigates with ↑/↓ arrows, exits with Q
```

## Key Technical Patterns

- **Regex-based parsing**: No Roslyn dependency, lightweight but less robust for complex syntax
- **Tree traversal**: Recursive depth-first for building tree lines and calculating statistics
- **Nullable reference types**: Enabled throughout, use `?` and null checks appropriately
- **Spectre.Console**: All UI rendering (tables, grids, panels, markup, interactive prompts)
- **Configuration persistence**: JSON serialization for user preferences

## Important Implementation Notes

### Rendering Architecture
- **Markup preservation**: To preserve Spectre.Console markup through NoColors rendering:
  1. Escape all markup in TreeLineBuilder with `[[` and `]]`
  2. Render tree with `ColorSystem = ColorSystemSupport.NoColors`
  3. Unescape in TreeRenderer: replace `[[` → `[` and `]]` → `]`
  4. Pass unescaped markup strings to `new Markup()` for final rendering
- **Why this approach**: Spectre.Console Tree widget doesn't support interactive selection, so we render to text lines for manual viewport/selection handling. NoColors mode preserves escaped markup as literal text.

### Tree Structure
- **Nested vs Inherited classes**: Nested classes are stored in NestedClasses, not Children. They don't contribute to inheritance chain depth calculations
- **Node list synchronization**: When building tree lines, maintain parallel List<InheritanceNode> that maps line indices to nodes for details panel
- **Selection styling**: Applied at tree build time by passing selectedNode parameter (unused in current line-based rendering)

### UI Layout
- **Statistics rendering**: Stats must render inside TreeRenderer's interactive loop (passed as Grid parameter), not after RenderTree() returns (which blocks until user exits)
- **Viewport management**: Track scrollOffset to keep selectedIndex visible within ViewportHeight (16 lines)
- **Path resolution**: Display relative paths from base directory with ".../" prefix for cleaner UI

### Development Workflow
- There is no need to run build commands - user runs tests manually
- Theres no need for you to run build commands, I'll have to run them when testing anyway