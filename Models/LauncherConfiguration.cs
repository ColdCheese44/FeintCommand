namespace FeintCommand.Models;

public sealed class LauncherConfiguration
{
    public int SchemaVersion { get; set; } = 1;
    public string Theme { get; set; } = "System";
    public List<FeintProgram> Programs { get; set; } = [];
}
