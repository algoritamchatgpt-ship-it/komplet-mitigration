using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public partial class LdOz08ViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private string _naslov = "OBRAZAC OZ 8 BOLOVANJE PRVO USKLADJIVANJE";
    [ObservableProperty] private ObservableCollection<LdOzUskladjivanjeStavka> _stavke = [];
    [ObservableProperty] private LdOzUskladjivanjeStavka? _selektovana;
    [ObservableProperty] private string _poruka = string.Empty;

    public event Action? ZatvaranjeZatrazeno;

    public LdOz08ViewModel(AppState appState)
    {
        _folderPath = appState.AktivnaFirma?.FolderPath ?? string.Empty;
        Ucitaj();
    }

    [RelayCommand]
    private void ObrazacNaknada()
    {
        if (Selektovana is null)
        {
            Poruka = "Nije selektovan red.";
            return;
        }

        var view = new Views.Zarade.LdBolGenericReportView(
            "LDOZ080 - OBRAZAC NAKNADA",
            new[] { Selektovana },
            1);
        view.ShowDialog();
    }

    [RelayCommand]
    private void Dodaj()
    {
        var nova = new LdOzUskladjivanjeStavka();
        Stavke.Add(nova);
        Selektovana = nova;
        Sacuvaj();
        Poruka = "Dodat je novi red.";
    }

    [RelayCommand]
    private void Preracun()
    {
        foreach (var red in Stavke)
        {
            red.K1posatu = red.K1sati == 0 ? 0 : LdBolovanjeDbfSupport.Round(red.K1zarada / red.K1sati, 0);
            red.K1prosmes = LdBolovanjeDbfSupport.Round(red.K1mogsati / 3m, 0);
            red.K1pzarada = red.K1posatu * red.K1prosmes;

            red.K2posatu = red.K2sati == 0 ? 0 : LdBolovanjeDbfSupport.Round(red.K2zarada / red.K2sati, 0);
            red.K2prosmes = red.K2mogsati;
            red.K2pzarada = red.K2posatu * red.K2prosmes;

            red.Koef = red.K1pzarada == 0 ? 0 : LdBolovanjeDbfSupport.Round(red.K2pzarada / red.K1pzarada, 4);
        }

        Stavke = new ObservableCollection<LdOzUskladjivanjeStavka>(Stavke);
        Selektovana = Stavke.FirstOrDefault();
        Sacuvaj();
        Poruka = "Preracun je zavrsen.";
    }

    [RelayCommand]
    private void KarticaF7()
    {
        if (Selektovana is null)
        {
            Poruka = "Nije selektovan red.";
            return;
        }

        var view = new Views.Zarade.LdBolRowEditorView("LDOZ08K - KARTICA F7", Selektovana);
        view.ShowDialog();
        Sacuvaj();
        Poruka = "Izmene su sačuvane.";
    }

    [RelayCommand]
    private void Brisanje()
    {
        Stavke.Clear();
        Sacuvaj();
        Poruka = "Tabela je obrisana.";
        ZatvaranjeZatrazeno?.Invoke();
    }

    [RelayCommand]
    private void SacuvajRucno()
    {
        Sacuvaj();
        Poruka = "Izmene su sačuvane.";
    }

    [RelayCommand]
    private void Izlaz() => ZatvaranjeZatrazeno?.Invoke();

    private void Ucitaj()
    {
        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var dbfPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, "ldoz08.dbf");
        if (dbfPath is null)
        {
            Poruka = "ldoz08.dbf nije pronađen.";
            return;
        }

        try
        {
            foreach (var z in DbfReader.CitajSveZapise(dbfPath))
            {
                Stavke.Add(new LdOzUskladjivanjeStavka
                {
                    K1zarada = LdBolovanjeDbfSupport.Dec(z, "K1ZARADA"),
                    K1sati = LdBolovanjeDbfSupport.Dec(z, "K1SATI"),
                    K1posatu = LdBolovanjeDbfSupport.Dec(z, "K1POSATU"),
                    K1mogsati = LdBolovanjeDbfSupport.Dec(z, "K1MOGSATI"),
                    K1prosmes = LdBolovanjeDbfSupport.Dec(z, "K1PROSMES"),
                    K1pzarada = LdBolovanjeDbfSupport.Dec(z, "K1PZARADA"),
                    K2zarada = LdBolovanjeDbfSupport.Dec(z, "K2ZARADA"),
                    K2sati = LdBolovanjeDbfSupport.Dec(z, "K2SATI"),
                    K2posatu = LdBolovanjeDbfSupport.Dec(z, "K2POSATU"),
                    K2mogsati = LdBolovanjeDbfSupport.Dec(z, "K2MOGSATI"),
                    K2prosmes = LdBolovanjeDbfSupport.Dec(z, "K2PROSMES"),
                    K2pzarada = LdBolovanjeDbfSupport.Dec(z, "K2PZARADA"),
                    Koef = LdBolovanjeDbfSupport.Dec(z, "KOEF"),
                    Mesec1 = LdBolovanjeDbfSupport.Str(z, "MESEC1"),
                    Mesec2 = LdBolovanjeDbfSupport.Str(z, "MESEC2"),
                    Preneto = LdBolovanjeDbfSupport.Str(z, "PRENETO"),
                    Idbr = LdBolovanjeDbfSupport.Long(z, "IDBR")
                });
            }

            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {Stavke.Count} redova iz ldoz08.dbf.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri ucitavanju: {ex.Message}";
        }
    }

    private void Sacuvaj()
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
            return;

        try
        {
            LdBolovanjeDbfSupport.SacuvajTabelu(
                _folderPath,
                "ldoz08.dbf",
                Stavke.ToList(),
                ResolveValue);
        }
        catch
        {
            // Poruka postavlja caller.
        }
    }

    private static object? ResolveValue(LdOzUskladjivanjeStavka row, string fieldName) => fieldName.ToUpperInvariant() switch
    {
        "K1ZARADA" => row.K1zarada,
        "K1SATI" => row.K1sati,
        "K1POSATU" => row.K1posatu,
        "K1MOGSATI" => row.K1mogsati,
        "K1PROSMES" => row.K1prosmes,
        "K1PZARADA" => row.K1pzarada,
        "K2ZARADA" => row.K2zarada,
        "K2SATI" => row.K2sati,
        "K2POSATU" => row.K2posatu,
        "K2MOGSATI" => row.K2mogsati,
        "K2PROSMES" => row.K2prosmes,
        "K2PZARADA" => row.K2pzarada,
        "KOEF" => row.Koef,
        "MESEC1" => LdBolovanjeDbfSupport.NormalizeText(row.Mesec1),
        "MESEC2" => LdBolovanjeDbfSupport.NormalizeText(row.Mesec2),
        "PRENETO" => LdBolovanjeDbfSupport.NormalizeText(row.Preneto),
        "IDBR" => row.Idbr,
        _ => null
    };
}
