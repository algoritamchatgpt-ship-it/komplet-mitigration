using System.Windows;

namespace Algoritam.WPF.Controls;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string poruka, string naslov = "Potvrda")
    {
        InitializeComponent();
        TxtNaslov.Text = naslov;
        TxtPoruka.Text = poruka;
        KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Escape) { DialogResult = false; Close(); }
            if (e.Key == System.Windows.Input.Key.Enter)  { DialogResult = true;  Close(); }
        };
    }

    private void BtnDa_Click(object sender, RoutedEventArgs e) { DialogResult = true;  Close(); }
    private void BtnNe_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    public static bool Pitaj(string poruka, string naslov = "Potvrda", Window? owner = null)
    {
        var dlg = new ConfirmDialog(poruka, naslov);
        if (owner != null) dlg.Owner = owner;
        return dlg.ShowDialog() == true;
    }
}
