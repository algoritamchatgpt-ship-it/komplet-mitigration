using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za M4 — EVIDENCIJA OBRASCA M4.
/// Glavni DBF: ldizvn.dbf (jedan red po radniku po mesecu).
/// Podaci firme: ldm4pod.dbf (PIB, RegBroj itd.).
/// FoxPro ekvivalent: ldm4.scx + ldm4dodaj.scx.
/// </summary>
public partial class M4ViewModel : ObservableObject
{
    private readonly AppState _appState;

    [ObservableProperty] private ObservableCollection<M4Stavka> _stavke = [];
    [ObservableProperty] private M4Stavka?                      _selektovana;
    [ObservableProperty] private string _naslov = "EVIDENCIJA OBRASCA M4";
    [ObservableProperty] private string _poruka = "";

    // Podaci firme iz ldm4pod.dbf
    [ObservableProperty] private string _firmaPib       = "";
    [ObservableProperty] private string _firmaRegBroj   = "";
    [ObservableProperty] private string _firmaDelatnost = "";

    public M4ViewModel(AppState appState)
    {
        _appState = appState;
        Ucitaj();
    }

    // ── STARI KONSTRUKTOR ZA KOMPATIBILNOST (ZaradeMenuViewModel ga poziva sa folderPath) ──
    public M4ViewModel(string folderPath)
    {
        _appState = null!;
        _folderPath = folderPath;
        UcitajSaFolderPath(folderPath);
    }

    private string? _folderPath;
    private string FolderPath => _folderPath ?? _appState?.AktivnaFirma?.FolderPath ?? string.Empty;

    private void Ucitaj() => UcitajSaFolderPath(FolderPath);

    private void UcitajSaFolderPath(string folderPath)
    {
        Stavke.Clear();
        if (string.IsNullOrWhiteSpace(folderPath)) { Poruka = "Nije izabrana firma."; return; }

        // Podaci firme (ldm4pod.dbf)
        var podPath = PronadjiDbf(folderPath, "ldm4pod.dbf");
        if (podPath is not null)
        {
            var pod = DbfReader.CitajSveZapise(podPath).FirstOrDefault();
            if (pod is not null)
            {
                FirmaPib       = Str(pod, "PIB");
                FirmaRegBroj   = Str(pod, "REGBROJ");
                FirmaDelatnost = Str(pod, "DELATNOST");
            }
        }

        // Mesečni podaci (ldizvn.dbf)
        var dbfPath = PronadjiDbf(folderPath, "ldizvn.dbf");
        if (dbfPath is null) { Poruka = "ldizvn.dbf nije pronađen."; return; }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi)
                Stavke.Add(M4Stavka.IzZapisa(z));

            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Učitano {Stavke.Count} stavki iz ldizvn.dbf.";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void DatumiIsplata()
    {
        var folder = FolderPath;
        if (string.IsNullOrWhiteSpace(folder)) { Poruka = "Nije izabrana firma."; return; }
        var view = new Algoritam.WPF.Views.Zarade.LdM4DatView(folder);
        view.ShowDialog();
    }

    // ── DODAVANJE MESECA ─────────────────────────────────────────────────────
    // FoxPro: ldm4dodaj.scx — unesi broj meseca, čita LD{n}.DBF, puni ldizvn.dbf.

