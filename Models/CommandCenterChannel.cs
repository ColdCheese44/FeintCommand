namespace FeintCommand.Models;

public sealed class CommandCenterChannel
{
    public string Category { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;

    public string DisplayName => $"#{Name}";
}
