using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class ZdravstveneKnjiziceViewModel : ObservableObject
{
    private readonly string _folderPath;
    private List<ZdravstvenaKnjizicaStavka> _svi = [];

    [ObservableProperty] private ObservableCollection<ZdravstvenaKnjizicaStavka> _stavke = [];
    [ObservableProperty] private ZdravstvenaKnjizicaStavka? _selektovana;
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private string _pretraga = "";

    public string Naslov => "ZDRAVSTVENE KNJIŽICE — PREGLED OSIGURANJA";

    public ZdravstveneKnjiziceViewModel(string folderPath)
    {
        _folderPath = folderPath;
        Ucitaj();
    }

    partial void OnPretragaChanged(string value) => Filtriraj(value);

    [RelayCommand]
    private void Osvezi()
    {
        Pretraga = "";
        Ucitaj();
    }

    // ── Članovi porodice (LDRADCL) ────────────────────────────────────────────
    // Fox: DO FORM LDRADCL → pregled članova porodice za izabranog radnika

    [RelayCommand]
    private void ClanPorodice()
    {
        var dbfPath = Path.Combine(_folderPath, "ldradcl.dbf");
        if (!File.Exists(dbfPath))
            dbfPath = Directory.GetFiles(_folderPath, "ldradcl.dbf", SearchOption.TopDirectoryOnly)
                               .FirstOrDefault() ?? dbfPath;

        if (!File.Exists(dbfPath))
        {
            Poruka = "ldradcl.dbf nije pronađen.";
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);

            // Filtriraj po izabranom radniku ako je izabran
            int? filterBroj = Selektovana?.Broj;
            if (filterBroj.HasValue)
                zapisi = zapisi.Where(z => IntZ(z, "BROJ") == filterBroj.Value).ToList();

            var stavke = zapisi.Select(z => new Algoritam.WPF.Models.PregledTabelaStavka
            {
                Sifra  = Str(z, "CMATICNI"),
                Naziv  = Str(z, "CIME_PREZ"),
                Iznos1 = 0,
                Iznos2 = 0,
            }).ToList();

            var naslov = filterBroj.HasValue
                ? $"ČLANOVI PORODICE — {Selektovana?.ImePrez ?? filterBroj.ToString()}"
                : "ČLANOVI PORODICE — SVI RADNICI";

            var view = new Views.Zarade.FoxPregledTabelaView(
                naslov,
                $"ldradcl.dbf | {stavke.Count} članova",
                stavke, "LBO", "Srodstvo");
            view.ShowDialog();
        }
        catch (Exception ex)
        {
            Poruka = $"Greška: {ex.Message}";
        }
    }

    // ── ZZO Zahtev za markice ─────────────────────────────────────────────────
    // Fox: LDZZO010 = baza (tabela za unos), LDZZO01 = tekst (pregled za štampu)

    [RelayCommand]
    private void ZahtevMarkiceBaza()
    {
        var vm   = new GkDbfPregledViewModel(_folderPath, "ldzzo01.dbf", "ZZO MARKICE — BAZA ZAHTEVA", "#1B5E20", "#E8F5E9");
        var view = new Views.GlavnaKnjiga.GkDbfPregledView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    [RelayCommand]
    private void ZahtevMarkice()
    {
        OtvoriTekstFormu(LdTekstIzjavaViewModel.CreateZzoMarkice(_folderPath));
    }

    // ── Ovlašćenje ────────────────────────────────────────────────────────────
    // Fox: LDZZO020 = baza (tabela za unos), LDZZO02 = tekst (pregled za štampu)

    [RelayCommand]
    private void OvlascenjeBaza()
    {
        var vm   = new GkDbfPregledViewModel(_folderPath, "ldzzo02.dbf", "OVLAŠĆENJE — BAZA", "#1B5E20", "#E8F5E9");
        var view = new Views.GlavnaKnjiga.GkDbfPregledView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    [RelayCommand]
    private void OvlascenjeTekst()
    {
        OtvoriTekstFormu(LdTekstIzjavaViewModel.CreateZzoOvlascenje(_folderPath));
    }

    private static void OtvoriTekstFormu(LdTekstIzjavaViewModel vm)
    {
        var view = new Views.Zarade.LdTekstIzjavaView { DataContext = vm };
        view.ShowDialog();
    }

    private void Ucitaj()
    {
        _svi.Clear();
        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var dbfPath = Path.Combine(_folderPath, "ldrad.dbf");
        if (!File.Exists(dbfPath))
            dbfPath = Directory.GetFiles(_folderPath, "ldrad.dbf", SearchOption.TopDirectoryOnly)
                               .FirstOrDefault() ?? dbfPath;

        if (!File.Exists(dbfPath))
        {
            Poruka = $"ldrad.dbf nije pronađen u: {_folderPath}";
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi.OrderBy(z => Int(z, "BROJ")))
            {
                _svi.Add(new ZdravstvenaKnjizicaStavka
                {
                    Broj           = Int(z, "BROJ"),
                    ImePrez        = Str(z, "IME_PREZ"),
                    Maticnibr      = Str(z, "MATICNIBR"),
                    LboBroj        = Str(z, "LBOBROJ"),
                    ZkBroj         = Str(z, "ZKBROJ"),
                    DatumOd        = Dat(z, "DATOSIG0"),
                    DatumDo        = Dat(z, "DATOSIG1"),
                    OsnovOsiguranja = Str(z, "OSNOVOSIG"),
                });
            }

            Filtriraj(Pretraga);
            Poruka = $"Učitano {_svi.Count} radnika.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška: {ex.Message}";
        }
    }

    private void Filtriraj(string tekst)
    {
        Stavke.Clear();
        var term = tekst.Trim().ToUpperInvariant();

        var filtrirani = string.IsNullOrEmpty(term)
            ? _svi
            : _svi.Where(s =>
                s.ImePrez.ToUpperInvariant().Contains(term) ||
                s.LboBroj.Contains(term) ||
                s.ZkBroj.Contains(term) ||
                s.Maticnibr.Contains(term) ||
                s.Broj.ToString().Contains(term));

        foreach (var s in filtrirani)
            Stavke.Add(s);
    }

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static int IntZ(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0;
        return v switch { decimal d => (int)d, int i => i, long l => (int)l, _ => 0 };
    }

    private static int Int(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0;
        return v switch { decimal d => (int)d, int i => i, long l => (int)l, _ => 0 };
    }

    private static DateTime? Dat(Dictionary<string, object?> r, string k)
    {
        if (r.TryGetValue(k, out var v) && v is DateTime d && d != DateTime.MinValue)
            return d;
        return null;
    }
}

public class ZdravstvenaKnjizicaStavka
{
    public int      Broj            { get; set; }
    public string   ImePrez         { get; set; } = string.Empty;
    public string   Maticnibr       { get; set; } = string.Empty;
    public string   LboBroj         { get; set; } = string.Empty;
    public string   ZkBroj          { get; set; } = string.Empty;
    public DateTime? DatumOd        { get; set; }
    public DateTime? DatumDo        { get; set; }
    public string   OsnovOsiguranja { get; set; } = string.Empty;
}
