using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za obrazac Gradovi/Mesta — čita mesta.dbf iz data00 foldera FIN instalacije.
/// Originalni FoxPro poziv: DO FORM LDMESTA
/// IBAZE otvara mesta.dbf pod aliasom LDMESTA.
/// </summary>
public partial class GradoviViewModel : ObservableObject
{
    private const string PodrazumevanaPunaMestaTabela = @"C:\FINFORTUNAkockatestotpremnice\data00\MESTA.dbf";

    private static readonly HashSet<string> MestaEkstenzije = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dbf",
        ".cdx",
        ".fpt",
        ".dbt"
    };

    private readonly string _finRootFolder;
    private readonly string _firmaFolderPath;
    private List<GradoviStavka> _sveStavke = [];

    [ObservableProperty] private ObservableCollection<GradoviStavka> _stavke = [];
    [ObservableProperty] private GradoviStavka? _selektovana;
    [ObservableProperty] private string _naslov = "MESTA";
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private string _pretragaMesta = string.Empty;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PopuniCommand))]
    [NotifyCanExecuteChangedFor(nameof(IsprazniCommand))]
    private bool _ucitava = true;

    public GradoviViewModel(string finRootFolder, string? firmaFolderPath = null)
    {
        _finRootFolder = finRootFolder;
        _firmaFolderPath = firmaFolderPath ?? string.Empty;
        UcitajPodatke();
    }

    partial void OnPretragaMestaChanged(string value) => PrimeniFilter();

    private bool MozePopuni() => !Ucitava;
    private bool MozeIsprazni() => !Ucitava;

    [RelayCommand(CanExecute = nameof(MozeIsprazni))]
    private void Isprazni()
    {
        var targetDbf = OdrediCiljnuMestaTabelu();
        if (targetDbf is null)
        {
            Poruka = "Nije pronađena ciljna tabela MESTA za aktivnu firmu.";
            return;
        }

        try
        {
            IsprazniDbfSadrzaj(targetDbf);
            ObrisiIndeksAkoPostoji(targetDbf);
            UcitajPodatke();
            Poruka = $"Tabela je ispraznjena: {targetDbf}";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri praznjenju MESTA: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(MozePopuni))]
    private void Popuni()
    {
        var targetDbf = OdrediCiljnuMestaTabelu();
        if (targetDbf is null)
        {
            Poruka = "Nije pronađena ciljna tabela MESTA za aktivnu firmu.";
            return;
        }

        try
        {
            var postojeci = DbfReader.CitajSveZapise(targetDbf);
            if (postojeci.Count > 0)
            {
                Poruka = $"Tabela nije prazna ({postojeci.Count} zapisa). Prvo klikni ISPRAZNI.";
                return;
            }

            var sourceDbf = PronadjiPunuMestaTabelu(targetDbf);
            if (sourceDbf is null)
            {
                Poruka = $"Puna tabela MESTA nije pronađena. Ocekivano: {PodrazumevanaPunaMestaTabela}";
                return;
            }

            KopirajMestaDatoteke(sourceDbf, targetDbf);
            UcitajPodatke();
            Poruka = $"Tabela je popunjena iz: {sourceDbf}";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri popunjavanju MESTA: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OcistiPretragu() => PretragaMesta = string.Empty;

    private void UcitajPodatke()
    {
        Ucitava = true;
        _sveStavke = [];
        Stavke = [];

        if (string.IsNullOrWhiteSpace(_finRootFolder))
        {
            Poruka = "Nije izabrana firma.";
            Ucitava = false;
            return;
        }

        var dbfPath = OdrediCiljnuMestaTabelu();

        if (dbfPath is null)
        {
            Poruka = "Tabela MESTA za aktivnu firmu nije pronađena.";
            Ucitava = false;
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);

            if (zapisi.Count == 0)
            {
                Poruka = "Tabela MESTA je prazna.";
            }
            else
            {
                foreach (var z in zapisi)
                {
                    _sveStavke.Add(new GradoviStavka
                    {
                        Mp       = Str(z, "MP"),
                        Posta    = Str(z, "POSTA"),
                        Mesto    = Str(z, "MESTO"),
                        Ziro1    = Str(z, "ZIRO1"),
                        Ziro2    = Str(z, "ZIRO2"),
                        PorBroj  = Str(z, "PORBROJ"),
                        PorBrojP = Str(z, "PORBROJP"),
                        RegSoc   = Str(z, "REGSOC"),
                        Por      = Dec(z, "POR"),
                        Zdr      = Dec(z, "ZDR"),
                        Pio      = Dec(z, "PIO"),
                        Nez      = Dec(z, "NEZ"),
                        Vrsta    = Str(z, "VRSTA"),
                        Mesec    = Int(z, "MESEC"),
                        Isplata  = Int(z, "ISPLATA"),
                        Preneto  = Str(z, "PRENETO"),
                    });
                }
                PrimeniFilter();
            }
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri citanju: {ex.Message}";
        }

        Ucitava = false;
    }

    private void PrimeniFilter()
    {
        var upit = (PretragaMesta ?? string.Empty).Trim();

        IEnumerable<GradoviStavka> filtrirano = _sveStavke;
        if (!string.IsNullOrWhiteSpace(upit))
        {
            filtrirano = _sveStavke.Where(s =>
                s.Mesto.Contains(upit, StringComparison.OrdinalIgnoreCase));
        }

        Stavke = new ObservableCollection<GradoviStavka>(filtrirano);

        Poruka = string.IsNullOrWhiteSpace(upit)
            ? $"Ucitano {_sveStavke.Count} mesta."
            : $"Pronađeno {Stavke.Count} mesta za \"{upit}\".";
    }

    private string? OdrediCiljnuMestaTabelu()
    {
        if (!string.IsNullOrWhiteSpace(_firmaFolderPath))
        {
            var izFirme = PronadjiMestaDbfUFolderu(_firmaFolderPath);
            if (izFirme is not null)
                return izFirme;
        }

        var izRoota = PronadjiMestaDbfUFolderu(_finRootFolder);
        if (izRoota is not null)
            return izRoota;

        return PronadjiMestaDbf(_finRootFolder);
    }

    private string? PronadjiPunuMestaTabelu(string targetDbfPath)
    {
        var candidates = new List<string>
        {
            PodrazumevanaPunaMestaTabela,
            Path.Combine(_finRootFolder, "data00", "mesta.dbf"),
            Path.Combine(_finRootFolder, "DATA00", "MESTA.DBF"),
            Path.Combine(_finRootFolder, "TXT", "MESTA.DBF")
        };

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
                continue;

            if (string.Equals(candidate, targetDbfPath, StringComparison.OrdinalIgnoreCase))
                continue;

            return candidate;
        }

        return null;
    }

    private static void KopirajMestaDatoteke(string sourceDbfPath, string targetDbfPath)
    {
        var sourceDir = Path.GetDirectoryName(sourceDbfPath) ?? string.Empty;
        var sourceBase = Path.GetFileNameWithoutExtension(sourceDbfPath);
        var targetDir = Path.GetDirectoryName(targetDbfPath) ?? string.Empty;
        var targetBase = Path.GetFileNameWithoutExtension(targetDbfPath);

        foreach (var ext in MestaEkstenzije)
        {
            var sourceFile = Path.Combine(sourceDir, sourceBase + ext);
            if (!File.Exists(sourceFile))
                continue;

            var targetFile = Path.Combine(targetDir, targetBase + ext);
            Directory.CreateDirectory(targetDir);
            File.Copy(sourceFile, targetFile, overwrite: true);
        }
    }

    private static void IsprazniDbfSadrzaj(string dbfPath)
    {
        using var fs = new FileStream(dbfPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        Span<byte> header = stackalloc byte[32];
        fs.ReadExactly(header);

        var headerLength = header[8] | (header[9] << 8);
        if (headerLength < 33)
            throw new InvalidOperationException("Neispravan DBF header.");

        var now = DateTime.Now;
        header[1] = (byte)(now.Year - 1900);
        header[2] = (byte)now.Month;
        header[3] = (byte)now.Day;
        header[4] = 0;
        header[5] = 0;
        header[6] = 0;
        header[7] = 0;

        fs.Position = 0;
        fs.Write(header);
        fs.SetLength(headerLength + 1L);
        fs.Position = headerLength;
        fs.WriteByte(0x1A);
    }

    private static void ObrisiIndeksAkoPostoji(string dbfPath)
    {
        var cdxPath = Path.ChangeExtension(dbfPath, ".cdx");
        if (File.Exists(cdxPath))
            File.Delete(cdxPath);
    }

    private static string? PronadjiMestaDbfUFolderu(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return null;

        return Directory.GetFiles(folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => string.Equals(Path.GetFileName(f), "mesta.dbf", StringComparison.OrdinalIgnoreCase));
    }

    private static string? PronadjiMestaDbf(string finRootFolder)
    {
        var kandidati = new[]
        {
            Path.Combine(finRootFolder, "data00", "mesta.dbf"),
            Path.Combine(finRootFolder, "DATA00", "MESTA.DBF"),
            Path.Combine(finRootFolder, "mesta.dbf"),
        };

        foreach (var kandidat in kandidati)
        {
            if (File.Exists(kandidat))
                return kandidat;
        }

        var data00 = Path.Combine(finRootFolder, "data00");
        if (Directory.Exists(data00))
        {
            var fromData00 = Directory.GetFiles(data00, "*.dbf", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => string.Equals(Path.GetFileName(f), "mesta.dbf", StringComparison.OrdinalIgnoreCase));
            if (fromData00 != null)
                return fromData00;
        }

        if (Directory.Exists(finRootFolder))
        {
            return Directory.GetFiles(finRootFolder, "*.dbf", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => string.Equals(Path.GetFileName(f), "mesta.dbf", StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s : string.Empty;

    private static int Int(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0;
        return v switch { decimal d => (int)d, int i => i, long l => (int)l, _ => 0 };
    }

    private static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;

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

    // ── DODAJ ─────────────────────────────────────────────────────────────────
    // Fox: APPEND BLANK → otvara MESTAK za unos novog града

    [RelayCommand]
    private void Dodaj()
    {
        var nova = new GradoviStavka();
        _sveStavke.Add(nova);
        Stavke.Add(nova);
        Selektovana = nova;
        Poruka = "Dodat nov red — unesite podatke i sačuvajte (F2).";
    }

    // ── OBRIŠI ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Obrisi()
    {
        if (Selektovana is null)
        {
            Poruka = "Nije izabran grad za brisanje.";
            return;
        }
        var naziv = string.IsNullOrWhiteSpace(Selektovana.Mesto)
            ? $"MP:{Selektovana.Mp}"
            : Selektovana.Mesto.Trim();
        if (MessageBox.Show($"Obrisati mesto: {naziv}?", "Brisanje",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        _sveStavke.Remove(Selektovana);
        Stavke.Remove(Selektovana);
        Selektovana = Stavke.FirstOrDefault();
        Sacuvaj();
        Poruka = $"Mesto '{naziv}' obrisano i sačuvano.";
    }

    // ── KARTICA ───────────────────────────────────────────────────────────────
    // Fox: LOCATE FOR MP=selectedMP → otvara MESTAK za edit

    [RelayCommand]
    private void Kartica()
    {
        if (Selektovana is null)
        {
            Poruka = "Izaberite grad za uređivanje.";
            return;
        }
        Poruka = $"Uredite red u tabeli — MP: {Selektovana.Mp}, Mesto: {Selektovana.Mesto}. Kada završite, kliknite SAČUVAJ.";
    }

    // ── SAČUVAJ ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Sacuvaj()
    {
        var dbfPath = OdrediCiljnuMestaTabelu();
        if (dbfPath is null || !File.Exists(dbfPath))
        {
            Poruka = "mesta.dbf nije pronađena za čuvanje.";
            return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(dbfPath);
            var rows = _sveStavke.Select(s => new Dictionary<string, object?>
            {
                ["MP"]       = s.Mp,
                ["POSTA"]    = s.Posta,
                ["MESTO"]    = s.Mesto,
                ["ZIRO1"]    = s.Ziro1,
                ["ZIRO2"]    = s.Ziro2,
                ["PORBROJ"]  = s.PorBroj,
                ["PORBROJP"] = s.PorBrojP,
                ["REGSOC"]   = s.RegSoc,
                ["POR"]      = s.Por,
                ["ZDR"]      = s.Zdr,
                ["PIO"]      = s.Pio,
                ["NEZ"]      = s.Nez,
                ["VRSTA"]    = s.Vrsta,
                ["MESEC"]    = (decimal)s.Mesec,
                ["ISPLATA"]  = (decimal)s.Isplata,
                ["PRENETO"]  = s.Preneto,
            }).ToList();

            DbfTableWriter.WriteTable(
                dbfPath, schema, rows,
                static (row, field) => row.TryGetValue(field, out var v) ? v : null);

            Poruka = $"Sačuvano {_sveStavke.Count} mesta u {Path.GetFileName(dbfPath)}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čuvanju: {ex.Message}";
        }
    }

    // ── SPECIFIKACIJA ZDRAVSTVO VLASNIK ───────────────────────────────────────
    // Fox: otvara LDOPJSPEC — spec. doprinos za zdravstvo po opštini

    [RelayCommand]
    private void Specifikacija()
    {
        if (_sveStavke.Count == 0)
        {
            Poruka = "Nema podataka za specifikaciju.";
            return;
        }

        var stavke = _sveStavke
            .Where(s => !string.IsNullOrWhiteSpace(s.Mp))
            .Select(s => new Algoritam.WPF.Models.PregledTabelaStavka
            {
                Sifra  = s.Mp,
                Naziv  = s.Mesto,
                Iznos1 = s.Zdr,
                Iznos2 = s.Por,
            })
            .ToList();

        if (stavke.Count == 0) { Poruka = "Nema stavki za specifikaciju."; return; }

        var view = new Views.Zarade.FoxPregledTabelaView(
            "SPECIFIKACIJA ZDRAVSTVO VLASNIK",
            "Stope doprinosa po mestu/opštini",
            stavke, "ZDRAVSTVO %", "POREZ %");
        view.ShowDialog();
    }

    [RelayCommand]
    private void Osvezi() => UcitajPodatke();
}

public class GradoviStavka
{
    public string  Mp       { get; set; } = string.Empty;
    public string  Posta    { get; set; } = string.Empty;
    public string  Mesto    { get; set; } = string.Empty;
    public string  Ziro1    { get; set; } = string.Empty;
    public string  Ziro2    { get; set; } = string.Empty;
    public string  PorBroj  { get; set; } = string.Empty;
    public string  PorBrojP { get; set; } = string.Empty;
    public string  RegSoc   { get; set; } = string.Empty;
    public decimal Por      { get; set; }
    public decimal Zdr      { get; set; }
    public decimal Pio      { get; set; }
    public decimal Nez      { get; set; }
    public string  Vrsta    { get; set; } = string.Empty;
    public int     Mesec    { get; set; }
    public int     Isplata  { get; set; }
    public string  Preneto  { get; set; } = string.Empty;
}
