using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// LDPREV - Evidencija prevoza (FoxPro: DO FORM LDPREV).
/// </summary>
public partial class LdPrevozViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly bool _arhivaRezim;
    private DbfOptimisticConcurrency.FileSnapshot? _snapshot;

    [ObservableProperty] private ObservableCollection<LdPrevozStavka> _stavke = [];
    [ObservableProperty] private LdPrevozStavka? _selektovana;
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private string _naslov = "EVIDENCIJA PREVOZA";
    [ObservableProperty] private string _pretragaIme = string.Empty;
    [ObservableProperty] private string _pretragaSifra = string.Empty;

    public LdPrevozViewModel(string folderPath, bool arhivaRezim = false)
    {
        _folderPath = folderPath;
        _arhivaRezim = arhivaRezim;
        Naslov = arhivaRezim ? "ARHIVA PREVOZA" : "EVIDENCIJA PREVOZA";
        Ucitaj();
    }

    partial void OnSelektovanaChanged(LdPrevozStavka? value)
    {
        if (value == null) return;

        var prefix = _arhivaRezim ? "ARHIVA — PREVOZ ZA" : "PREVOZ ZA";
        Naslov = $"{prefix} {value.Mesec:00} {value.Nazmes}".TrimEnd();
        Poruka = $"Kolona: {value.LastTouchedField}";
    }

    [RelayCommand]
    private void Prvi()
    {
        if (Stavke.Count == 0) return;
        Selektovana = Stavke[0];
    }

    [RelayCommand]
    private void Prethodni()
    {
        if (Selektovana == null || Stavke.Count == 0) return;
        var idx = Stavke.IndexOf(Selektovana);
        if (idx > 0) Selektovana = Stavke[idx - 1];
    }

    [RelayCommand]
    private void Sledeci()
    {
        if (Selektovana == null || Stavke.Count == 0) return;
        var idx = Stavke.IndexOf(Selektovana);
        if (idx < Stavke.Count - 1) Selektovana = Stavke[idx + 1];
    }

    [RelayCommand]
    private void Poslednji()
    {
        if (Stavke.Count == 0) return;
        Selektovana = Stavke[^1];
    }

    [RelayCommand]
    private void Dodaj()
    {
        var nova = new LdPrevozStavka
        {
            Mesec = TrenutniMesec(),
            Nazmes = TrenutniNazivMeseca(),
            Datum = DateTime.Today,
            Stopa = 10m,
            Arhiva = " ",
        };
        nova.Recalculate();

        Stavke.Add(nova);
        Renum();
        Selektovana = nova;
        Poruka = "Dodat je novi zapis u prevoz tabelu (u memoriji).";
    }

    [RelayCommand]
    private void BrisanjeJednog()
    {
        if (Selektovana == null) return;

        Stavke.Remove(Selektovana);
        Renum();
        Selektovana = Stavke.FirstOrDefault();
        Poruka = "Obrisan je selektovani zapis.";
    }

    [RelayCommand]
    private void BrisiSve()
    {
        Stavke.Clear();
        Poruka = "Obrisani su svi zapisi (u memoriji).";
    }

    [RelayCommand]
    private void TraziIme()
    {
        if (string.IsNullOrWhiteSpace(PretragaIme))
        {
            return;
        }

        var upit = PretragaIme.Trim();
        var found = Stavke.FirstOrDefault(s =>
            s.ImePrez.Contains(upit, StringComparison.OrdinalIgnoreCase));

        if (found != null)
        {
            Selektovana = found;
            Poruka = $"Pronađen po imenu: {found.ImePrez}";
            return;
        }

        Poruka = "Nije pronađen zapis za trazeno ime.";
    }

    [RelayCommand]
    private void TraziSifru()
    {
        if (string.IsNullOrWhiteSpace(PretragaSifra))
        {
            return;
        }

        var upit = PretragaSifra.Trim();
        var found = Stavke.FirstOrDefault(s =>
            s.Broj.ToString(CultureInfo.InvariantCulture) == upit ||
            s.Sifraprih.Equals(upit, StringComparison.OrdinalIgnoreCase));

        if (found != null)
        {
            Selektovana = found;
            Poruka = $"Pronađen zapis za sifru/broj: {upit}";
            return;
        }

        Poruka = "Nije pronađen zapis za trazenu sifru.";
    }

    [RelayCommand]
    private void PreuzmiIzSpiska()
    {
        if (Stavke.Count > 0)
        {
            Poruka = "Preuzimanje iz spiska radi samo kada je tabela prazna (kao u Fox-u).";
            return;
        }

        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var ldradPath = Path.Combine(_folderPath, "ldrad.dbf");
        if (!File.Exists(ldradPath))
        {
            Poruka = "ldrad.dbf nije pronađen.";
            return;
        }

        int mesec = TrenutniMesec();
        string nazmes = TrenutniNazivMeseca();

        var zapisi = DbfReader.CitajSveZapise(ldradPath);
        var brojDodatih = 0;

        foreach (var z in zapisi)
        {
            var prevozRaw = Str(z, "PREVOZ");
            if (string.IsNullOrWhiteSpace(prevozRaw))
            {
                continue;
            }

            var stavka = new LdPrevozStavka
            {
                Broj = Int(z, "BROJ"),
                ImePrez = Str(z, "IME_PREZ"),
                Dana = 1m,
                Karta = ParseDecimal(prevozRaw),
                Neoporez = 0m,
                Stopa = 10m,
                Datum = DateTime.Today,
                Mesec = mesec,
                Nazmes = nazmes,
                Arhiva = " ",
            };
            stavka.Recalculate();

            Stavke.Add(stavka);
            brojDodatih++;
        }

        Renum();
        Selektovana = Stavke.FirstOrDefault();
        Poruka = $"Preuzeto iz spiska: {brojDodatih} zapisa.";
    }

    [RelayCommand]
    private void Popunjavanje()
    {
        var vm = new LdPrevParamViewModel
        {
            Mesec = TrenutniMesec(),
            Nazmes = TrenutniNazivMeseca(),
            Datum = DateTime.Today,
        };
        var dlg = new Algoritam.WPF.Views.Zarade.LdPrevParamView { DataContext = vm };
        vm.ZatvaranjeZahtevano += () => dlg.Close();
        dlg.ShowDialog();

        if (!vm.Potvrdjeno) return;

        foreach (var s in Stavke)
        {
            s.Dana = vm.Dana;
            s.Karta = vm.Karta;
            s.Neoporez = vm.Neoporez;
            s.Datum = vm.Datum;
            s.Mesec = vm.Mesec;
            s.Nazmes = vm.Nazmes;
        }

        Poruka = $"Popunjeno {Stavke.Count} zapisa (Dana={vm.Dana}, Karta={vm.Karta:N2}, Neoporez={vm.Neoporez:N2}).";
    }

    [RelayCommand]
    private void ArhivaDearhiva()
    {
        if (Selektovana == null)
        {
            return;
        }

        Selektovana.Arhiva = Selektovana.Arhiva == "*" ? " " : "*";
        Poruka = Selektovana.Arhiva == "*"
            ? "Zapis je oznacen kao arhiva."
            : "Zapis je vracen iz arhive.";
    }

    [RelayCommand]
    private void SifraPrihoda()
    {
        var tekuca = Stavke.FirstOrDefault()?.Sifraprih;
        var vm = new LdPrevSifraViewModel
        {
            Sifraprih = string.IsNullOrWhiteSpace(tekuca) ? "101110000" : tekuca,
        };
        var dlg = new Algoritam.WPF.Views.Zarade.LdPrevSifraView { DataContext = vm };
        vm.ZatvaranjeZahtevano += () => dlg.Close();
        dlg.ShowDialog();

        if (!vm.Potvrdjeno) return;

        foreach (var s in Stavke)
            s.Sifraprih = vm.Sifraprih;

        Poruka = $"Šifra prihoda '{vm.Sifraprih}' upisana u {Stavke.Count} zapisa.";
    }

    [RelayCommand]
    private void PregledSve()
    {
        var ukupno = Stavke.Sum(x => x.Ukupprev);
        Poruka = $"Pregled: zapisa {Stavke.Count}, ukupno prevoz {ukupno:N2}.";
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        if (Stavke.Count == 0) { Poruka = "Nema podataka za čuvanje."; return; }
        if (string.IsNullOrWhiteSpace(_folderPath)) { Poruka = "Nije izabrana firma."; return; }

        string dbfPath = Path.Combine(_folderPath, "ldprev00.dbf");
        if (!File.Exists(dbfPath))
            dbfPath = Path.Combine(_folderPath, "ldprev.dbf");
        if (!File.Exists(dbfPath)) { Poruka = "ldprev DBF nije pronađen — ne mogu da sačuvam."; return; }

        if (_snapshot != null && DbfOptimisticConcurrency.HasFileChanged(dbfPath, _snapshot))
        {
            var r = System.Windows.MessageBox.Show(
                $"Fajl {Path.GetFileName(dbfPath)} je izmenjen od strane drugog korisnika.\nNastaviti sa čuvanjem (prepisati)?",
                "Upozorenje — dual korisnici", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (r != System.Windows.MessageBoxResult.Yes) return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(dbfPath);
            var rows = Stavke.Select(s => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["BROJ"]      = (decimal)s.Broj,
                ["IME_PREZ"]  = s.ImePrez,
                ["DANA"]      = s.Dana,
                ["KARTA"]     = s.Karta,
                ["PREVOZ"]    = s.Prevoz,
                ["NEOPOREZ"]  = s.Neoporez,
                ["OPOREZ"]    = s.Oporez,
                ["STOPA"]     = s.Stopa,
                ["POREZ"]     = s.Porez,
                ["UKUPPREV"]  = s.Ukupprev,
                ["DATUM"]     = s.Datum,
                ["GRUPA"]     = (decimal)s.Grupa,
                ["MESEC"]     = (decimal)s.Mesec,
                ["ISPLATA"]   = (decimal)s.Isplata,
                ["VRSTA"]     = s.Vrsta,
                ["NAZMES"]    = s.Nazmes,
                ["SIFRAPRIH"] = s.Sifraprih,
                ["ARHIVA"]    = s.Arhiva,
                ["ARHIVA2"]   = s.Arhiva2,
                ["PRENETO"]   = s.Preneto,
                ["IDBR"]      = (decimal)s.Idbr,
            }).ToList<Dictionary<string, object?>>();

            DbfTableWriter.WriteTable(
                dbfPath,
                schema,
                rows,
                static (row, field) => row.TryGetValue(field, out var v) ? v : null);

            _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(dbfPath);
            Poruka = $"Sačuvano {Stavke.Count} zapisa u {Path.GetFileName(dbfPath)}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čuvanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za export.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var path = Path.Combine(_folderPath, $"ldprev_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("BROJ;IME_PREZ;DANA;KARTA;PREVOZ;NEOPOREZ;OPOREZ;STOPA;POREZ;UKUPPREV;DATUM;GRUPA;MESEC;ISPLATA;VRSTA;NAZMES;SIFRAPRIH;ARHIVA;ARHIVA2;PRENETO;NUMRED;IDBR");

        foreach (var s in Stavke)
        {
            sb.AppendLine(string.Join(";",
                s.Broj.ToString(CultureInfo.InvariantCulture),
                Csv(s.ImePrez),
                D(s.Dana),
                D(s.Karta),
                D(s.Prevoz),
                D(s.Neoporez),
                D(s.Oporez),
                D(s.Stopa),
                D(s.Porez),
                D(s.Ukupprev),
                s.Datum.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                s.Grupa.ToString(CultureInfo.InvariantCulture),
                s.Mesec.ToString(CultureInfo.InvariantCulture),
                s.Isplata.ToString(CultureInfo.InvariantCulture),
                Csv(s.Vrsta),
                Csv(s.Nazmes),
                Csv(s.Sifraprih),
                Csv(s.Arhiva),
                Csv(s.Arhiva2),
                Csv(s.Preneto),
                s.Numred.ToString(CultureInfo.InvariantCulture),
                s.Idbr.ToString(CultureInfo.InvariantCulture)));
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        Poruka = $"Export zavrsen: {path}";
    }

    private void Ucitaj()
    {
        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        string dbfPath = Path.Combine(_folderPath, "ldprev00.dbf");
        if (!File.Exists(dbfPath))
        {
            dbfPath = Path.Combine(_folderPath, "ldprev.dbf");
        }

        if (!File.Exists(dbfPath))
        {
            Poruka = "ldprev00.dbf/ldprev.dbf nije pronađen.";
            return;
        }

        _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(dbfPath);

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);

            foreach (var z in zapisi)
            {
                var arhiva = Str(z, "ARHIVA");
                bool jeArhiviran = arhiva.Trim() == "*";

                if (_arhivaRezim && !jeArhiviran) continue;
                if (!_arhivaRezim && jeArhiviran) continue;

                var s = new LdPrevozStavka
                {
                    Broj = Int(z, "BROJ"),
                    ImePrez = Str(z, "IME_PREZ"),
                    Dana = Dec(z, "DANA"),
                    Karta = Dec(z, "KARTA"),
                    Prevoz = Dec(z, "PREVOZ"),
                    Neoporez = Dec(z, "NEOPOREZ"),
                    Oporez = Dec(z, "OPOREZ"),
                    Stopa = Dec(z, "STOPA"),
                    Porez = Dec(z, "POREZ"),
                    Ukupprev = Dec(z, "UKUPPREV"),
                    Datum = Dat(z, "DATUM"),
                    Grupa = Int(z, "GRUPA"),
                    Mesec = Int(z, "MESEC"),
                    Isplata = Int(z, "ISPLATA"),
                    Vrsta = Str(z, "VRSTA"),
                    Nazmes = Str(z, "NAZMES"),
                    Sifraprih = Str(z, "SIFRAPRIH"),
                    Arhiva = arhiva,
                    Arhiva2 = Str(z, "ARHIVA2"),
                    Preneto = Str(z, "PRENETO"),
                    Idbr = Long(z, "IDBR"),
                };
                s.Recalculate();
                Stavke.Add(s);
            }

            Renum();
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {Stavke.Count} zapisa iz {Path.GetFileName(dbfPath)}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri ucitavanju: {ex.Message}";
        }
    }

    private void Renum()
    {
        for (int i = 0; i < Stavke.Count; i++)
        {
            Stavke[i].Numred = i + 1;
        }
    }

    private int TrenutniMesec()
    {
        var p = ProcitajLdParam();
        return p?.Item1 ?? DateTime.Today.Month;
    }

    private string TrenutniNazivMeseca()
    {
        var p = ProcitajLdParam();
        return string.IsNullOrWhiteSpace(p?.Item2)
            ? DateTime.Today.ToString("MMMM", CultureInfo.InvariantCulture).ToUpperInvariant()
            : p!.Value.Item2!;
    }

    private (int mesec, string? nazmes)? ProcitajLdParam()
    {
        var put = Path.Combine(_folderPath, "ldparam.dbf");
        if (!File.Exists(put))
        {
            return null;
        }

        var zapisi = DbfReader.CitajSveZapise(put);
        var prvi = zapisi.FirstOrDefault();
        if (prvi == null)
        {
            return null;
        }

        return (Int(prvi, "MESEC"), Str(prvi, "NAZMES"));
    }

    private static string Csv(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace(";", ",").Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static string D(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static int Int(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? (int)d : 0;

    private static long Long(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? (long)d : 0L;

    private static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;

    private static DateTime Dat(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is DateTime dt ? dt : DateTime.Today;

    private static decimal ParseDecimal(string value)
    {
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var dInv))
        {
            return dInv;
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var dCur))
        {
            return dCur;
        }

        return 0m;
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();
}

public partial class LdPrevozStavka : ObservableObject
{
    [ObservableProperty] private int _broj;
    [ObservableProperty] private string _imePrez = string.Empty;
    [ObservableProperty] private decimal _dana;
    [ObservableProperty] private decimal _karta;
    [ObservableProperty] private decimal _prevoz;
    [ObservableProperty] private decimal _neoporez;
    [ObservableProperty] private decimal _oporez;
    [ObservableProperty] private decimal _stopa;
    [ObservableProperty] private decimal _porez;
    [ObservableProperty] private decimal _ukupprev;
    [ObservableProperty] private DateTime _datum = DateTime.Today;
    [ObservableProperty] private int _grupa;
    [ObservableProperty] private int _mesec;
    [ObservableProperty] private int _isplata;
    [ObservableProperty] private string _vrsta = string.Empty;
    [ObservableProperty] private string _nazmes = string.Empty;
    [ObservableProperty] private string _sifraprih = string.Empty;
    [ObservableProperty] private string _arhiva = string.Empty;
    [ObservableProperty] private string _arhiva2 = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private int _numred;
    [ObservableProperty] private long _idbr;
    [ObservableProperty] private string _lastTouchedField = string.Empty;

    partial void OnDanaChanged(decimal value)
    {
        LastTouchedField = "DANA";
        Recalculate();
    }

    partial void OnKartaChanged(decimal value)
    {
        LastTouchedField = "KARTA";
        Recalculate();
    }

    partial void OnNeoporezChanged(decimal value)
    {
        LastTouchedField = "NEOPOREZ";
        Recalculate();
    }

    partial void OnStopaChanged(decimal value)
    {
        LastTouchedField = "STOPA";
        Recalculate();
    }

    public void Recalculate()
    {
        var stopaLokal = Stopa == 0m ? 10m : Stopa;
        if (Stopa == 0m)
        {
            Stopa = stopaLokal;
        }

        Prevoz = Math.Round(Karta * Dana, 2);
        Oporez = Math.Round(Prevoz - Neoporez, 2);
        Porez = stopaLokal == 0m
            ? 0m
            : Math.Round(Oporez * stopaLokal / (100m - stopaLokal), 2);
        Ukupprev = Math.Round(Prevoz + Porez, 2);
    }
}
