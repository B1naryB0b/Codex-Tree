# Codex Tree

A cross-language class inheritance visualizer for the terminal. Point it at a codebase, navigate the hierarchy interactively, inspect class details, preview source files, and export the tree as a PNG.

> Please note LLMs were used in the development of this tool as it was mainly built for helping me dig around the inheritance heirarchy in unreal engine's source code

<table>
  <tr>
    <td align="center"><b>Select a directory</b><br><img src="assets/Screenshot%202026-02-27%20012705.png" width="540"></td>
    <td align="center"><b>Analyse a language</b><br><img src="assets/Screenshot%202026-02-27%20012813.png" width="540"></td>
  </tr>
  <tr>
    <td align="center"><b>Visualise inheritance hierarchy</b><br><img src="assets/Screenshot%202026-02-27%20013218.png" width="540"></td>
    <td align="center"><b>Inspect individual classes</b><br><img src="assets/Screenshot%202026-02-27%20013258.png" width="540"></td>
  </tr>
</table>

---

## Features

- **Multi-language parsing** — C#, C++, and Python, with an extensible factory-based architecture for adding more
- **Interactive tree navigation** — scrollable viewport with keyboard controls and real-time class details
- **Dual view mode** — switch between a details panel and a syntax-highlighted source preview
- **Statistics panel** — class counts, modifier breakdown chart, max depth, largest class, deep-inheritance warnings
- **PNG export** — render the full tree to an image with configurable themes, font sizes, and dimensions
- **Persistent directory management** — save and quickly recall frequently used project paths

---

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

---

## Getting Started

```bash
git clone https://github.com/cuney/Codex-Tree.git
cd Codex-Tree
dotnet run --project Codex-Tree/Codex-Tree.csproj
```

On launch, select or add a directory and choose the language to analyse. The app parses all matching files recursively, builds the inheritance tree, and enters interactive mode.

---

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `↑` / `↓` | Navigate tree nodes (details mode) or scroll file preview |
| `Enter` | Toggle between details panel and source preview |
| `S` | Export current tree to PNG |
| `Q` / `Esc` | Quit |

---

## Supported Languages

| Language | Extensions |
|----------|-----------|
| C# | `.cs` |
| C++ | `.cpp` `.h` `.hpp` `.cc` `.cxx` |
| Python | `.py` |

---

## PNG Export

Press `S` during navigation to open the export wizard. Options include:

- Custom title
- Image width (400–4000 px)
- Font size (8–32 pt)
- Transparent background
- Color scheme: **Light**, **Dark** (Nord-based), or **High Contrast**

---

## Project Structure

```
Codex-Tree/
├── Analysis/          # Tree builder and statistics
├── Models/            # ClassInfo and InheritanceNode
├── Parsers/           # ILanguageParser, BaseParser, language implementations
├── Syntax/            # ISyntaxHighlighter and language implementations
├── UI/                # DirectoryManager, LanguageManager, InputHandler
└── Visualization/     # TreeRenderer, DetailsPanel, FilePreview, ImageExporter
```

### Architecture Overview

```
Program.cs
├── DirectoryManager   — directory selection with JSON persistence
├── LanguageManager    — file-type analysis and language prompt
├── ParserFactory      — selects the appropriate ILanguageParser
│   ├── CSharpParser   — regex + brace-depth tracking
│   ├── CppParser      — regex + brace-depth tracking
│   └── PythonParser   — regex + indentation tracking
├── InheritanceTreeBuilder — builds InheritanceNode tree from ClassInfo list
├── TreeStatistics     — metrics calculation and Spectre.Console grid
└── TreeRenderer       — interactive UI loop
    ├── TreeLineBuilder    — constructs Spectre.Console Tree with markup escaping
    ├── DetailsPanel       — class metadata panel
    ├── FilePreview        — syntax-highlighted source viewport
    │   └── SyntaxHighlighterFactory
    ├── InputHandler       — keyboard input routing
    └── ImageExporter      — SkiaSharp PNG rendering
        └── ExportConfigBuilder — interactive export configuration
```

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| [Spectre.Console](https://spectreconsole.net) | 0.51.1 | Terminal UI — markup, tables, prompts, charts |
| [SkiaSharp](https://github.com/mono/SkiaSharp) | 2.88.8 | 2D graphics for PNG export |
