using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalogIzborWindow : Window
{
    public string? IzabraniKod { get; private set; }

    public NalogIzborWindow(IEnumerable<string> nalozi)
    {
        InitializeComponent();
        foreach (var n in nalozi)
            LstNalozi.Items.Add(n);
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        IzabraniKod = KodIzTeksta();
        if (IzabraniKod == null) return;
        DialogResult = true;
    }

    private void LstNalozi_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LstNalozi.SelectedItem is string sel)
        {
            IzabraniKod = sel.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (IzabraniKod != null)
                DialogResult = true;
        }
    }

    private void TxtBrnal_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            IzabraniKod = KodIzTeksta();
            if (IzabraniKod != null) DialogResult = true;
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) DialogResult = false;
    }

    private string? KodIzTeksta()
    {
        var txt = TxtBrnal.Text.Trim();
        if (!string.IsNullOrEmpty(txt)) return txt.PadLeft(6);
        if (LstNalozi.SelectedItem is string sel)
            return sel.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return null;
    }
}
