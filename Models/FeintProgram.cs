using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace FeintCommand.Models;

public sealed class FeintProgram : INotifyPropertyChanged
{
    private bool _isRunning;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New Feint program";
    public string Description { get; set; } = "FeintAI application";
    public string AccentColor { get; set; } = "#4FA9FF";
    public List<LaunchTarget> Targets { get; set; } = [];

    [JsonIgnore]
    public bool IsConfigured => Targets.Count > 0 && Targets.Any(IsTargetConfigured);

    [JsonIgnore]
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (_isRunning == value)
            {
                return;
            }

            _isRunning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }

    [JsonIgnore]
    public string StatusText => IsRunning ? "RUNNING" : IsConfigured ? "READY" : "SETUP NEEDED";

    [JsonIgnore]
    public Brush AccentBrush
    {
        get
        {
            try
            {
                return (Brush)new BrushConverter().ConvertFromString(AccentColor)!;
            }
            catch (FormatException)
            {
                return Brushes.DodgerBlue;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public FeintProgram Clone() => new()
    {
        Id = Id,
        Name = Name,
        Description = Description,
        AccentColor = AccentColor,
        Targets = Targets.Select(target => target.Clone()).ToList(),
    };

    public void RefreshComputedProperties()
    {
        OnPropertyChanged(nameof(IsConfigured));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(AccentBrush));
    }

    private static bool IsTargetConfigured(LaunchTarget target)
    {
        if (string.IsNullOrWhiteSpace(target.Command))
        {
            return false;
        }

        string command = Environment.ExpandEnvironmentVariables(target.Command);
        return File.Exists(command)
            || Directory.Exists(command)
            || Uri.TryCreate(target.Command, UriKind.Absolute, out Uri? uri) && uri.Scheme is "http" or "https";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
