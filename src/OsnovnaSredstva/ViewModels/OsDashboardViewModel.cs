using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using System.Collections.ObjectModel;

namespace OsnovnaSredstva.ViewModels;

public partial class OsDashboardViewModel : ObservableObject
{
    private readonly AppState _appState;

    [ObservableProperty] private int    _ukupnoKartica;
    [ObservableProperty] private decimal _ukupnaNabavna;
    [ObservableProperty] private decimal _ukupnaAmortizacija;
    [ObservableProperty] private decimal _ukupnoNeotpisano;
    [ObservableProperty] private string  _poruka = string.Empty;
    [ObservableProperty] private bool    _ucitava;
    [ObservableProperty] private string  _datumOsvezavanja = string.Empty;
    [ObservableProperty] private ObservableCollection<DashboardVrstaStavka> _stavkePoVrsti = [];

    public string NazivFirme => _appState.AktivnaFirma?.Naziv is { Length: > 0 } n ? n
                             : _appState.AktivnaFirma?.FolderIme ?? "—";

    public OsDashboardViewModel(AppState appState)
    {
        _appState = appState;
        Ucitaj();
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    private void Ucitaj()
    {
        Ucitava = true;
        Poruka  = "Učitavam...";

        var path = DbfHelper.NadjiDbf(_appState, "os.dbf");
        if (path is null)
        {
            Poruka  = "os.dbf nije pronađen u folderu firme.";
            Ucitava = false;
            return;
        }

        try
        {
            var reader = new SimpleDbfReader(path);

            int    ukBroj  = 0;
            decimal ukNab  = 0m;
            decimal ukIsp  = 0m;
            decimal ukSad  = 0m;

            var poVrsti = new Dictionary<string, (int Br, decimal Nab, decimal Isp, decimal Sad)>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var r in reader.Zapisi())
            {
                ukBroj++;
                var nab  = r.DajDecimal("NAB0");
                var isp  = r.DajDecimal("ISP0");
                var sad  = r.DajDecimal("SAD0");
                var vrsta = r.DajString("VRSTA").Trim();
                if (string.IsNullOrWhiteSpace(vrsta)) vrsta = "(bez vrste)";

                ukNab += nab;
                ukIsp += isp;
                ukSad += sad;

                if (!poVrsti.TryGetValue(vrsta, out var v)) v = default;
                poVrsti[vrsta] = (v.Br + 1, v.Nab + nab, v.Isp + isp, v.Sad + sad);
            }

            UkupnoKartica       = ukBroj;
            UkupnaNabavna       = ukNab;
            UkupnaAmortizacija  = ukIsp;
            UkupnoNeotpisano    = ukSad;

            StavkePoVrsti = new ObservableCollection<DashboardVrstaStavka>(
                poVrsti
                    .OrderByDescending(kv => kv.Value.Nab)
                    .Select(kv => new DashboardVrstaStavka(
                        kv.Key,
                        kv.Value.Br,
                        kv.Value.Nab,
                        kv.Value.Isp,
                        kv.Value.Sad)));

            DatumOsvezavanja = $"Osveženo: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
            Poruka = string.Empty;
        }
        catch (Exception ex)
        {
            Poruka = $"Greška: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }
}

public record DashboardVrstaStavka(
    string  Vrsta,
    int     Broj,
    decimal NabavnaVr,
    decimal Amortizacija,
    decimal Neotpisano);
