using Codex_Tree.Models;
using Codex_Tree.Visualization;

namespace Codex_Tree.UI;

/// <summary>
/// Handles keyboard input for tree navigation and preview scrolling
/// </summary>
public class InputHandler
{
    private readonly FilePreview _filePreview;

    public InputHandler(FilePreview filePreview)
    {
        _filePreview = filePreview;
    }

    /// <summary>
    /// Handle keyboard input and update navigation state
    /// Returns false if user wants to exit, true if normal navigation, null if export requested
    /// </summary>
    public bool? HandleInput(ref int selectedIndex, ref int previewScrollOffset, ref bool showPreview, int totalLines, List<InheritanceNode> nodeList, out bool exportRequested)
    {
        exportRequested = false;
        var key = Console.ReadKey(true);

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                HandleUpArrow(ref selectedIndex, ref previewScrollOffset, showPreview);
                break;

            case ConsoleKey.DownArrow:
                HandleDownArrow(ref selectedIndex, ref previewScrollOffset, showPreview, totalLines, nodeList);
                break;

            case ConsoleKey.Enter:
                showPreview = !showPreview;
                previewScrollOffset = 0; // Reset preview scroll when toggling
                break;

            case ConsoleKey.S:
                exportRequested = true;
                break;

            case ConsoleKey.Q:
            case ConsoleKey.Escape:
                return false;
        }

        return true;
    }

    private void HandleUpArrow(ref int selectedIndex, ref int previewScrollOffset, bool showPreview)
    {
        if (showPreview)
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
    }

    private void HandleDownArrow(ref int selectedIndex, ref int previewScrollOffset, bool showPreview, int totalLines, List<InheritanceNode> nodeList)
    {
        if (showPreview)
        {
            // Get max scroll position for current file
            var currentNode = nodeList[selectedIndex];
            var maxScroll = _filePreview.GetMaxScrollOffset(currentNode);
            if (previewScrollOffset < maxScroll)
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
    }
}