using System.Windows;
using System.Windows.Input;

namespace Algoritam.WPF.Views.Zarade;

public partial class UputstvaIzborWindow : Window
{
    public sealed class UputstvoOpcija
    {
        public required string Naziv { get; init; }
        public required string Opis { get; init; }
        public required string Putanja { get; init; }
    }

    public UputstvoOpcija? IzabranoUputstvo => UputstvaListBox.SelectedItem as UputstvoOpcija;

    public UputstvaIzborWindow(IEnumerable<UputstvoOpcija> uputstva)
    {
        InitializeComponent();

        var lista = uputstva?.ToList() ?? [];
        UputstvaListBox.ItemsSource = lista;

        if (lista.Count > 0)
            UputstvaListBox.SelectedIndex = 0;
    }

    private void OtvoriClick(object sender, RoutedEventArgs e) => PotvrdiIzbor();

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();

    private void UputstvaListBoxMouseDoubleClick(object sender, MouseButtonEventArgs e) => PotvrdiIzbor();

    private void PotvrdiIzbor()
    {
        if (IzabranoUputstvo is null)
            return;

        DialogResult = true;
        Close();
    }
}
