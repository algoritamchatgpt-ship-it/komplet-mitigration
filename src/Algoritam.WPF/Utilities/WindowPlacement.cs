using System.IO;
using System.Text.Json;
using System.Windows;

namespace Algoritam.WPF.Utilities;

public static class WindowPlacement
{
    private static readonly string _dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Algoritam", "WindowPlacement");

    private record WindowBounds(double Left, double Top, double Width, double Height, bool Maximized);

    public static void Restore(Window window, string key, double defaultWidth = 0, double defaultHeight = 0)
    {
        try
        {
            var file = Path.Combine(_dir, $"{key}.json");
            if (!File.Exists(file)) return;

            var b = JsonSerializer.Deserialize<WindowBounds>(File.ReadAllText(file));
            if (b == null) return;

            var area = SystemParameters.WorkArea;
            var left = Math.Max(area.Left, Math.Min(b.Left, area.Right - 100));
            var top = Math.Max(area.Top, Math.Min(b.Top, area.Bottom - 60));
            var w = b.Width > 200 ? b.Width : (defaultWidth > 0 ? defaultWidth : window.Width);
            var h = b.Height > 100 ? b.Height : (defaultHeight > 0 ? defaultHeight : window.Height);

            window.Left = left;
            window.Top = top;
            window.Width = w;
            window.Height = h;
            if (b.Maximized) window.WindowState = WindowState.Maximized;
        }
        catch { }
    }

    public static void Save(Window window, string key)
    {
        try
        {
            Directory.CreateDirectory(_dir);
            var maximized = window.WindowState == WindowState.Maximized;
            var bounds = maximized
                ? new WindowBounds(window.RestoreBounds.Left, window.RestoreBounds.Top,
                                   window.RestoreBounds.Width, window.RestoreBounds.Height, true)
                : new WindowBounds(window.Left, window.Top, window.Width, window.Height, false);
            File.WriteAllText(Path.Combine(_dir, $"{key}.json"),
                JsonSerializer.Serialize(bounds));
        }
        catch { }
    }
}
