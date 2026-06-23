using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using FeintCommand.Models;
using FeintCommand.Services;

namespace FeintCommand;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly ConfigurationService _configurationService = new();
    private readonly EnvironmentService _environmentService = new();
    private readonly ProcessLauncher _processLauncher = new();
    private readonly DispatcherTimer _statusTimer;
    private LauncherConfiguration _configuration;
    private EnvironmentSettings _environmentSettings;
    private string _selectedTheme;
    private string _statusMessage = "Ready.";
    private bool _isInitialized;

    public MainWindow()
    {
        _configuration = _configurationService.Load();
        _environmentSettings = _environmentService.Load();
        _selectedTheme = ThemeService.Options.Contains(_configuration.Theme) ? _configuration.Theme : "System";
        ThemeService.Apply(_selectedTheme);

        Programs = new ObservableCollection<FeintProgram>(_configuration.Programs);
        CommandCenterChannels = new ObservableCollection<CommandCenterChannel>(CreateCommandCenterChannels());
        Activities = [];
        InitializeComponent();
        DataContext = this;

        AddActivity("FeintCommand ready.");
        if (_environmentSettings.HasDiscordServerId)
        {
            AddActivity("Discord command center linked.");
        }
        else
        {
            AddActivity("Add FEINTCOMMAND_SERVER_ID to .env to link Discord.");
        }

        int setupCount = Programs.Count(program => !program.IsConfigured);
        if (setupCount > 0)
        {
            AddActivity($"{setupCount} profile{(setupCount == 1 ? string.Empty : "s")} need setup.");
        }

        RefreshStatus();
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _statusTimer.Tick += (_, _) => RefreshStatus(false);
        _statusTimer.Start();
        Closed += (_, _) => _statusTimer.Stop();
        _isInitialized = true;
    }

    public ObservableCollection<FeintProgram> Programs { get; }

    public ObservableCollection<CommandCenterChannel> CommandCenterChannels { get; }

    public ObservableCollection<ActivityEntry> Activities { get; }

    public string[] ThemeOptions => ThemeService.Options;

    public string ConfigurationPath => _configurationService.ConfigurationPath;

    public string EnvironmentFilePath => _environmentSettings.EnvironmentFileDisplay;

    public string PreferredEnvironmentFilePath => _environmentSettings.PreferredEnvironmentFilePath;

    public string DiscordServerId => _environmentSettings.DiscordServerIdDisplay;

    public string DiscordServerIdSource => _environmentSettings.DiscordServerIdSource;

    public string DiscordServerStatus => _environmentSettings.DiscordStatusText;

    public string DiscordServerUrl => _environmentSettings.DiscordServerUrl;

    public bool HasDiscordServerId => _environmentSettings.HasDiscordServerId;

    public bool HasLikelyDiscordServerId => _environmentSettings.HasLikelyDiscordServerId;

    public bool HasEnvironmentFile => _environmentSettings.HasEnvironmentFile;

    public int ProgramCount => Programs.Count;

    public int ConfiguredCount => Programs.Count(program => program.IsConfigured);

    public int RunningCount => Programs.Count(program => program.IsRunning);

    public int TargetCount => Programs.Sum(program => program.Targets.Count);

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme == value)
            {
                return;
            }

            _selectedTheme = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (DashboardView is not null && sender is FrameworkElement { Tag: string destination })
        {
            ShowView(destination);
        }
    }

    private void ShowView(string destination)
    {
        DashboardView.Visibility = destination == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
        ProgramsView.Visibility = destination == "Programs" ? Visibility.Visible : Visibility.Collapsed;
        SettingsView.Visibility = destination == "Settings" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LaunchTarget_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: LaunchTarget target })
        {
            return;
        }

        FeintProgram? program = Programs.FirstOrDefault(candidate => candidate.Targets.Contains(target));
        if (program is null)
        {
            return;
        }

        try
        {
            _processLauncher.Launch(target);
            program.IsRunning = true;
            AddActivity($"Started {program.Name}: {target.Name}");
            ShowStatus($"Started {target.Name}.");
            RefreshStatus(false);
        }
        catch (Exception exception) when (exception is InvalidOperationException or FileNotFoundException or Win32Exception)
        {
            AddActivity($"Could not start {target.Name}: {exception.Message}", true);
            ShowStatus(exception.Message, true);

            if (MessageBox.Show(
                    this,
                    $"{exception.Message}\n\nOpen the {program.Name} profile now?",
                    "Launch target needs attention",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                EditProgram(program);
            }
        }
    }

    private void AddProgram_Click(object sender, RoutedEventArgs e)
    {
        ShowStatus("Opening a new program profile...");
        FeintProgram program = new()
        {
            Name = "New Feint program",
            Description = "FeintAI application",
            Targets = [new LaunchTarget { Name = "Launch" }],
        };

        ProgramEditorWindow editor = new(program) { Owner = this };
        editor.Saved += savedProgram =>
        {
            Programs.Add(savedProgram);
            SaveConfiguration();
            AddActivity($"Added {savedProgram.Name}.");
            ShowStatus($"Added {savedProgram.Name}.");
            RefreshStatus(false);
        };
        editor.Show();
    }

    private void EditProgram_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: FeintProgram program })
        {
            EditProgram(program);
        }
    }

    private void EditProgram(FeintProgram program)
    {
        ShowStatus($"Opening {program.Name} profile...");
        ProgramEditorWindow editor = new(program) { Owner = this };
        editor.Saved += savedProgram =>
        {
            int index = Programs.IndexOf(program);
            if (index >= 0)
            {
                Programs[index] = savedProgram;
            }

            SaveConfiguration();
            AddActivity($"Updated {savedProgram.Name}.");
            ShowStatus($"Saved {savedProgram.Name}.");
            RefreshStatus(false);
        };
        editor.Show();
    }

    private void RemoveProgram_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: FeintProgram program })
        {
            return;
        }

        MessageBoxResult result = MessageBox.Show(
            this,
            $"Remove {program.Name} from FeintCommand? This will not delete the program itself.",
            "Remove launcher profile",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        Programs.Remove(program);
        SaveConfiguration();
        AddActivity($"Removed {program.Name}.");
        ShowStatus($"Removed {program.Name}.");
        RefreshStatus(false);
    }

    private void OpenProgramFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: FeintProgram program })
        {
            return;
        }

        LaunchTarget? target = program.Targets.FirstOrDefault();
        string path = target?.WorkingDirectory ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path) && target is not null)
        {
            string command = Environment.ExpandEnvironmentVariables(target.Command);
            path = Directory.Exists(command) ? command : Path.GetDirectoryName(command) ?? string.Empty;
        }

        try
        {
            ProcessLauncher.OpenFolder(path);
            ShowStatus($"Opened {program.Name} folder.");
        }
        catch (DirectoryNotFoundException exception)
        {
            ShowStatus(exception.Message, true);
            EditProgram(program);
        }
    }

    private void OpenConfigFolder_Click(object sender, RoutedEventArgs e)
    {
        ProcessLauncher.OpenFolder(_configurationService.ConfigurationDirectory);
        ShowStatus("Opened the FeintCommand configuration folder.");
    }

    private void OpenConfiguration_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{_configurationService.ConfigurationPath}\"") { UseShellExecute = true });
        ShowStatus("Opened launcher.json.");
    }

    private void OpenEnvironmentFile_Click(object sender, RoutedEventArgs e)
    {
        string path = _environmentSettings.HasEnvironmentFile
            ? _environmentSettings.EnvironmentFilePath!
            : _environmentSettings.PreferredEnvironmentFilePath;
        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"") { UseShellExecute = true });
        ShowStatus(_environmentSettings.HasEnvironmentFile ? "Opened .env." : "Opened the preferred .env path.");
    }

    private void OpenProjectFolder_Click(object sender, RoutedEventArgs e)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        ProcessLauncher.OpenFolder(directory?.FullName ?? AppContext.BaseDirectory);
        ShowStatus("Opened the FeintCommand project folder.");
    }

    private void OpenDiscordServer_Click(object sender, RoutedEventArgs e)
    {
        if (!HasDiscordServerId)
        {
            ShowStatus($"Add {EnvironmentService.DiscordServerIdKey} to .env first.", true);
            return;
        }

        if (BrowserLauncher.OpenUrl(DiscordServerUrl))
        {
            ShowStatus("Opened the FeintCommand Discord server.");
        }
        else
        {
            ShowStatus("Could not open the FeintCommand Discord server.", true);
        }
    }

    private void CopyDiscordServerId_Click(object sender, RoutedEventArgs e)
    {
        if (!HasDiscordServerId)
        {
            ShowStatus($"No {EnvironmentService.DiscordServerIdKey} value is configured.", true);
            return;
        }

        Clipboard.SetText(_environmentSettings.DiscordServerId);
        ShowStatus("Copied Discord server ID.");
    }

    private void CopyChannelBlueprint_Click(object sender, RoutedEventArgs e)
    {
        string blueprint = string.Join(
            Environment.NewLine,
            CommandCenterChannels.Select(channel => $"{channel.Category} / #{channel.Name} - {channel.Purpose}"));
        Clipboard.SetText(blueprint);
        ShowStatus("Copied Discord channel blueprint.");
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(
                this,
                "Replace all current launcher profiles with the FeintTrade, FeintSupplyCo, and FeintSignal starters?",
                "Reset launcher profiles",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        LauncherConfiguration defaults = _configurationService.CreateDefaults();
        Programs.Clear();
        foreach (FeintProgram program in defaults.Programs)
        {
            Programs.Add(program);
        }

        SaveConfiguration();
        AddActivity("Restored starter profiles.");
        ShowStatus("Starter profiles restored.");
        RefreshStatus(false);
    }

    private void ThemeCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_isInitialized || ThemeCombo.SelectedItem is not string theme)
        {
            return;
        }

        SelectedTheme = theme;
        ThemeService.Apply(theme);
        SaveConfiguration();
        ShowStatus($"Theme set to {theme}.");
        ApplyWindowBackdrop();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshEnvironmentSettings(false);
        RefreshStatus();
        AddActivity("Refreshed launcher status.");
    }

    private void RefreshStatus(bool showMessage = true)
    {
        foreach (FeintProgram program in Programs)
        {
            program.IsRunning = program.Targets.Any(_processLauncher.IsRunning);
            program.RefreshComputedProperties();
        }

        OnPropertyChanged(nameof(ProgramCount));
        OnPropertyChanged(nameof(ConfiguredCount));
        OnPropertyChanged(nameof(RunningCount));
        OnPropertyChanged(nameof(TargetCount));

        if (showMessage)
        {
            ShowStatus($"Ready. {ConfiguredCount} of {ProgramCount} programs configured.");
        }
    }

    private void RefreshEnvironmentSettings(bool showMessage = true)
    {
        _environmentSettings = _environmentService.Load();
        OnPropertyChanged(nameof(EnvironmentFilePath));
        OnPropertyChanged(nameof(PreferredEnvironmentFilePath));
        OnPropertyChanged(nameof(DiscordServerId));
        OnPropertyChanged(nameof(DiscordServerIdSource));
        OnPropertyChanged(nameof(DiscordServerStatus));
        OnPropertyChanged(nameof(DiscordServerUrl));
        OnPropertyChanged(nameof(HasDiscordServerId));
        OnPropertyChanged(nameof(HasLikelyDiscordServerId));
        OnPropertyChanged(nameof(HasEnvironmentFile));

        if (showMessage)
        {
            ShowStatus("Environment settings refreshed.");
        }
    }

    private void SaveConfiguration()
    {
        _configuration.Theme = SelectedTheme;
        _configuration.Programs = Programs.ToList();
        _configurationService.Save(_configuration);
    }

    private void AddActivity(string message, bool isError = false)
    {
        Activities.Insert(0, new ActivityEntry(DateTimeOffset.Now, message, isError));
        while (Activities.Count > 4)
        {
            Activities.RemoveAt(Activities.Count - 1);
        }
    }

    private void ShowStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        StatusDot.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, isError ? "DangerBrush" : "SuccessBrush");
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            Refresh_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.N && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            AddProgram_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.OemComma && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            ShowView("Settings");
            e.Handled = true;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
        }
        else
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximize() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Window_SourceInitialized(object? sender, EventArgs e) => ApplyWindowBackdrop();

    private void ApplyWindowBackdrop()
    {
        try
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            int backdrop = 2;
            DwmSetWindowAttribute(handle, 38, ref backdrop, sizeof(int));
            int darkMode = SelectedTheme == "Dark" || SelectedTheme == "System" && !SystemParameters.HighContrast ? 1 : 0;
            DwmSetWindowAttribute(handle, 20, ref darkMode, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // The theme-aware solid background remains available on older Windows versions.
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static IEnumerable<CommandCenterChannel> CreateCommandCenterChannels() =>
    [
        new CommandCenterChannel
        {
            Category = "FeintCommand",
            Name = "command-center",
            Purpose = "Pinned overview, active priorities, and operator shortcuts.",
        },
        new CommandCenterChannel
        {
            Category = "FeintCommand",
            Name = "announcements",
            Purpose = "Major FeintAI platform notices and release timing.",
        },
        new CommandCenterChannel
        {
            Category = "FeintCommand",
            Name = "status-rollups",
            Purpose = "Brief cross-app health summaries from each Feint program.",
        },
        new CommandCenterChannel
        {
            Category = "FeintCommand",
            Name = "release-notes",
            Purpose = "Short changelogs for FeintCommand and connected apps.",
        },
        new CommandCenterChannel
        {
            Category = "FeintCommand",
            Name = "roadmap",
            Purpose = "Near-term launcher, automation, and standalone-hosting plans.",
        },
        new CommandCenterChannel
        {
            Category = "FeintCommand",
            Name = "incidents",
            Purpose = "Cross-platform outages, degraded services, and recovery notes.",
        },
        new CommandCenterChannel
        {
            Category = "FeintSignal",
            Name = "signal-summary",
            Purpose = "Brief FeintSignal intelligence summaries and handoffs.",
        },
        new CommandCenterChannel
        {
            Category = "FeintSupplyCo",
            Name = "supplyco-summary",
            Purpose = "Brief supply, inventory, and operations rollups.",
        },
        new CommandCenterChannel
        {
            Category = "FeintTrade",
            Name = "trade-summary",
            Purpose = "Brief trading workspace status and market-operation notes.",
        },
        new CommandCenterChannel
        {
            Category = "FeintAI Network",
            Name = "app-directory",
            Purpose = "Links to each Feint app server, repo, docs, and dashboard.",
        },
        new CommandCenterChannel
        {
            Category = "FeintAI Network",
            Name = "support-requests",
            Purpose = "Cross-app requests that do not belong inside one product server.",
        },
    ];

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute, ref int value, int size);
}