    [RelayCommand]
    private void DodajMesec()
    {
        var mesecStr = LdBrojUnos.Pitaj("Unesite broj meseca (1–24):", "Dodavanje meseca");
        if (!int.TryParse(mesecStr?.Trim(), out int mesec) || mesec < 1 || mesec > 24)
        {
            if (!string.IsNullOrWhiteSpace(mesecStr))
                Poruka = "Neispravan broj meseca.";
            return;
        }

        var folderPath = FolderPath;

        // Provjeri da li mesec već postoji
        if (Stavke.Any(s => s.Mesec == mesec))
        {
            Poruka = $"Mesec {mesec} već postoji u ldizvn.dbf.";
            return;
        }

        // Čitaj LD{n}.DBF (standardni obračun)
        var ldPath = PronadjiDbf(folderPath, $"ld{mesec:D2}.dbf")
                  ?? PronadjiDbf(folderPath, $"ld{mesec}.dbf");

        if (ldPath is null)
        {
            Poruka = $"Fajl LD{mesec:D2}.DBF nije pronađen.";
            return;
        }

        try
        {
            var noviRedi = new List<M4Stavka>();

            // LD obračun (redovna zarada)
            var ldZapisi = DbfReader.CitajSveZapise(ldPath);
            foreach (var z in ldZapisi)
            {
                var brutold   = DecZ(z, "BRUTO");
                var brutobol  = DecZ(z, "DINBOL") + DecZ(z, "DINBOL2") + DecZ(z, "DIN3BOL") + DecZ(z, "DIN4BOL");
                var pensoc    = DecZ(z, "DOPPR") + DecZ(z, "DOPPF");
                var casbol    = DecZ(z, "CASBOL") + DecZ(z, "CASBOL2") + DecZ(z, "CAS3BOL") + DecZ(z, "CAS4BOL");

                decimal penbol = brutold > 0 ? Math.Round(brutobol / brutold * pensoc, 2) : 0;

                noviRedi.Add(new M4Stavka
                {
                    Broj     = IntZ(z, "BROJ"),
                    Mesec    = mesec,
                    CasUk    = DecZ(z, "CASUK"),
                    CasBol   = casbol,
                    BrutoLd  = brutold,
                    BrutoM4  = brutold - brutobol,
                    PenSoc   = pensoc - penbol,
                    BrutoBol = brutobol,
                    PenBol   = penbol,
                    BenDop   = DecZ(z, "BENDIN"),
                    NazMes   = NazMesec(mesec),
                });
            }

            // LDP (porodiljsko) — samo bolovanje polje
            var ldpPath = PronadjiDbf(folderPath, $"ldp{mesec:D2}.dbf")
                       ?? PronadjiDbf(folderPath, $"ldp{mesec}.dbf");
            if (ldpPath is not null)
            {
                foreach (var z in DbfReader.CitajSveZapise(ldpPath))
                {
                    var brutobol = DecZ(z, "BRUTO");
                    var pensoc   = DecZ(z, "DOPPR") + DecZ(z, "DOPPF");
                    noviRedi.Add(new M4Stavka
                    {
                        Broj     = IntZ(z, "BROJ"),
                        Mesec    = mesec,
                        CasBol   = DecZ(z, "CASUK"),
                        BrutoBol = brutobol,
                        PenBol   = pensoc,
                        NazMes   = NazMesec(mesec),
                    });
                }
            }

            if (noviRedi.Count == 0) { Poruka = $"Nema podataka za mesec {mesec}."; return; }

            // Upiši u ldizvn.dbf
            var ldizvnPath = PronadjiDbf(folderPath, "ldizvn.dbf");
            if (ldizvnPath is null) { Poruka = "ldizvn.dbf nije pronađen za upis."; return; }

            var schema = DbfTableWriter.LoadSchema(ldizvnPath);
            var novi = noviRedi.Select(s => s.UZapis()).ToList();
            DbfTableWriter.DodajRedove(ldizvnPath, schema, novi);

            foreach (var s in noviRedi) Stavke.Add(s);
            Poruka = $"Dodato {noviRedi.Count} stavki za mesec {mesec}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri dodavanju meseca: {ex.Message}";
        }
    }

    // ── BRISANJE MESECA ──────────────────────────────────────────────────────

