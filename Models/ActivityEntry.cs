namespace FeintCommand.Models;

public sealed record ActivityEntry(DateTimeOffset Timestamp, string Message, bool IsError = false)
{
    public string TimeText => Timestamp.LocalDateTime.ToString("h:mm tt");
}
