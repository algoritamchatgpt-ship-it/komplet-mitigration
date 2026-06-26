using System.Windows;
using System.Windows.Controls;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Mali WPF dialog za unos jednog broja/teksta — zamena za VisualBasic.InputBox.
/// </summary>
public static class LdBrojUnos
{
    public static string? Pitaj(string poruka, string naslov = "Unos", string podrazumevano = "")
    {
        var win = new Window
        {
            Title = naslov,
            Width = 340,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Background = System.Windows.Media.Brushes.WhiteSmoke,
        };

        var panel = new StackPanel { Margin = new Thickness(16) };

        panel.Children.Add(new TextBlock
        {
            Text = poruka,
            FontFamily = new System.Windows.Media.FontFamily("Tahoma"),
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var tb = new TextBox
        {
            Text = podrazumevano,
            FontFamily = new System.Windows.Media.FontFamily("Tahoma"),
            FontSize = 12,
            Padding = new Thickness(4, 3, 4, 3),
            Margin = new Thickness(0, 0, 0, 12),
        };
        panel.Children.Add(tb);

        var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        string? result = null;

        var ok = new Button
        {
            Content = "OK",
            Width = 80, Height = 30,
            IsDefault = true,
            Margin = new Thickness(0, 0, 8, 0),
            FontFamily = new System.Windows.Media.FontFamily("Tahoma"),
        };
        ok.Click += (_, _) => { result = tb.Text; win.Close(); };

        var cancel = new Button
        {
            Content = "Odustani",
            Width = 90, Height = 30,
            IsCancel = true,
            FontFamily = new System.Windows.Media.FontFamily("Tahoma"),
        };
        cancel.Click += (_, _) => win.Close();

        btns.Children.Add(ok);
        btns.Children.Add(cancel);
        panel.Children.Add(btns);

        win.Content = panel;
        tb.Focus();
        tb.SelectAll();
        win.ShowDialog();
        return result;
    }
}
