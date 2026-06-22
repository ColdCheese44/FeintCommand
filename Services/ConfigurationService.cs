using System.Text.Json;
using System.IO;
using FeintCommand.Models;

namespace FeintCommand.Services;

public sealed class ConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public ConfigurationService()
    {
        ConfigurationDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FeintAI",
            "FeintCommand");
        ConfigurationPath = Path.Combine(ConfigurationDirectory, "launcher.json");
    }

    public string ConfigurationDirectory { get; }
    public string ConfigurationPath { get; }

    public LauncherConfiguration Load()
    {
        Directory.CreateDirectory(ConfigurationDirectory);

        if (!File.Exists(ConfigurationPath))
        {
            LauncherConfiguration defaults = CreateDefaults();
            Save(defaults);
            return defaults;
        }

        try
        {
            string json = File.ReadAllText(ConfigurationPath);
            LauncherConfiguration configuration = JsonSerializer.Deserialize<LauncherConfiguration>(json, JsonOptions)
                ?? CreateDefaults();
            configuration.Programs ??= [];
            return configuration;
        }
        catch (JsonException)
        {
            string backupPath = ConfigurationPath + $".invalid-{DateTime.Now:yyyyMMdd-HHmmss}";
            File.Copy(ConfigurationPath, backupPath, true);
            LauncherConfiguration defaults = CreateDefaults();
            Save(defaults);
            return defaults;
        }
    }

    public void Save(LauncherConfiguration configuration)
    {
        Directory.CreateDirectory(ConfigurationDirectory);
        string tempPath = ConfigurationPath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(configuration, JsonOptions));
        File.Move(tempPath, ConfigurationPath, true);
    }

    public LauncherConfiguration CreateDefaults() => new()
    {
        Programs =
        [
            CreateProgram("FeintTrade", "Algorithmic trading workspace and market operations.", "#42D77D", @"D:\Windows\FeintTrade\FeintTrade.exe"),
            CreateProgram("FeintSupplyCo", "Supply intelligence, inventory, and operational planning.", "#F4B942", @"D:\Windows\FeintSupplyCo\FeintSupplyCo.exe"),
            CreateProgram("FeintSignal", "Local-first current-affairs intelligence and analysis.", "#4FA9FF", @"D:\Windows\FeintSignal\FeintSignal.exe"),
        ],
    };

    private static FeintProgram CreateProgram(string name, string description, string accent, string command) => new()
    {
        Name = name,
        Description = description,
        AccentColor = accent,
        Targets =
        [
            new LaunchTarget
            {
                Name = $"Launch {name}",
                Command = command,
                WorkingDirectory = Path.GetDirectoryName(command) ?? string.Empty,
            },
        ],
    };
}
