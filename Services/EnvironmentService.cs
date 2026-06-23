using System.IO;
using FeintCommand.Models;

namespace FeintCommand.Services;

public sealed class EnvironmentService
{
    public const string DiscordServerIdKey = "FEINTCOMMAND_SERVER_ID";

    public EnvironmentSettings Load()
    {
        string preferredPath = GetPreferredEnvironmentPath();
        string? environmentFilePath = GetCandidateEnvironmentPaths(preferredPath).FirstOrDefault(File.Exists);
        Dictionary<string, string> fileValues = environmentFilePath is null ? [] : ReadEnvironmentFile(environmentFilePath);

        string? processValue = Environment.GetEnvironmentVariable(DiscordServerIdKey);
        string discordServerId = !string.IsNullOrWhiteSpace(processValue)
            ? processValue.Trim()
            : fileValues.TryGetValue(DiscordServerIdKey, out string? fileValue) ? fileValue.Trim() : string.Empty;

        return new EnvironmentSettings
        {
            EnvironmentFilePath = environmentFilePath,
            PreferredEnvironmentFilePath = preferredPath,
            DiscordServerId = discordServerId,
            DiscordServerIdSource = !string.IsNullOrWhiteSpace(processValue)
                ? "Process environment"
                : environmentFilePath is not null && fileValues.ContainsKey(DiscordServerIdKey) ? ".env file" : "Not configured",
        };
    }

    private static string GetPreferredEnvironmentPath()
    {
        DirectoryInfo? projectDirectory = FindProjectDirectory();
        return Path.Combine(projectDirectory?.FullName ?? AppContext.BaseDirectory, ".env");
    }

    private static IEnumerable<string> GetCandidateEnvironmentPaths(string preferredPath)
    {
        List<string> paths = [];
        HashSet<string> seenPaths = new(StringComparer.OrdinalIgnoreCase);
        AddPath(preferredPath);
        AddPath(Path.Combine(Environment.CurrentDirectory, ".env"));
        AddPath(Path.Combine(AppContext.BaseDirectory, ".env"));

        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        for (int depth = 0; directory is not null && depth < 8; depth++)
        {
            AddPath(Path.Combine(directory.FullName, ".env"));

            if (Directory.Exists(Path.Combine(directory.FullName, ".git"))
                || File.Exists(Path.Combine(directory.FullName, "FeintCommand.csproj"))
                || File.Exists(Path.Combine(directory.FullName, "FeintCommand.slnx")))
            {
                break;
            }

            directory = directory.Parent;
        }

        string localConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FeintAI",
            "FeintCommand",
            ".env");
        AddPath(localConfigPath);

        return paths;

        void AddPath(string path)
        {
            if (seenPaths.Add(path))
            {
                paths.Add(path);
            }
        }
    }

    private static DirectoryInfo? FindProjectDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        for (int depth = 0; directory is not null && depth < 8; depth++)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git"))
                || File.Exists(Path.Combine(directory.FullName, "FeintCommand.csproj"))
                || File.Exists(Path.Combine(directory.FullName, "FeintCommand.slnx")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static Dictionary<string, string> ReadEnvironmentFile(string path)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                line = line[7..].TrimStart();
            }

            int separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            string key = line[..separatorIndex].Trim();
            string value = line[(separatorIndex + 1)..].Trim();
            if (key.Length == 0)
            {
                continue;
            }

            values[key] = Unquote(value);
        }

        return values;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            value = value[1..^1];
        }

        return value.Replace("\\n", "\n", StringComparison.Ordinal);
    }
}
