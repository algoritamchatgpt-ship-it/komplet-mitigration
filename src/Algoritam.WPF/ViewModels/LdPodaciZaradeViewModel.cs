using Algoritam.Application;
using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Dbf;
using Algoritam.WPF.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Fox forma LDPOD - zbirni podaci o platama. Čita direktno iz ldpod.dbf.
/// </summary>
public partial class LdPodaciZaradeViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly string _dbfFileName;
    private readonly string _naslov;
    private readonly string _foxReportPrefix;

    [ObservableProperty]
    private ObservableCollection<LdPodStavka> _stavke = [];

    [ObservableProperty]
    private string _poruka = string.Empty;

    [ObservableProperty]
    private bool _ucitava;

    public LdPodaciZaradeViewModel(AppState appState)
        : this(appState, "ldpod.dbf", "ZBIRNI PODACI O PLATAMA (LDPOD)", "LDPOD")
    {
    }

    public LdPodaciZaradeViewModel(AppState appState, string dbfFileName, string naslov, string foxReportPrefix)
    {
        _appState = appState;
        _dbfFileName = string.IsNullOrWhiteSpace(dbfFileName) ? "ldpod.dbf" : dbfFileName;
        _naslov = string.IsNullOrWhiteSpace(naslov) ? "ZBIRNI PODACI O PLATAMA (LDPOD)" : naslov;
        _foxReportPrefix = string.IsNullOrWhiteSpace(foxReportPrefix) ? "LDPOD" : foxReportPrefix;
        _ = UcitajAsync();
    }

    public string Naslov => _naslov;

    [RelayCommand]
    private async Task UcitajAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Stavke = [];
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        try
        {
            Ucitava = true;
            var putanja = LdObracunDbfReader.PronadjiDbf(folder, _dbfFileName);
            if (putanja == null)
            {
                Stavke = [];
                Poruka = $"{_dbfFileName} nije pronađen u folderu firme.";
                return;
            }

            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(putanja));
            var lista = zapisi.Select(MapirajZapis).ToList();
            for (var i = 0; i < lista.Count; i++)
            {
                if (lista[i].Numred <= 0)
                    lista[i].Numred = i + 1;
            }
            Stavke = new ObservableCollection<LdPodStavka>(lista);

            var ukupno = lista.Sum(x => x.Svu);
            Poruka = $"Ucitano {lista.Count} stavki. Ukupno SVU: {ukupno:N2}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri ucitavanju: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand]
    private void Izvestaj()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za izvestaj.";
            return;
        }

        var redovi = KreirajZbirniIzvestaj(Stavke.Take(62));
        OtvoriFoxIzvestaj($"{_foxReportPrefix} - IZVESTAJ", "RECNO() <= 62", redovi);
    }

    [RelayCommand]
    private void Izvestaj2()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za izvestaj 2.";
            return;
        }

        var redovi = KreirajZbirniIzvestaj(Stavke.Skip(62));
        OtvoriFoxIzvestaj($"{_foxReportPrefix} - IZVESTAJ 2", "RECNO() > 62", redovi);
    }

    private static List<PregledTabelaStavka> KreirajZbirniIzvestaj(IEnumerable<LdPodStavka> stavke)
    {
        return stavke
            .GroupBy(s => new { Kod = (s.Kod ?? string.Empty).Trim(), Opis = (s.Opis ?? string.Empty).Trim() })
            .OrderBy(g => g.Key.Kod)
            .ThenBy(g => g.Key.Opis)
            .Select(g => new PregledTabelaStavka
            {
                Sifra = string.IsNullOrWhiteSpace(g.Key.Kod) ? "-" : g.Key.Kod,
                Naziv = string.IsNullOrWhiteSpace(g.Key.Opis) ? "-" : g.Key.Opis,
                Iznos1 = g.Sum(x => x.Su),
                Iznos2 = g.Sum(x => x.Svu)
            })
            .Where(x => x.Iznos1 != 0m || x.Iznos2 != 0m)
            .ToList();
    }

    private void OtvoriFoxIzvestaj(string naslov, string tipIzvestaja, List<PregledTabelaStavka> redovi)
    {
        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za izabrani izvestaj.";
            return;
        }

        var podnaslov = $"{tipIzvestaja} | Stavki: {Stavke.Count}";

        var view = new Views.Zarade.FoxPregledTabelaView(
            naslov,
            podnaslov,
            redovi,
            "SU",
            "SVU");

        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null)
            view.Owner = aktivniProzor;

        view.ShowDialog();
        Poruka = $"Otvoren izvestaj: {naslov}.";
    }

    private static LdPodStavka MapirajZapis(Dictionary<string, object?> z)
    {
        return new LdPodStavka
        {
            Kod = Str(z, "KOD"),
            Opis = Str(z, "OPIS"),
            S1a = Dec(z, "S1A"),
            Sv1a = Dec(z, "SV1A"),
            S1b = Dec(z, "S1B"),
            Sv1b = Dec(z, "SV1B"),
            S1c = Dec(z, "S1C"),
            Sv1c = Dec(z, "SV1C"),
            S1u = Dec(z, "S1U"),
            Sv1u = Dec(z, "SV1U"),
            S2a = Dec(z, "S2A"),
            Sv2a = Dec(z, "SV2A"),
            S2b = Dec(z, "S2B"),
            Sv2b = Dec(z, "SV2B"),
            S2c = Dec(z, "S2C"),
            Sv2c = Dec(z, "SV2C"),
            S2u = Dec(z, "S2U"),
            Sv2u = Dec(z, "SV2U"),
            S3a = Dec(z, "S3A"),
            Sv3a = Dec(z, "SV3A"),
            S3b = Dec(z, "S3B"),
            Sv3b = Dec(z, "SV3B"),
            S3c = Dec(z, "S3C"),
            Sv3c = Dec(z, "SV3C"),
            S3u = Dec(z, "S3U"),
            Sv3u = Dec(z, "SV3U"),
            S4a = Dec(z, "S4A"),
            Sv4a = Dec(z, "SV4A"),
            S4b = Dec(z, "S4B"),
            Sv4b = Dec(z, "SV4B"),
            S4c = Dec(z, "S4C"),
            Sv4c = Dec(z, "SV4C"),
            S4u = Dec(z, "S4U"),
            Sv4u = Dec(z, "SV4U"),
            Su = Dec(z, "SU"),
            Svu = Dec(z, "SVU"),
            Mesec = Int(z, "MESEC"),
            Isplata = Int(z, "ISPLATA"),
            Vrsta = Str(z, "VRSTA"),
            Preneto = Str(z, "PRENETO"),
            Numred = Int(z, "NUMRED"),
            Idbr = Long(z, "IDBR")
        };
    }

    private static string Str(Dictionary<string, object?> z, string k)
        => z.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static int Int(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0;
        if (v is decimal d) return (int)d;
        if (v is int i) return i;
        if (int.TryParse(v.ToString(), out var parsed)) return parsed;
        return 0;
    }

    private static long Long(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0L;
        if (v is decimal d) return (long)d;
        if (v is long l) return l;
        if (long.TryParse(v.ToString(), out var parsed)) return parsed;
        return 0L;
    }

    private static decimal Dec(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0m;
        if (v is decimal d) return d;
        if (decimal.TryParse(v.ToString(), out var parsed)) return parsed;
        return 0m;
    }

    [RelayCommand]
    private Task OsveziAsync() => UcitajAsync();
}
