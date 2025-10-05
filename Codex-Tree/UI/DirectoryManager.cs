using Spectre.Console;
using System.Text.Json;

namespace Codex_Tree.UI;

/// <summary>
/// Manages saved directories and provides directory selection UI
/// </summary>
public class DirectoryManager
{
    private const string ConfigFileName = ".codex-tree-config.json";
    private readonly string _configPath;

    public DirectoryManager()
    {
        // Save config in the application directory
        var appDirectory = AppContext.BaseDirectory;
        _configPath = Path.Combine(appDirectory, ConfigFileName);
    }

    /// <summary>
    /// Show directory selection menu and return selected directory
    /// </summary>
    public string? SelectDirectory()
    {
        var config = LoadConfig();

        while (true)
        {
            var choices = new List<string>();

            if (!string.IsNullOrEmpty(config.LastViewedDirectory) && Directory.Exists(config.LastViewedDirectory))
            {
                choices.Add($"[green]Use last viewed:[/] {config.LastViewedDirectory}");
            }

            if (config.SavedDirectories.Count > 0)
            {
                choices.Add("[cyan]Select from saved directories[/]");
            }

            choices.Add("[yellow]Add new directory[/]");

            if (config.SavedDirectories.Count > 0)
            {
                choices.Add("[red]Delete directory from list[/]");
            }

            choices.Add("[dim]Exit[/]");

            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Directory Selection:[/]")
                    .PageSize(10)
                    .AddChoices(choices));

            if (selection.Contains("Use last viewed:"))
            {
                return config.LastViewedDirectory;
            }
            else if (selection.Contains("Select from saved"))
            {
                var selected = SelectFromSaved(config);
                if (selected != null)
                {
                    config.LastViewedDirectory = selected;
                    SaveConfig(config);
                    return selected;
                }
            }
            else if (selection.Contains("Add new directory"))
            {
                var newDir = AddNewDirectory(config);
                if (newDir != null)
                {
                    config.LastViewedDirectory = newDir;
                    SaveConfig(config);
                    return newDir;
                }
            }
            else if (selection.Contains("Delete directory"))
            {
                DeleteDirectory(config);
                SaveConfig(config);
            }
            else if (selection.Contains("Exit"))
            {
                return null;
            }
        }
    }

    private string? SelectFromSaved(DirectoryConfig config)
    {
        if (config.SavedDirectories.Count == 0)
            return null;

        var choices = config.SavedDirectories
            .Where(Directory.Exists)
            .Select(d => new { Path = d, Display = d })
            .ToList();

        if (choices.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No valid saved directories found![/]");
            return null;
        }

        choices.Add(new { Path = "", Display = "[dim]Back[/]" });

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold cyan]Saved Directories:[/]")
                .PageSize(15)
                .AddChoices(choices.Select(c => c.Display)));

        if (selection.Contains("Back"))
            return null;

        return choices.First(c => c.Display == selection).Path;
    }

    private string? AddNewDirectory(DirectoryConfig config)
    {
        var directory = AnsiConsole.Ask<string>(
            "[yellow]Enter directory path[/] (or [dim]'cancel'[/] to go back):",
            Directory.GetCurrentDirectory());

        if (directory.Equals("cancel", StringComparison.OrdinalIgnoreCase))
            return null;

        // Expand environment variables and resolve relative paths
        directory = Environment.ExpandEnvironmentVariables(directory);
        directory = Path.GetFullPath(directory);

        if (!Directory.Exists(directory))
        {
            AnsiConsole.MarkupLine("[red]Error: Directory not found![/]");
            Console.ReadKey();
            return null;
        }

        // Add to saved directories if not already present
        if (!config.SavedDirectories.Contains(directory))
        {
            config.SavedDirectories.Add(directory);
            AnsiConsole.MarkupLine($"[green]Added to saved directories:[/] {directory}");
        }

        return directory;
    }

    private void DeleteDirectory(DirectoryConfig config)
    {
        if (config.SavedDirectories.Count == 0)
            return;

        var choices = config.SavedDirectories.Select(d => d).ToList();
        choices.Add("[dim]← Cancel[/]");

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold red]Delete Directory:[/]")
                .PageSize(15)
                .AddChoices(choices));

        if (selection.Contains("Cancel"))
            return;

        config.SavedDirectories.Remove(selection);

        // Clear last viewed if it was deleted
        if (config.LastViewedDirectory == selection)
        {
            config.LastViewedDirectory = null;
        }

        AnsiConsole.MarkupLine($"[green]Removed:[/] {selection}");
        Console.ReadKey();
    }

    private DirectoryConfig LoadConfig()
    {
        if (!File.Exists(_configPath))
            return new DirectoryConfig();

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<DirectoryConfig>(json) ?? new DirectoryConfig();
        }
        catch
        {
            return new DirectoryConfig();
        }
    }

    private void SaveConfig(DirectoryConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Warning: Could not save config: {ex.Message}[/]");
        }
    }
}

/// <summary>
/// Configuration for saved directories
/// </summary>
public class DirectoryConfig
{
    public string? LastViewedDirectory { get; set; }
    public List<string> SavedDirectories { get; set; } = new();
}