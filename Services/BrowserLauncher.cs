using System.Diagnostics;
using System.IO;

namespace FeintCommand.Services;

public static class BrowserLauncher
{
    public const string BrowserPathKey = "FEINT_BROWSER_PATH";
    public const string BrowserKey = "FEINT_BROWSER";
    public const string BrowserModeKey = "FEINT_BROWSER_MODE";

    private static readonly string[] BravePathCandidates =
    [
        @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe",
        @"C:\Program Files (x86)\BraveSoftware\Brave-Browser\Application\brave.exe",
        @"%LOCALAPPDATA%\BraveSoftware\Brave-Browser\Application\brave.exe",
    ];

    private static readonly string[] BraveCommandCandidates =
    [
        "brave.exe",
        "brave-browser.exe",
        "brave",
        "brave-browser",
    ];

    private static readonly EnvironmentService EnvironmentService = new();

    public static ProcessStartInfo CreateStartInfo(string url, string? mode = null)
    {
        string? browserPath = FindBrowserExecutable();
        if (browserPath is null)
        {
            LogWarning("Brave was not found. Falling back to the default browser.");
            return new ProcessStartInfo(url) { UseShellExecute = true };
        }

        ProcessStartInfo startInfo = new(browserPath)
        {
            UseShellExecute = false,
        };

        foreach (string argument in CreateBrowserArguments(url, mode))
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    public static bool OpenUrl(string url, string? mode = null)
    {
        try
        {
            Process.Start(CreateStartInfo(url, mode));
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            LogWarning($"Browser launch failed. Falling back to the default browser. {exception.Message}");
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            LogWarning($"Default browser launch failed. {exception.Message}");
            return false;
        }
    }

    public static string[] CreateBrowserArguments(string url, string? mode = null)
    {
        string normalizedMode = NormalizeBrowserMode(mode ?? GetSetting(BrowserModeKey) ?? "fullscreen");
        List<string> arguments = ["--new-window"];

        switch (normalizedMode)
        {
            case "fullscreen":
                arguments.Add("--start-fullscreen");
                break;
            case "maximized":
                arguments.Add("--start-maximized");
                break;
            case "kiosk":
                arguments.Add("--kiosk");
                break;
            case "normal":
                break;
        }

        arguments.Add(url);
        return [.. arguments];
    }

    public static string? FindBrowserExecutable()
    {
        string? explicitPath = GetSetting(BrowserPathKey);
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            string expandedPath = Environment.ExpandEnvironmentVariables(explicitPath.Trim());
            if (File.Exists(expandedPath))
            {
                return expandedPath;
            }

            LogWarning($"{BrowserPathKey} is set, but the file was not found.");
        }

        string requestedBrowser = GetSetting(BrowserKey) ?? "brave";
        string expandedBrowser = Environment.ExpandEnvironmentVariables(requestedBrowser.Trim());
        if (File.Exists(expandedBrowser))
        {
            return expandedBrowser;
        }

        if (requestedBrowser.Equals("default", StringComparison.OrdinalIgnoreCase)
            || requestedBrowser.Equals("system", StringComparison.OrdinalIgnoreCase))
        {
            LogWarning($"{BrowserKey} requested the default browser.");
            return null;
        }

        foreach (string candidate in BravePathCandidates)
        {
            string expandedCandidate = Environment.ExpandEnvironmentVariables(candidate);
            if (File.Exists(expandedCandidate))
            {
                return expandedCandidate;
            }
        }

        foreach (string command in BraveCommandCandidates)
        {
            string? path = FindExecutableOnPath(command);
            if (path is not null)
            {
                return path;
            }
        }

        if (!requestedBrowser.Equals("brave", StringComparison.OrdinalIgnoreCase)
            && !requestedBrowser.Equals("brave-browser", StringComparison.OrdinalIgnoreCase))
        {
            string? requestedPath = FindExecutableOnPath(requestedBrowser);
            if (requestedPath is not null)
            {
                return requestedPath;
            }

            LogWarning($"{BrowserKey} requested '{requestedBrowser}', but it was not found.");
        }

        return null;
    }

    private static string NormalizeBrowserMode(string mode)
    {
        string normalizedMode = mode.Trim().ToLowerInvariant();
        return normalizedMode is "fullscreen" or "maximized" or "normal" or "kiosk"
            ? normalizedMode
            : "fullscreen";
    }

    private static string? GetSetting(string key)
    {
        string? value = EnvironmentService.GetValue(key);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? FindExecutableOnPath(string command)
    {
        if (command.Contains(Path.DirectorySeparatorChar) || command.Contains(Path.AltDirectorySeparatorChar))
        {
            return File.Exists(command) ? command : null;
        }

        string[] extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM").Split(';', StringSplitOptions.RemoveEmptyEntries)
            : [string.Empty];
        string[] names = Path.HasExtension(command)
            ? [command]
            : extensions.Select(extension => command + extension.ToLowerInvariant()).Prepend(command).ToArray();

        foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            foreach (string name in names)
            {
                string candidate = Path.Combine(directory.Trim(), name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static void LogWarning(string message)
    {
        try
        {
            string directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FeintAI",
                "FeintCommand");
            Directory.CreateDirectory(directory);
            File.AppendAllText(
                Path.Combine(directory, "browser.log"),
                $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}");
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
