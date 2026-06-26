using Algoritam.WPF.Models;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class KontaktView : Window
{
    public KontaktView()
    {
        InitializeComponent();
        PopuniPodatke();
    }

    private void PopuniPodatke()
    {
        var naziv = KontaktPodaci.NazivFirme?.Trim();
        TxtNazivFirme.Text       = string.IsNullOrWhiteSpace(naziv) ? "" : naziv;
        TxtNazivFirme.Visibility = string.IsNullOrWhiteSpace(naziv) ? Visibility.Collapsed : Visibility.Visible;
        TxtNapomena.Text         = KontaktPodaci.NapomenaFooter;

        PostaviPolje(PanelAdresa,     TxtAdresa,     KontaktPodaci.Adresa);
        PostaviPolje(PanelEmail,      TxtEmail,      KontaktPodaci.Email);
        PostaviPolje(PanelRadnoVreme, TxtRadnoVreme, KontaktPodaci.RadnoVreme);
        PostaviPolje(PanelWebsite,    TxtWebsite,    KontaktPodaci.Website);

        var telefon = string.IsNullOrWhiteSpace(KontaktPodaci.Telefon2)
            ? KontaktPodaci.Telefon1
            : $"{KontaktPodaci.Telefon1}\n{KontaktPodaci.Telefon2}";
        PostaviPolje(PanelTelefon, TxtTelefon, telefon);

        var imaPodataka = PanelAdresa.Visibility == Visibility.Visible
            || PanelTelefon.Visibility == Visibility.Visible
            || PanelEmail.Visibility == Visibility.Visible
            || PanelRadnoVreme.Visibility == Visibility.Visible
            || PanelWebsite.Visibility == Visibility.Visible;
        TxtPrazno.Visibility = imaPodataka ? Visibility.Collapsed : Visibility.Visible;
    }

    private static void PostaviPolje(UIElement panel, System.Windows.Controls.TextBlock txt, string vrednost)
    {
        if (string.IsNullOrWhiteSpace(vrednost))
        {
            panel.Visibility = Visibility.Collapsed;
        }
        else
        {
            txt.Text = vrednost.Trim();
            panel.Visibility = Visibility.Visible;
        }
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
