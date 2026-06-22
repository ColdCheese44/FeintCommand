using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FeintCommand.Models;

public sealed class LaunchTarget : INotifyPropertyChanged
{
    private string _name = "Launch";
    private string _command = string.Empty;
    private string _arguments = string.Empty;
    private string _workingDirectory = string.Empty;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string Command
    {
        get => _command;
        set => SetField(ref _command, value);
    }

    public string Arguments
    {
        get => _arguments;
        set => SetField(ref _arguments, value);
    }

    public string WorkingDirectory
    {
        get => _workingDirectory;
        set => SetField(ref _workingDirectory, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public LaunchTarget Clone() => new()
    {
        Id = Id,
        Name = Name,
        Command = Command,
        Arguments = Arguments,
        WorkingDirectory = WorkingDirectory,
    };

    private void SetField(ref string field, string value, [CallerMemberName] string? propertyName = null)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