    [RelayCommand]
    private void BrisiMesec()
    {
        if (Selektovana is null) { Poruka = "Izaberite red za brisanje."; return; }

        int mesec = Selektovana.Mesec;
        var r = MessageBox.Show($"Obrisati sve zapise za mesec {mesec}?",
            "Brisanje meseca", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;

        var ldizvnPath = PronadjiDbf(FolderPath, "ldizvn.dbf");
        if (ldizvnPath is null) { Poruka = "ldizvn.dbf nije pronađen."; return; }

        try
        {
            // Označi za brisanje u DBF (postavi deleted flag)
            var zapisi = DbfReader.CitajSveZapise(ldizvnPath);
            var indeksiZaBrisanje = new List<int>();
            for (int i = 0; i < zapisi.Count; i++)
            {
                if (IntZ(zapisi[i], "MESEC") == mesec)
                    indeksiZaBrisanje.Add(i);
            }

            DbfTableWriter.OznaciBrisanjePoIndeksima(ldizvnPath, indeksiZaBrisanje);

            int pre = Stavke.Count;
            for (int i = Stavke.Count - 1; i >= 0; i--)
            {
                if (Stavke[i].Mesec == mesec)
                    Stavke.RemoveAt(i);
            }
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Obrisano {pre - Stavke.Count} stavki za mesec {mesec}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri brisanju: {ex.Message}";
        }
    }

    // ── Navigacija ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Dole()
    {
        if (Stavke.Count == 0) return;
        var idx = Selektovana is null ? -1 : Stavke.IndexOf(Selektovana);
        Selektovana = idx < Stavke.Count - 1 ? Stavke[idx + 1] : Stavke[^1];
    }

    [RelayCommand]
    private void Gore()
    {
        if (Stavke.Count == 0) return;
        var idx = Selektovana is null ? Stavke.Count : Stavke.IndexOf(Selektovana);
        Selektovana = idx > 0 ? Stavke[idx - 1] : Stavke[0];
    }

    [RelayCommand]
    private void Zadnji()
    {
        if (Stavke.Count > 0) Selektovana = Stavke[^1];
    }

    [RelayCommand]
    private void Prvi()
    {
        if (Stavke.Count > 0) Selektovana = Stavke[0];
    }

    // ── DODAVANJE REDA ───────────────────────────────────────────────────────
    // Fox: row-level append u ldizvn.dbf za jedan radnik

    [RelayCommand]
    private void DodajRed()
    {
        var ldizvnPath = PronadjiDbf(FolderPath, "ldizvn.dbf");
        if (ldizvnPath is null) { Poruka = "ldizvn.dbf nije pronađen."; return; }

        var mesecStr = LdBrojUnos.Pitaj("Unesite broj radnika i mesec (npr: 5/3 = radnik 5, mesec 3):", "Dodavanje reda");
        if (string.IsNullOrWhiteSpace(mesecStr)) return;

        var delovi = mesecStr.Split('/');
        if (!int.TryParse(delovi[0].Trim(), out int broj) || !(delovi.Length > 1 && int.TryParse(delovi[1].Trim(), out int mesec)))
        {
            Poruka = "Neispravan unos. Format: <broj_radnika>/<broj_meseca>";
            return;
        }

        try
        {
            var novi = new M4Stavka { Broj = broj, Mesec = mesec, NazMes = NazMesec(mesec) };
            var schema = DbfTableWriter.LoadSchema(ldizvnPath);
            DbfTableWriter.DodajRedove(ldizvnPath, schema, [novi.UZapis()]);
            Stavke.Add(novi);
            Selektovana = novi;
            Poruka = $"Dodat red: radnik {broj}, mesec {mesec}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri dodavanju reda: {ex.Message}";
        }
    }

    // ── OSNOVICE (pregled LDOSN) ─────────────────────────────────────────────
    // Fox: USE LDOSN IN 0 EXCLU → DO FORM PREGLED

    [RelayCommand]
    private void Osnovice()
    {
        var folder = FolderPath;
        var ldosnPath = PronadjiDbf(folder, "ldosn.dbf");
        if (ldosnPath is null)
        {
            Poruka = "Fajl ldosn.dbf nije pronađen.";
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(ldosnPath);
            var stavke = zapisi.Select(z => new Algoritam.WPF.Models.PregledTabelaStavka
            {
                Sifra  = Str(z, "MESEC") is { Length: > 0 } m ? m : IntZ(z, "BROJ").ToString(),
                Naziv  = Str(z, "NAZMES").Length > 0 ? Str(z, "NAZMES") : Str(z, "OPIS"),
                Iznos1 = DecZ(z, "BRUTOM4"),
                Iznos2 = DecZ(z, "PENSOC"),
            }).ToList();

            if (stavke.Count == 0) { Poruka = "ldosn.dbf je prazna."; return; }

            var view = new Views.Zarade.FoxPregledTabelaView(
                "OSNOVICE — LDOSN",
                $"{Path.GetFileName(ldosnPath)} | {stavke.Count} zapisa",
                stavke, "BRUTO M4", "PIO");
            view.ShowDialog();
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri otvaranju LDOSN: {ex.Message}";
        }
    }

    // ── M-4K NOVI (pregled / štampa) ─────────────────────────────────────────

    [RelayCommand]
    private void M4Novi()
    {
        if (Stavke.Count == 0) { Poruka = "Nema podataka za štampu."; return; }

        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;

        var fd = GradeM4FlowDocument();
        var pag = ((IDocumentPaginatorSource)fd).DocumentPaginator;
        pag.PageSize = new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);
        dlg.PrintDocument(pag, "M4 — Evidencija obrasca");
        Poruka = "M4 poslat na štampu.";
    }

    private FlowDocument GradeM4FlowDocument()
    {
        var fd = new FlowDocument
        {
            FontFamily  = new FontFamily("Courier New"),
            FontSize    = 9,
            PagePadding = new Thickness(40, 30, 40, 30),
            ColumnWidth = double.PositiveInfinity,
        };

        fd.Blocks.Add(new Paragraph(new Bold(new Run("EVIDENCIJA ZA OBRAZAC M4")))
        { FontSize = 13, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 4) });

