using System.Windows;
using System.Windows.Controls;

namespace Algoritam.WPF.Views.Zarade;

public partial class ZaradeMenuView : UserControl
{
    public ZaradeMenuView()
    {
        InitializeComponent();
    }

    private void BoxFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        var filter = BoxFilter.Text.Trim().ToLowerInvariant();
        BtnClearFilter.Visibility = string.IsNullOrEmpty(filter) ? Visibility.Collapsed : Visibility.Visible;

        TextBlock? lastHeader = null;
        WrapPanel? lastPanel = null;

        foreach (UIElement child in MenuSadrzaj.Children)
        {
            if (child is TextBlock header)
            {
                lastHeader = header;
                lastPanel = null;
            }
            else if (child is WrapPanel panel)
            {
                lastPanel = panel;
                bool anyVisible = false;
                foreach (UIElement btn in panel.Children)
                {
                    if (btn is not Button b) continue;
                    var text = (b.Content as string ?? "").Replace("\n", " ").ToLowerInvariant();
                    var tip  = (b.ToolTip  as string ?? "").ToLowerInvariant();
                    bool show = string.IsNullOrEmpty(filter) || text.Contains(filter) || tip.Contains(filter);
                    b.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                    if (show) anyVisible = true;
                }
                bool sectionVisible = string.IsNullOrEmpty(filter) || anyVisible;
                panel.Visibility = sectionVisible ? Visibility.Visible : Visibility.Collapsed;
                if (lastHeader != null) lastHeader.Visibility = sectionVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    private void BtnClearFilter_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        BoxFilter.Clear();
        BoxFilter.Focus();
    }
}
