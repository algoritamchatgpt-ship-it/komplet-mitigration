using Algoritam.Application;
using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Dbf;
using Algoritam.WPF.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Fox forma LDSPIS - spiskovi za isplatu.
/// Funkcionalnost i redosled komandi su preneti iz LDSPIS.SCT.
/// </summary>
public partial class LdSpiskoviViewModel : ObservableObject
{
    private sealed class BankaInfo
    {
        public string Naziv { get; init; } = string.Empty;
        public string Mesto { get; init; } = string.Empty;
        public string Ziro { get; init; } = string.Empty;
    }

    private readonly AppState _appState;
    private Dictionary<string, BankaInfo> _bankePoSifri = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private ObservableCollection<LdSpisStavka> _stavke = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SpisakCommand))]
    private LdSpisStavka? _selektovana;

    [ObservableProperty]
    private int _mesec = DateTime.Now.Month;

    [ObservableProperty]
    private int _isplata = 1;

    [ObservableProperty]
    private string _poruka = string.Empty;

    [ObservableProperty]
    private string _statusBanke = string.Empty;

    [ObservableProperty]
    private bool _ucitava;

    public LdSpiskoviViewModel(AppState appState)
    {
        _appState = appState;
        _ = UcitajAsync();
    }

    public string Naslov => "SPISAK ZA ISPLATU";

    partial void OnSelektovanaChanged(LdSpisStavka? value)
    {
        OsveziStatusBanke();
    }

    [RelayCommand]
    private async Task UcitajAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Stavke = [];
            Poruka = "Folder aktivne firme nije pronađen.";
            StatusBanke = string.Empty;
            return;
        }

        Ucitava = true;
        try
        {
            var putanja = LdObracunDbfReader.PronadjiDbf(folder, "ldspis.dbf");
            if (putanja == null)
            {
                Stavke = [];
                Poruka = "ldspis.dbf nije pronađen u folderu firme.";
                StatusBanke = string.Empty;
                return;
            }

            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(putanja));
            _bankePoSifri = await Task.Run(() => UcitajBankeMapu(folder));

            var lista = zapisi.Select(z => new LdSpisStavka
            {
                Broj = Int(z, "BROJ"),
                ImePrez = Str(z, "IME_PREZ"),
                Partija = Str(z, "PARTIJA"),
                Iznos = Dec(z, "IZNOS"),
                Sifra = Str(z, "SIFRA"),
                Preneto = Str(z, "PRENETO"),
                Idbr = Long(z, "IDBR")
            }).ToList();

            Stavke = new ObservableCollection<LdSpisStavka>(lista);
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {Stavke.Count} stavki iz ldspis.dbf.";
            OsveziStatusBanke();
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri ucitavanju: {ex.Message}";
            StatusBanke = string.Empty;
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand]
    private void Dodaj()
    {
        var stavka = new LdSpisStavka();
        Stavke.Add(stavka);
        Selektovana = stavka;
        Poruka = "Dodat je prazan red (FOX DODAJ).";
    }

    [RelayCommand]
    private async Task ObrisiAsync()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Tabela je vec prazna.";
            return;
        }

        Stavke.Clear();
        Selektovana = null;
        await SacuvajAsync();
        Poruka = "Obrisane su sve stavke (FOX BRIŠI).";
        StatusBanke = string.Empty;
    }

    [RelayCommand]
    private async Task SacuvajAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var putanja = LdObracunDbfReader.PronadjiDbf(folder, "ldspis.dbf");
        if (putanja == null)
        {
            Poruka = "ldspis.dbf nije pronađen - nije moguće sačuvati.";
            return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(putanja);
            var redovi = Stavke.Select(s => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["BROJ"] = (decimal)s.Broj,
                ["IME_PREZ"] = (s.ImePrez ?? string.Empty).Trim(),
                ["PARTIJA"] = (s.Partija ?? string.Empty).Trim(),
                ["IZNOS"] = s.Iznos,
                ["SIFRA"] = (s.Sifra ?? string.Empty).Trim(),
                ["PRENETO"] = (s.Preneto ?? string.Empty).Trim(),
                ["IDBR"] = (decimal)s.Idbr
            }).ToList();

            await Task.Run(() => DbfTableWriter.WriteTable(
                putanja,
                schema,
                redovi,
                static (red, fieldName) => red.TryGetValue(fieldName, out var value) ? value : null));

            Poruka = $"Sačuvano {redovi.Count} stavki u ldspis.dbf.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri cuvanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PrenosAsync()
    {
        // Fox logika: PRENOS briše sve postojeće stavke, zatim puni iz LD i snima
        Stavke.Clear();
        Selektovana = null;
        StatusBanke = string.Empty;
        await PrenosIzLdAsync("ZAISPLATU", "Prenet neto iz LD (FOX PRENOS).");
    }

    [RelayCommand]
    private async Task PrenosPrevozAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var putanja = LdObracunDbfReader.PronadjiDbf(folder, "ldprev00.dbf")
                     ?? LdObracunDbfReader.PronadjiDbf(folder, "ldprev.dbf");
        if (putanja == null)
        {
            Poruka = "ldprev00.dbf/ldprev.dbf nije pronađen.";
            return;
        }

        await PrenosSaRelacijomAsync(
            putanja,
            "PREVOZ",
            "ldrad.dbf",
            "Prenet prevoz iz LDPREV00 (FOX PRENOS PREVOZA).");
    }

    [RelayCommand]
    private async Task PrenosIzOpj1Async()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var putanja = LdObracunDbfReader.PronadjiDbf(folder, "ldopj1n.dbf");
        if (putanja == null)
        {
            Poruka = "ldopj1n.dbf nije pronađen.";
            return;
        }

        await PrenosSaRelacijomAsync(
            putanja,
            "ZAISPLATU",
            "ldrad.dbf",
            "Prenos iz OPJ1 je zavrsen.");
    }

    [RelayCommand]
    private async Task PrenosIzOpj6Async()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var putanja = LdObracunDbfReader.PronadjiDbf(folder, "ldopj6.dbf");
        if (putanja == null)
        {
            Poruka = "ldopj6.dbf nije pronađen.";
            return;
        }

        await PrenosSaRelacijomAsync(
            putanja,
            "NETO",
            "ldrad0.dbf",
            "Prenos iz OPJ6 je zavrsen.");
    }

    [RelayCommand]
    private void Spiskovi()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema stavki za SPISKOVI.";
            return;
        }

        var redovi = Stavke
            .OrderBy(s => s.Broj)
            .Select(s => new PregledTabelaStavka
            {
                Sifra = string.IsNullOrWhiteSpace(s.Sifra) ? "-" : s.Sifra.Trim(),
                Naziv = $"{s.Broj} - {(string.IsNullOrWhiteSpace(s.ImePrez) ? "-" : s.ImePrez.Trim())}",
                Iznos1 = s.Iznos,
                Iznos2 = 0m
            })
            .ToList();

        OtvoriPregled("SPISKOVI ZA ISPLATU", "Svi redovi iz LDSPIS", redovi, "IZNOS", "REZERVA");
        Poruka = $"Otvoren SPISKOVI pregled: {redovi.Count} stavki.";
    }

    private bool MozeSpisak() => Selektovana != null;

    [RelayCommand(CanExecute = nameof(MozeSpisak))]
    private void Spisak()
    {
        if (Selektovana == null)
        {
            Poruka = "Izaberite red za SPISAK.";
            return;
        }

        var sifra = (Selektovana.Sifra ?? string.Empty).Trim();
        var filtrirane = Stavke
            .Where(s => string.Equals((s.Sifra ?? string.Empty).Trim(), sifra, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.Broj)
            .ToList();

        if (filtrirane.Count == 0)
        {
            Poruka = "Nema stavki za izabranu sifru.";
            return;
        }

        var redovi = filtrirane.Select(s => new PregledTabelaStavka
        {
            Sifra = string.IsNullOrWhiteSpace(s.Partija) ? "-" : s.Partija.Trim(),
            Naziv = $"{s.Broj} - {(string.IsNullOrWhiteSpace(s.ImePrez) ? "-" : s.ImePrez.Trim())}",
            Iznos1 = s.Iznos,
            Iznos2 = 0m
        }).ToList();

        var podnaslov = string.IsNullOrWhiteSpace(sifra)
            ? "SPISAK bez sifre banke"
            : $"SPISAK za sifru banke: {sifra}";

        OtvoriPregled("SPISAK ZA ISPLATU", podnaslov, redovi, "IZNOS", "REZERVA");
        Poruka = $"Otvoren SPISAK za sifru '{sifra}' ({redovi.Count} stavki).";
    }

    [RelayCommand]
    private void SaldoFirme()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema stavki za SALDO FIRME.";
            return;
        }

        var redovi = Stavke
            .GroupBy(s => (s.Sifra ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                _bankePoSifri.TryGetValue(g.Key, out var banka);
                var naziv = banka == null
                    ? "-"
                    : $"{banka.Naziv} {banka.Mesto}".Trim();

                var ziro = banka?.Ziro ?? string.Empty;
                var opis = string.IsNullOrWhiteSpace(ziro) ? naziv : $"{naziv} | {ziro}";

                return new PregledTabelaStavka
                {
                    Sifra = string.IsNullOrWhiteSpace(g.Key) ? "-" : g.Key,
                    Naziv = string.IsNullOrWhiteSpace(opis) ? "-" : opis,
                    Iznos1 = g.Sum(x => x.Iznos),
                    Iznos2 = 0m
                };
            })
            .ToList();

        OtvoriPregled(
            "SALDO FIRME",
            $"Saldo spiskova po sifri banke (mesec {Mesec:00}, isplata {Isplata})",
            redovi,
            "IZNOS",
            "REZERVA");

        Poruka = "Otvoren SALDO FIRME.";
    }

    [RelayCommand]
    private void Prvi() => SelektujPoIndeks(0);

    [RelayCommand]
    private void Gore()
    {
        if (Stavke.Count == 0)
            return;

        if (Selektovana == null)
        {
            SelektujPoIndeks(0);
            return;
        }

        var index = Stavke.IndexOf(Selektovana);
        SelektujPoIndeks(index <= 0 ? 0 : index - 1);
    }

    [RelayCommand]
    private void Dole()
    {
        if (Stavke.Count == 0)
            return;

        if (Selektovana == null)
        {
            SelektujPoIndeks(0);
            return;
        }

        var index = Stavke.IndexOf(Selektovana);
        SelektujPoIndeks(index < 0 ? 0 : Math.Min(Stavke.Count - 1, index + 1));
    }

    [RelayCommand]
    private void Zadnji() => SelektujPoIndeks(Stavke.Count - 1);

    [RelayCommand]
    private void IzvozTxtPostanska() => IzvozBankaTxt("POSTANSKA", "Poštanska štedionica");

    [RelayCommand]
    private void IzvozTxtIntesa() => IzvozBankaTxt("INTESA", "Banca Intesa");

    [RelayCommand]
    private void IzvozTxtUnikredit() => IzvozBankaTxt("UNIKREDIT", "UniCredit");

    [RelayCommand]
    private void IzvozTxtSociete() => IzvozBankaTxt("SOCIETE", "Société Générale");

    [RelayCommand]
    private void IzvozTxtVojvodjanska() => IzvozBankaTxt("VOJVODJANSKA", "Vojvođanska banka");

    [RelayCommand]
    private void IzvozTxtKomercijalna() => IzvozBankaTxt("KOMERCIJALNA", "Komercijalna banka");

    [RelayCommand]
    private void IzvozTxtFindomestic() => IzvozBankaTxt("FINDOMESTIC", "Findomestic");

    private void IzvozBankaTxt(string bankaKljuc, string bankaNazivPrikaz)
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema stavki u spisku.";
            return;
        }

        var sifra = _bankePoSifri
            .FirstOrDefault(kv => kv.Value.Naziv.Contains(bankaKljuc, StringComparison.OrdinalIgnoreCase))
            .Key;

        if (string.IsNullOrWhiteSpace(sifra))
            sifra = bankaKljuc;

        var stavke = Stavke
            .Where(s => string.Equals((s.Sifra ?? string.Empty).Trim(), sifra, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.Broj)
            .ToList();

        if (stavke.Count == 0)
        {
            MessageBox.Show(
                $"Nema stavki za banku '{bankaNazivPrikaz}' (šifra '{sifra}').",
                "Izvoz TXT", MessageBoxButton.OK, MessageBoxImage.Information);
            Poruka = $"Nema stavki za '{bankaNazivPrikaz}'.";
            return;
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = $"Sačuvaj TXT za {bankaNazivPrikaz}",
            FileName = $"SPIS_{bankaKljuc}_{DateTime.Now:yyyyMMdd}.TXT",
            Filter = "Tekstualni fajl (*.txt)|*.txt|Svi fajlovi (*.*)|*.*",
            DefaultExt = "txt"
        };

        if (dlg.ShowDialog() != true) return;

        var lines = stavke
            .Select(s => string.Format(
                CultureInfo.InvariantCulture,
                "{0};{1};{2}",
                (s.Partija ?? string.Empty).Trim(),
                s.Iznos.ToString("F2", CultureInfo.InvariantCulture),
                (s.ImePrez ?? string.Empty).Trim()))
            .ToList();

        File.WriteAllLines(dlg.FileName, lines, System.Text.Encoding.GetEncoding(1250));
        Poruka = $"TXT za {bankaNazivPrikaz} sačuvan: {stavke.Count} stavki → {Path.GetFileName(dlg.FileName)}";
    }

    private async Task PrenosIzLdAsync(string poljeIznosa, string poruka)
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var putanja = LdObracunDbfReader.PronadjiDbf(folder, "ld.dbf");
        if (putanja == null)
        {
            Poruka = "ld.dbf nije pronađen.";
            return;
        }

        try
        {
            Ucitava = true;
            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(putanja));
            var nove = zapisi
                .Where(ZadovoljavaMesecIsplatu)
                .Select(z => new LdSpisStavka
                {
                    Broj = Int(z, "BROJ"),
                    ImePrez = Str(z, "IME_PREZ"),
                    Partija = Str(z, "PARTIJA"),
                    Sifra = Str(z, "SIFRA"),
                    Iznos = Dec(z, poljeIznosa),
                    Idbr = Long(z, "IDBR")
                })
                .Where(s => s.Broj > 0 && s.Iznos != 0m)
                .ToList();

            DodajPreneteStavke(nove);
            await SacuvajAsync();
            Poruka = $"{poruka} Dodato: {nove.Count}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri prenosu: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    private async Task PrenosSaRelacijomAsync(
        string sourceTablePath,
        string amountField,
        string radnikDbfName,
        string poruka)
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        try
        {
            Ucitava = true;

            var source = await Task.Run(() => DbfReader.CitajSveZapise(sourceTablePath));
            var radnikPath = LdObracunDbfReader.PronadjiDbf(folder, radnikDbfName);
            var radnici = radnikPath == null
                ? []
                : await Task.Run(() => DbfReader.CitajSveZapise(radnikPath));
            var radByBroj = radnici
                .GroupBy(r => Int(r, "BROJ"))
                .ToDictionary(g => g.Key, g => g.First());

            var nove = new List<LdSpisStavka>();
            foreach (var red in source.Where(ZadovoljavaMesecIsplatu))
            {
                var broj = Int(red, "BROJ");
                var iznos = Dec(red, amountField);
                if (broj <= 0 || iznos == 0m)
                    continue;

                var rad = radByBroj.TryGetValue(broj, out var r) ? r : null;
                nove.Add(new LdSpisStavka
                {
                    Broj = broj,
                    ImePrez = rad == null ? Str(red, "IME_PREZ") : Str(rad, "IME_PREZ"),
                    Partija = rad == null ? Str(red, "PARTIJA") : Str(rad, "PARTIJA"),
                    Sifra = rad == null ? Str(red, "SIFRA") : Str(rad, "SIFRA"),
                    Iznos = iznos,
                    Idbr = Long(red, "IDBR")
                });
            }

            DodajPreneteStavke(nove);
            await SacuvajAsync();
            Poruka = $"{poruka} Dodato: {nove.Count}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri prenosu: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    private void DodajPreneteStavke(IEnumerable<LdSpisStavka> noveStavke)
    {
        LdSpisStavka? poslednja = null;
        foreach (var stavka in noveStavke)
        {
            Stavke.Add(stavka);
            poslednja = stavka;
        }

        if (poslednja != null)
            Selektovana = poslednja;
    }

    private bool ZadovoljavaMesecIsplatu(Dictionary<string, object?> zapis)
    {
        var m = IntNullable(zapis, "MESEC");
        var i = IntNullable(zapis, "ISPLATA");
        var mesecOk = m == null || m.Value == Mesec;
        var isplataOk = i == null || i.Value == Isplata;
        return mesecOk && isplataOk;
    }

    private void OtvoriPregled(
        string naslov,
        string podnaslov,
        IReadOnlyList<PregledTabelaStavka> redovi,
        string label1,
        string label2)
    {
        var view = new Views.Zarade.FoxPregledTabelaView(naslov, podnaslov, redovi.ToList(), label1, label2);
        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null)
            view.Owner = aktivniProzor;
        view.ShowDialog();
    }

    private void SelektujPoIndeks(int indeks)
    {
        if (Stavke.Count == 0)
        {
            Selektovana = null;
            return;
        }

        indeks = Math.Clamp(indeks, 0, Stavke.Count - 1);
        Selektovana = Stavke[indeks];
    }

    private void OsveziStatusBanke()
    {
        var sifra = (Selektovana?.Sifra ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(sifra))
        {
            StatusBanke = string.Empty;
            return;
        }

        if (_bankePoSifri.TryGetValue(sifra, out var banka))
        {
            var desno = $"{banka.Naziv} {banka.Mesto}".Trim();
            StatusBanke = string.IsNullOrWhiteSpace(desno) ? sifra : $"{sifra} {desno}";
            return;
        }

        StatusBanke = sifra;
    }

    private static Dictionary<string, BankaInfo> UcitajBankeMapu(string folder)
    {
        var map = new Dictionary<string, BankaInfo>(StringComparer.OrdinalIgnoreCase);
        var an0Path = LdObracunDbfReader.PronadjiDbf(folder, "an0.dbf");
        if (an0Path == null)
            return map;

        var redovi = DbfReader.CitajSveZapise(an0Path);
        foreach (var red in redovi)
        {
            var sifra = Str(red, "SIFRA");
            if (string.IsNullOrWhiteSpace(sifra))
                continue;

            var naziv = Str(red, "NAZIV");
            var mesto = Str(red, "MESTO");
            var ziro = Str(red, "ZIRO");
            if (string.IsNullOrWhiteSpace(ziro))
                ziro = Str(red, "TEKRAC");

            map[sifra] = new BankaInfo
            {
                Naziv = naziv,
                Mesto = mesto,
                Ziro = ziro
            };
        }

        return map;
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

    private static int? IntNullable(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return null;
        if (v is decimal d) return (int)d;
        if (v is int i) return i;
        if (int.TryParse(v.ToString(), out var parsed)) return parsed;
        return null;
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
        if (decimal.TryParse(
                v.ToString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsedInvariant))
            return parsedInvariant;
        if (decimal.TryParse(v.ToString(), out var parsedCurrent))
            return parsedCurrent;
        return 0m;
    }
}