        var firma = _appState?.AktivnaFirma;
        fd.Blocks.Add(new Paragraph(new Run(
            $"Firma: {firma?.Naziv ?? ""}    PIB: {FirmaPib}    RegBroj: {FirmaRegBroj}"))
        { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 10) });

        var header = new Paragraph(new Bold(new Run(
            $"{"BROJ",-6}{"MES",-5}{"MESEC",-14}{"ČAS.UK",-10}{"ČAS.BOL",-10}" +
            $"{"BRUTO.LD",-12}{"BRUTO.M4",-12}{"PIO",-10}{"BRUTO.BOL",-12}{"PIO.BOL",-10}{"BEN.DOP",-10}")))
        { Margin = new Thickness(0, 0, 0, 2) };
        fd.Blocks.Add(header);

        fd.Blocks.Add(new BlockUIContainer(new System.Windows.Shapes.Line
        {
            X1 = 0, X2 = 700, StrokeThickness = 1,
            Stroke = System.Windows.Media.Brushes.Black,
        }));

        foreach (var g in Stavke.GroupBy(s => s.Mesec).OrderBy(g => g.Key))
        {
            foreach (var s in g)
            {
                fd.Blocks.Add(new Paragraph(new Run(
                    $"{s.Broj,-6}{s.Mesec,-5}{(s.NazMes ?? ""),-14}{s.CasUk,-10:N0}{s.CasBol,-10:N0}" +
                    $"{s.BrutoLd,-12:N2}{s.BrutoM4,-12:N2}{s.PenSoc,-10:N2}{s.BrutoBol,-12:N2}{s.PenBol,-10:N2}{s.BenDop,-10:N2}"))
                { Margin = new Thickness(0, 0, 0, 0) });
            }
        }
        return fd;
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────

    private static string? PronadjiDbf(string folderPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folderPath)) return null;
        var direct = Path.Combine(folderPath, fileName);
        if (File.Exists(direct)) return direct;
        var ci = Directory.GetFiles(folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));
        return ci;
    }

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : "";

    private static int IntZ(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0;
        if (v is decimal d) return (int)d;
        if (v is int i) return i;
        return 0;
    }

    private static decimal DecZ(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;

    private static string NazMesec(int mesec)
    {
        var nazivi = new[] { "", "Januar", "Februar", "Mart", "April", "Maj", "Jun",
            "Jul", "Avgust", "Septembar", "Oktobar", "Novembar", "Decembar" };
        int idx = ((mesec - 1) % 12) + 1;
        return idx >= 1 && idx <= 12 ? nazivi[idx] : mesec.ToString();
    }
}

// ── MODEL ──────────────────────────────────────────────────────────────────────

public class M4Stavka
{
    public int     Broj     { get; set; }
    public int     Mesec    { get; set; }
    public string? NazMes   { get; set; }
    public decimal CasUk    { get; set; }
    public decimal CasBol   { get; set; }
    public decimal BrutoLd  { get; set; }
    public decimal BrutoM4  { get; set; }
    public decimal PenSoc   { get; set; }
    public decimal BrutoBol { get; set; }
    public decimal PenBol   { get; set; }
    public decimal BenDop   { get; set; }
    public decimal BrutoOsn { get; set; }
    public string? MaticniBr { get; set; }

    public static M4Stavka IzZapisa(Dictionary<string, object?> z) => new()
    {
        Broj      = IntZ(z, "BROJ"),
        Mesec     = IntZ(z, "MESEC"),
        NazMes    = StrZ(z, "NAZMES"),
        CasUk     = DecZ(z, "CASUK"),
        CasBol    = DecZ(z, "CASBOL"),
        BrutoLd   = DecZ(z, "BRUTOLD"),
        BrutoM4   = DecZ(z, "BRUTOM4"),
        PenSoc    = DecZ(z, "PENSOC"),
        BrutoBol  = DecZ(z, "BRUTOBOL"),
        PenBol    = DecZ(z, "PENBOL"),
        BenDop    = DecZ(z, "BENDOP"),
        MaticniBr = StrZ(z, "MATICNIBR"),
    };

    public Dictionary<string, object?> UZapis() => new()
    {
        ["BROJ"]     = (decimal)Broj,
        ["MESEC"]    = (decimal)Mesec,
        ["NAZMES"]   = NazMes ?? "",
        ["CASUK"]    = CasUk,
        ["CASBOL"]   = CasBol,
        ["BRUTOLD"]  = BrutoLd,
        ["BRUTOM4"]  = BrutoM4,
        ["PENSOC"]   = PenSoc,
        ["BRUTOBOL"] = BrutoBol,
        ["PENBOL"]   = PenBol,
        ["BENDOP"]   = BenDop,
    };

    private static int IntZ(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0;
        if (v is decimal d) return (int)d;
        if (v is int i) return i;
        return 0;
    }
    private static decimal DecZ(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;
    private static string? StrZ(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : null;
}
