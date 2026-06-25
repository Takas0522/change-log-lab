using System.Windows;

namespace OrderClientApp.Wpf;

public static class ThemeManager
{
    public static void ApplyTheme(string? theme)
    {
        var target = string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase)
            ? "Dark"
            : "Light";
        var uri = new Uri($"/Themes/{target}Theme.xaml", UriKind.Relative);
        var resources = System.Windows.Application.Current.Resources;
        resources.MergedDictionaries.Clear();
        resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
    }
}
