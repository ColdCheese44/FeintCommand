using System.IO;

namespace FeintCommand.Models;

public sealed class EnvironmentSettings
{
    public string? EnvironmentFilePath { get; init; }
    public string PreferredEnvironmentFilePath { get; init; } = string.Empty;
    public string DiscordServerId { get; init; } = string.Empty;
    public string DiscordServerIdSource { get; init; } = "Not configured";

    public bool HasEnvironmentFile => !string.IsNullOrWhiteSpace(EnvironmentFilePath) && File.Exists(EnvironmentFilePath);
    public bool HasDiscordServerId => !string.IsNullOrWhiteSpace(DiscordServerId);
    public bool HasLikelyDiscordServerId => HasDiscordServerId && DiscordServerId.Length is >= 17 and <= 20 && DiscordServerId.All(char.IsDigit);

    public string EnvironmentFileDisplay => HasEnvironmentFile ? EnvironmentFilePath! : "No .env file found";
    public string DiscordServerIdDisplay => HasDiscordServerId ? DiscordServerId : "Not configured";
    public string DiscordStatusText => HasLikelyDiscordServerId ? "LINKED" : HasDiscordServerId ? "CHECK ID" : "SETUP NEEDED";
    public string DiscordServerUrl => HasDiscordServerId ? $"https://discord.com/channels/{DiscordServerId}" : "https://discord.com/app";
}
