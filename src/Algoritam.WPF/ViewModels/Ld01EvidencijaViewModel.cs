using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

public partial class Ld01EvidencijaViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _dbfPath;

    [ObservableProperty] private ObservableCollection<Ld0Stavka> _stavke = [];
    [ObservableProperty] private Ld0Stavka? _selektovanaStavka;
    [ObservableProperty] private string _naslov = "";
    [ObservableProperty] private string _poruka = "";

    // Top info bar (FoxPro: txtVrsta, txtMesec, txtNazmes, txtGodina, txtDat1)
    [ObservableProperty] private string _infoVrsta = "";
    [ObservableProperty] private string _infoMesec = "";
    [ObservableProperty] private string _infoNazmes = "";
    [ObservableProperty] private string _infoGodina = "";
    [ObservableProperty] private string _infoDat1 = "";

    public Action? ZatvoriAction { get; set; }

    private readonly string _firmaNaziv;
    private readonly string _firmaMesto;

    public Ld01EvidencijaViewModel(
        string folderPath,
        string dbfName    = "ld0.dbf",
        string naslov     = "EVIDENCIJA SVIH OBRAČUNA",
        string firmaNaziv = "",
        string firmaMesto = "")
    {
        _folderPath = folderPath;
        _dbfPath    = Path.Combine(folderPath, dbfName);
        _firmaNaziv = firmaNaziv;
        _firmaMesto = firmaMesto;
        Naslov      = naslov;
        UcitajPodatke();
    }

    partial void OnSelektovanaStavkaChanged(Ld0Stavka? value)
    {
        InfoVrsta  = value?.Vrsta ?? "";
        InfoMesec  = value?.Mesec > 0 ? value.Mesec.ToString() : "";
        InfoNazmes = value?.Nazmes ?? "";
        InfoGodina = value?.Godina ?? "";
        InfoDat1   = value?.Dat1?.ToString("dd.MM.yyyy") ?? "";
    }

    private void UcitajPodatke()
    {
        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        if (!File.Exists(_dbfPath))
        {
            Poruka = $"Fajl nije pronađen: {_dbfPath}";
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(_dbfPath);
            foreach (var z in zapisi)
                Stavke.Add(MapZapis(z));

            Poruka = zapisi.Count == 0 ? "Nema podataka." : $"Učitano {zapisi.Count} stavki.";
            if (Stavke.Count > 0) SelektovanaStavka = Stavke[0];
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čitanju: {ex.Message}";
        }
    }

    private static Ld0Stavka MapZapis(Dictionary<string, object?> z) => new()
    {
        Brobrac   = Int(z, "BROBRAC"),
        Mesec     = Int(z, "MESEC"),
        Nazmes    = Str(z, "NAZMES"),
        Vrsta     = Str(z, "VRSTA"),
        Casuk     = Dec(z, "CASUK"),
        Casbol    = Dec(z, "CASBOL"),
        Bruto     = Dec(z, "BRUTO"),
        Porez     = Dec(z, "POREZ"),
        Dopsocr   = Dec(z, "DOPSOCR"),
        Doppr     = Dec(z, "DOPPR"),
        Dopzr     = Dec(z, "DOPZR"),
        Dopnr     = Dec(z, "DOPNR"),
        Dopsocf   = Dec(z, "DOPSOCF"),
        Doppf     = Dec(z, "DOPPF"),
        Dopzf     = Dec(z, "DOPZF"),
        Dopnf     = Dec(z, "DOPNF"),
        Neto      = Dec(z, "NETO"),
        Ukobust   = Dec(z, "UKOBUST"),
        Zaisplatu = Dec(z, "ZAISPLATU"),
        Cenarada  = Dec(z, "CENARADA"),
        Dat1      = Date(z, "DAT1"),
        Dat2      = Date(z, "DAT2"),
        Dat3      = Date(z, "DAT3"),
        Dat4      = Date(z, "DAT4"),
        Godina    = Str(z, "GODINA"),
        Ppopj1    = Str(z, "PPOPJ1"),
        Ppopj2    = Str(z, "PPOPJ2"),
        Ppopj3    = Str(z, "PPOPJ3"),
        Ppopj4    = Str(z, "PPOPJ4"),
        Ppod01    = Str(z, "PPOD01"),
        Ppod02    = Str(z, "PPOD02"),
        Ppod03    = Str(z, "PPOD03"),
        Ppod04    = Str(z, "PPOD04"),
        Ppod01v   = Str(z, "PPOD01V"),
        Ppod02v   = Str(z, "PPOD02V"),
        Ppod03v   = Str(z, "PPOD03V"),
        Ppod04v   = Str(z, "PPOD04V"),
        Ppod11    = Str(z, "PPOD11"),
        Ppod12    = Str(z, "PPOD12"),
        Ppod13    = Str(z, "PPOD13"),
        Ppod14    = Str(z, "PPOD14"),
        Opis1     = Str(z, "OPIS1"),
        Opis2     = Str(z, "OPIS2"),
        Preneto   = Str(z, "PRENETO"),
        Idbr      = Long(z, "IDBR"),
    };

    [RelayCommand]
    private void Osvezi() => UcitajPodatke();

    [RelayCommand]
    private void ObrisiRed()
    {
        if (SelektovanaStavka == null)
        {
            Poruka = "Izaberite red za brisanje.";
            return;
        }

        var zaBrisanje = SelektovanaStavka;
        var potvrda = MessageBox.Show(
            $"Obrisati izabrani red (mesec {zaBrisanje.Mesec}, godina {zaBrisanje.Godina}, vrsta {zaBrisanje.Vrsta})?",
            "Brisanje reda",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (potvrda != MessageBoxResult.Yes)
            return;

        var idx = Stavke.IndexOf(zaBrisanje);
        if (idx < 0)
            return;

        Stavke.RemoveAt(idx);
        SelektovanaStavka = Stavke.Count == 0
            ? null
            : Stavke[Math.Min(idx, Stavke.Count - 1)];

        ZapisiNaDbf();
        Poruka = "Red je obrisan.";
    }

    // ── Navigacija (FoxPro: DOLE, GORE, ZADNJI, PRVI) ──────────────────────

    [RelayCommand]
    private void Dole()
    {
        if (Stavke.Count == 0) return;
        if (SelektovanaStavka == null) { SelektovanaStavka = Stavke[0]; return; }
        int idx = Stavke.IndexOf(SelektovanaStavka);
        if (idx < Stavke.Count - 1) SelektovanaStavka = Stavke[idx + 1];
    }

    [RelayCommand]
    private void Gore()
    {
        if (Stavke.Count == 0) return;
        if (SelektovanaStavka == null) { SelektovanaStavka = Stavke[0]; return; }
        int idx = Stavke.IndexOf(SelektovanaStavka);
        if (idx > 0) SelektovanaStavka = Stavke[idx - 1];
    }

    [RelayCommand]
    private void Zadnji()
    {
        if (Stavke.Count > 0) SelektovanaStavka = Stavke[^1];
    }

    [RelayCommand]
    private void Prvi()
    {
        if (Stavke.Count > 0) SelektovanaStavka = Stavke[0];
    }

    // ── PREUZMI OBRAČUNE F8 ─────────────────────────────────────────────────
    // FoxPro: čita ld.dbf, grupiše po VRSTA+NAZMES za svaki MESEC i ISPLATA,
    // sabira sve numeričke kolone, i dodaje zbirne zapise u ld0.dbf.
    [RelayCommand]
    private void Preuzmi()
    {
        var ldPath = Path.Combine(_folderPath, "ld.dbf");
        if (!File.Exists(ldPath))
        {
            Poruka = "Fajl ld.dbf nije pronađen u folderu firme.";
            return;
        }

        try
        {
            var ldZapisi = DbfReader.CitajSveZapise(ldPath);
            var novo = new List<Ld0Stavka>();

            for (int i = 1; i <= 99; i++)
            {
                foreach (int ispl in new[] { 1, 2 })
                {
                    var filtered = ldZapisi
                        .Where(r => Int(r, "MESEC") == i && Int(r, "ISPLATA") == ispl)
                        .ToList();

                    if (filtered.Count == 0) continue;

                    // FoxPro: TOTAL ON VRSTA+NAZMES — jedan zapis po grupi
                    foreach (var grp in filtered.GroupBy(r => Str(r, "VRSTA") + Str(r, "NAZMES")))
                    {
                        var first = grp.First();

                        // FoxPro: REPLACE ALL CASBOL WITH CASBOL+CASBOL2+CAS3BOL+CAS4BOL
                        var casbolTot = grp.Sum(r => Dec(r, "CASBOL"))
                                      + grp.Sum(r => Dec(r, "CASBOL2"))
                                      + grp.Sum(r => Dec(r, "CAS3BOL"))
                                      + grp.Sum(r => Dec(r, "CAS4BOL"));

                        // FoxPro: REPLACE ALL CASUK WITH CASUK-CASBOL
                        var casuk = grp.Sum(r => Dec(r, "CASUK")) - casbolTot;

                        novo.Add(new Ld0Stavka
                        {
                            Brobrac   = i,
                            Mesec     = Int(first, "MESEC"),
                            Nazmes    = Str(first, "NAZMES"),
                            Vrsta     = "A",   // FoxPro: REPLACE ALL VRSTA WITH 'A'
                            Casuk     = casuk,
                            Casbol    = casbolTot,
                            Bruto     = grp.Sum(r => Dec(r, "BRUTO")),
                            Porez     = grp.Sum(r => Dec(r, "POREZ")),
                            Dopsocr   = grp.Sum(r => Dec(r, "DOPSOCR")),
                            Doppr     = grp.Sum(r => Dec(r, "DOPPR")),
                            Dopzr     = grp.Sum(r => Dec(r, "DOPZR")),
                            Dopnr     = grp.Sum(r => Dec(r, "DOPNR")),
                            Dopsocf   = grp.Sum(r => Dec(r, "DOPSOCF")),
                            Doppf     = grp.Sum(r => Dec(r, "DOPPF")),
                            Dopzf     = grp.Sum(r => Dec(r, "DOPZF")),
                            Dopnf     = grp.Sum(r => Dec(r, "DOPNF")),
                            Neto      = grp.Sum(r => Dec(r, "NETO")),
                            Ukobust   = grp.Sum(r => Dec(r, "UKOBUST")),
                            Zaisplatu = grp.Sum(r => Dec(r, "ZAISPLATU")),
                            Cenarada  = Dec(first, "CENARADA"),
                            Dat1      = Date(first, "DAT1"),
                            Dat2      = Date(first, "DAT2"),
                            Dat3      = Date(first, "DAT3"),
                            Dat4      = Date(first, "DAT4"),
                            Godina    = Str(first, "GODINA"),
                            Opis1     = Str(first, "OPIS1"),
                            Opis2     = Str(first, "OPIS2"),
                        });
                    }
                }
            }

            foreach (var s in novo) Stavke.Add(s);
            ZapisiNaDbf();

            Poruka = novo.Count > 0
                ? $"Preuzeto {novo.Count} stavki."
                : "Nema novih podataka za preuzimanje.";

            if (SelektovanaStavka == null && Stavke.Count > 0)
                SelektovanaStavka = Stavke[0];
        }
        catch (Exception ex)
        {
            Poruka = $"Greška PREUZMI: {ex.Message}";
        }
    }

    // ── BRISANJE TABELE ─────────────────────────────────────────────────────
    // FoxPro: SELECT LD0; DELETE ALL; PACK; THISFORM.Release
    [RelayCommand]
    private void BrisanjeTabele()
    {
        if (MessageBox.Show("Obrisati sve podatke iz tabele?", "BRISANJE TABELE",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        Stavke.Clear();
        SelektovanaStavka = null;
        ZapisiNaDbf();
        Poruka = "Tabela je obrisana.";
        ZatvoriAction?.Invoke();  // FoxPro: THISFORM.Release
    }

    // ── PREGLED OBRAČUNA F10 (FoxPro: REPORT FORM LD010 PREVIEW) ───────────
    [RelayCommand]
    private void PregledObracuna()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za pregled.";
            return;
        }
        var view = new Algoritam.WPF.Views.Zarade.Ld010PregledObracunaView(
            Stavke, _firmaNaziv, _firmaMesto);
        view.Show();
    }

    // ── PREGLED PORESKIH PRIJAVA (FoxPro: REPORT FORM LD011 PREVIEW) ────────
    [RelayCommand]
    private void PregledPoreskihPrijava()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za pregled.";
            return;
        }
        var view = new Algoritam.WPF.Views.Zarade.Ld011PregledPoreskihPrijavaView(
            Stavke, _firmaNaziv, _firmaMesto);
        view.Show();
    }

    // ── Pisanje u DBF (za PREUZMI i BRISANJE) ──────────────────────────────
    private void ZapisiNaDbf()
    {
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            DbfTableWriter.WriteTable(_dbfPath, schema, Stavke.ToList(), (s, f) => f.ToUpperInvariant() switch
            {
                "BROBRAC"   => (object?)s.Brobrac,
                "MESEC"     => s.Mesec,
                "NAZMES"    => s.Nazmes,
                "VRSTA"     => s.Vrsta,
                "CASUK"     => s.Casuk,
                "CASBOL"    => s.Casbol,
                "BRUTO"     => s.Bruto,
                "POREZ"     => s.Porez,
                "DOPSOCR"   => s.Dopsocr,
                "DOPPR"     => s.Doppr,
                "DOPZR"     => s.Dopzr,
                "DOPNR"     => s.Dopnr,
                "DOPSOCF"   => s.Dopsocf,
                "DOPPF"     => s.Doppf,
                "DOPZF"     => s.Dopzf,
                "DOPNF"     => s.Dopnf,
                "NETO"      => s.Neto,
                "UKOBUST"   => s.Ukobust,
                "ZAISPLATU" => s.Zaisplatu,
                "CENARADA"  => s.Cenarada,
                "DAT1"      => (object?)(s.Dat1 is DateTime d1 ? d1 : (object?)""),
                "DAT2"      => (object?)(s.Dat2 is DateTime d2 ? d2 : (object?)""),
                "DAT3"      => (object?)(s.Dat3 is DateTime d3 ? d3 : (object?)""),
                "DAT4"      => (object?)(s.Dat4 is DateTime d4 ? d4 : (object?)""),
                "GODINA"    => s.Godina,
                "PPOPJ1"    => s.Ppopj1,
                "PPOPJ2"    => s.Ppopj2,
                "PPOPJ3"    => s.Ppopj3,
                "PPOPJ4"    => s.Ppopj4,
                "PPOD01"    => s.Ppod01,
                "PPOD02"    => s.Ppod02,
                "PPOD03"    => s.Ppod03,
                "PPOD04"    => s.Ppod04,
                "PPOD01V"   => s.Ppod01v,
                "PPOD02V"   => s.Ppod02v,
                "PPOD03V"   => s.Ppod03v,
                "PPOD04V"   => s.Ppod04v,
                "PPOD11"    => s.Ppod11,
                "PPOD12"    => s.Ppod12,
                "PPOD13"    => s.Ppod13,
                "PPOD14"    => s.Ppod14,
                "OPIS1"     => s.Opis1,
                "OPIS2"     => s.Opis2,
                "PRENETO"   => s.Preneto,
                "IDBR"      => s.Idbr,
                _           => null
            });
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri pisanju: {ex.Message}";
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s : string.Empty;
    private static int Int(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? (int)d : 0;
    private static long Long(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? (long)d : 0L;
    private static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;
    private static DateTime? Date(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is DateTime d ? d : (DateTime?)null;
}

public class Ld0Stavka
{
    public int      Brobrac   { get; set; }
    public int      Mesec     { get; set; }
    public string   Nazmes    { get; set; } = string.Empty;
    public string   Vrsta     { get; set; } = string.Empty;
    public decimal  Casuk     { get; set; }
    public decimal  Casbol    { get; set; }
    public decimal  Bruto     { get; set; }
    public decimal  Porez     { get; set; }
    public decimal  Dopsocr   { get; set; }
    public decimal  Doppr     { get; set; }
    public decimal  Dopzr     { get; set; }
    public decimal  Dopnr     { get; set; }
    public decimal  Dopsocf   { get; set; }
    public decimal  Doppf     { get; set; }
    public decimal  Dopzf     { get; set; }
    public decimal  Dopnf     { get; set; }
    public decimal  Neto      { get; set; }
    public decimal  Ukobust   { get; set; }
    public decimal  Zaisplatu { get; set; }
    public decimal  Cenarada  { get; set; }
    public DateTime? Dat1     { get; set; }
    public DateTime? Dat2     { get; set; }
    public DateTime? Dat3     { get; set; }
    public DateTime? Dat4     { get; set; }
    public string   Godina    { get; set; } = string.Empty;
    public string   Ppopj1    { get; set; } = string.Empty;
    public string   Ppopj2    { get; set; } = string.Empty;
    public string   Ppopj3    { get; set; } = string.Empty;
    public string   Ppopj4    { get; set; } = string.Empty;
    public string   Ppod01    { get; set; } = string.Empty;
    public string   Ppod02    { get; set; } = string.Empty;
    public string   Ppod03    { get; set; } = string.Empty;
    public string   Ppod04    { get; set; } = string.Empty;
    public string   Ppod01v   { get; set; } = string.Empty;
    public string   Ppod02v   { get; set; } = string.Empty;
    public string   Ppod03v   { get; set; } = string.Empty;
    public string   Ppod04v   { get; set; } = string.Empty;
    public string   Ppod11    { get; set; } = string.Empty;
    public string   Ppod12    { get; set; } = string.Empty;
    public string   Ppod13    { get; set; } = string.Empty;
    public string   Ppod14    { get; set; } = string.Empty;
    public string   Opis1     { get; set; } = string.Empty;
    public string   Opis2     { get; set; } = string.Empty;
    public string   Preneto   { get; set; } = string.Empty;
    public long     Idbr      { get; set; }
}
