using System.Diagnostics;
using System.IO;
using FeintCommand.Models;

namespace FeintCommand.Services;

public sealed class ProcessLauncher
{
    private readonly Dictionary<string, Process> _processes = [];

    public Process Launch(LaunchTarget target)
    {
        string command = Environment.ExpandEnvironmentVariables(target.Command.Trim());
        string workingDirectory = Environment.ExpandEnvironmentVariables(target.WorkingDirectory.Trim());

        if (string.IsNullOrWhiteSpace(command))
        {
            throw new InvalidOperationException("Choose a command before launching this target.");
        }

        ProcessStartInfo startInfo = CreateStartInfo(command, target.Arguments, workingDirectory);
        Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Windows did not start the selected command.");

        _processes[target.Id] = process;
        return process;
    }

    public bool IsRunning(LaunchTarget target)
    {
        if (!_processes.TryGetValue(target.Id, out Process? process))
        {
            return false;
        }

        try
        {
            return !process.HasExited;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static void OpenFolder(string path)
    {
        string expandedPath = Environment.ExpandEnvironmentVariables(path);
        if (!Directory.Exists(expandedPath))
        {
            throw new DirectoryNotFoundException($"Folder not found: {expandedPath}");
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{expandedPath}\"") { UseShellExecute = true });
    }

    private static ProcessStartInfo CreateStartInfo(string command, string arguments, string workingDirectory)
    {
        if (Uri.TryCreate(command, UriKind.Absolute, out Uri? uri) && uri.Scheme is "http" or "https")
        {
            return BrowserLauncher.CreateStartInfo(command);
        }

        if (!File.Exists(command) && !Directory.Exists(command))
        {
            throw new FileNotFoundException("Command not found. Edit this program and choose the correct executable or script.", command);
        }

        if (Directory.Exists(command))
        {
            return new ProcessStartInfo("explorer.exe", $"\"{command}\"") { UseShellExecute = true };
        }

        string extension = Path.GetExtension(command).ToLowerInvariant();
        ProcessStartInfo startInfo = extension switch
        {
            ".ps1" => new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{command}\" {arguments}"),
            ".bat" or ".cmd" => new ProcessStartInfo("cmd.exe", $"/c \"\"{command}\" {arguments}\""),
            _ => new ProcessStartInfo(command, arguments) { UseShellExecute = true },
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory) && Directory.Exists(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        return startInfo;
    }
}
