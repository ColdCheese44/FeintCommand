using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using FeintCommand.Models;
using Microsoft.Win32;

namespace FeintCommand;

public partial class ProgramEditorWindow : Window
{
    public ProgramEditorWindow(FeintProgram program)
    {
        DraftProgram = program.Clone();
        Targets = new ObservableCollection<LaunchTarget>(DraftProgram.Targets);
        InitializeComponent();
        DataContext = this;
        TargetsList.SelectedIndex = Targets.Count > 0 ? 0 : -1;
    }

    public FeintProgram DraftProgram { get; }

    public ObservableCollection<LaunchTarget> Targets { get; }

    public string[] AccentOptions { get; } = ["#4FA9FF", "#42D77D", "#F4B942", "#B078FF", "#FF6B86", "#42D6C4"];

    public event Action<FeintProgram>? Saved;

    private void AddTarget_Click(object sender, RoutedEventArgs e)
    {
        LaunchTarget target = new() { Name = $"Component {Targets.Count + 1}" };
        Targets.Add(target);
        TargetsList.SelectedItem = target;
    }

    private void RemoveTarget_Click(object sender, RoutedEventArgs e)
    {
        if (TargetsList.SelectedItem is not LaunchTarget target)
        {
            return;
        }

        int index = TargetsList.SelectedIndex;
        Targets.Remove(target);
        TargetsList.SelectedIndex = Math.Min(index, Targets.Count - 1);
    }

    private void BrowseCommand_Click(object sender, RoutedEventArgs e)
    {
        if (TargetsList.SelectedItem is not LaunchTarget target)
        {
            return;
        }

        OpenFileDialog dialog = new()
        {
            Title = "Choose an executable or script",
            Filter = "Launchable files|*.exe;*.bat;*.cmd;*.ps1;*.lnk|All files|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) == true)
        {
            target.Command = dialog.FileName;
            if (string.IsNullOrWhiteSpace(target.WorkingDirectory))
            {
                target.WorkingDirectory = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
            }
        }
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        if (TargetsList.SelectedItem is not LaunchTarget target)
        {
            return;
        }

        OpenFolderDialog dialog = new()
        {
            Title = "Choose the working directory",
            Multiselect = false,
        };

        if (dialog.ShowDialog(this) == true)
        {
            target.WorkingDirectory = dialog.FolderName;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(DraftProgram.Name))
        {
            ValidationText.Text = "Give this program a name.";
            return;
        }

        if (Targets.Count == 0)
        {
            ValidationText.Text = "Add at least one launch target.";
            return;
        }

        if (Targets.Any(target => string.IsNullOrWhiteSpace(target.Name)))
        {
            ValidationText.Text = "Every launch target needs a button label.";
            return;
        }

        DraftProgram.Name = DraftProgram.Name.Trim();
        DraftProgram.Description = DraftProgram.Description.Trim();
        DraftProgram.Targets = Targets.ToList();
        Saved?.Invoke(DraftProgram);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
