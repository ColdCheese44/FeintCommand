using Microsoft.Win32;
using System.Windows;

namespace FeintCommand.Services;

public static class ThemeService
{
    public static readonly string[] Options = ["System", "Dark", "Light"];

    public static void Apply(string preference)
    {
        bool useLight = preference.Equals("Light", StringComparison.OrdinalIgnoreCase)
            || preference.Equals("System", StringComparison.OrdinalIgnoreCase) && SystemUsesLightTheme();
        string source = useLight ? "Themes/Light.xaml" : "Themes/Dark.xaml";

        ResourceDictionary resources = Application.Current.Resources;
        ResourceDictionary? existing = resources.MergedDictionaries.FirstOrDefault(
            dictionary => dictionary.Source?.OriginalString.Contains("Themes/", StringComparison.OrdinalIgnoreCase) == true);

        if (existing is not null)
        {
            resources.MergedDictionaries.Remove(existing);
        }

        resources.MergedDictionaries.Insert(0, new ResourceDictionary { Source = new Uri(source, UriKind.Relative) });
    }

    private static bool SystemUsesLightTheme()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is not int value || value != 0;
    }
}
