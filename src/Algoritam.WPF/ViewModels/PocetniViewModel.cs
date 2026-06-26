using Algoritam.Application;
using Algoritam.Application.Services;
using Algoritam.Infrastructure.Data;
using Algoritam.Infrastructure.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Algoritam.WPF.ViewModels;

public partial class PocetniViewModel : ObservableObject
{
    private const string LokalniModMarkerFajl = ".algoritam-local-no-password";
    private static readonly Regex FirmaFolderRegex =
        new("^F\\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IPutanjaService _putanjaService;

    public PocetniViewModel(IPutanjaService putanjaService)
    {
        _putanjaService = putanjaService;
        OsveziPutanju();
        ArhivaPutanja = _putanjaService.DajArhivaPutanju() ?? string.Empty;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PutanjaPrikazana))]
    [NotifyPropertyChangedFor(nameof(PutanjaPostavljena))]
    [NotifyPropertyChangedFor(nameof(PutanjaBoja))]
    [NotifyPropertyChangedFor(nameof(PutanjaIkonica))]
    private string _finPutanja = string.Empty;

    [ObservableProperty]
    private string _statusPoruka = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ArhivaPutanjaPrikazana))]
    [NotifyPropertyChangedFor(nameof(ArhivaPutanjaPostavljena))]
    [NotifyPropertyChangedFor(nameof(ArhivaPutanjaBoja))]
    [NotifyPropertyChangedFor(nameof(ArhivaPutanjaIkonica))]
    private string _arhivaPutanja = string.Empty;

    public string ArhivaPutanjaPrikazana =>
        string.IsNullOrEmpty(ArhivaPutanja) ? "Putanja nije postavljena" : ArhivaPutanja;

    public bool ArhivaPutanjaPostavljena => !string.IsNullOrEmpty(ArhivaPutanja);

    public string ArhivaPutanjaBoja => ArhivaPutanjaPostavljena ? "#A5D6A7" : "#EF9A9A";
    public string ArhivaPutanjaIkonica => ArhivaPutanjaPostavljena ? "OK" : "!";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(KreirajLokalnuFinInstalacijuCommand))]
    [NotifyCanExecuteChangedFor(nameof(PostaviPutanjuCommand))]
    private bool _ucitava = false;

    public string PutanjaPrikazana =>
        string.IsNullOrEmpty(FinPutanja) ? "Putanja nije postavljena" : FinPutanja;

    public bool PutanjaPostavljena =>
        !string.IsNullOrEmpty(FinPutanja) && _putanjaService.JeValidanFinFolder(FinPutanja);

    public string PutanjaBoja => PutanjaPostavljena ? "#A5D6A7" : "#EF9A9A";

    public string PutanjaIkonica => PutanjaPostavljena ? "OK" : "!";

    [RelayCommand(CanExecute = nameof(MozeKreirajIliPostavi))]
    private void PostaviPutanju()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Izaberi root folder aplikacije",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        var odabranaPutanja = dialog.FolderName;

        if (!_putanjaService.JeValidanFinFolder(odabranaPutanja))
        {
            StatusPoruka = "Izabrani folder nije validan ili ne postoji.";
            return;
        }

        if (_putanjaService.SnimiFinPutanju(odabranaPutanja))
        {
            OsveziPutanju();
            StatusPoruka = string.Empty;
        }
        else
        {
            StatusPoruka = "Nije moguće sačuvati putanju.";
        }
    }

    [RelayCommand]
    private void PostaviArhivaPutanju()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Izaberite folder za arhiviranje firmi",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        if (_putanjaService.SnimiArhivaPutanju(dialog.FolderName))
        {
            ArhivaPutanja = dialog.FolderName;
            StatusPoruka = string.Empty;
        }
        else
        {
            StatusPoruka = "Nije moguÄ‡e saÄuvati putanju za arhivu.";
        }
    }

    private bool MozeKreirajIliPostavi() => !Ucitava;

    [RelayCommand(CanExecute = nameof(MozeKreirajIliPostavi))]
    private async Task KreirajLokalnuFinInstalaciju()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Izaberite folder za lokalnu FIN instalaciju (data00, data01, F1)",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        var ciljnaPutanja = dialog.FolderName;

        Ucitava = true;
        StatusPoruka = string.Empty;

        try
        {
            StatusPoruka = "TraÅ¾im template foldere...";
            var templateRoot = await Task.Run(() => PronadjiTemplateRoot(AppContext.BaseDirectory));

            StatusPoruka = "Kreiram strukturu foldera...";
            await Task.Run(async () =>
            {
                Directory.CreateDirectory(ciljnaPutanja);

                if (!string.IsNullOrEmpty(templateRoot))
                {
                    KopirajDirektorijum(Path.Combine(templateRoot, "data00"), Path.Combine(ciljnaPutanja, "data00"));
                    KopirajDirektorijum(Path.Combine(templateRoot, "data01"), Path.Combine(ciljnaPutanja, "data01"));
                    KopirajDirektorijum(Path.Combine(templateRoot, "F1"), Path.Combine(ciljnaPutanja, "F1"));
                }
                else
                {
                    Directory.CreateDirectory(Path.Combine(ciljnaPutanja, "data00"));
                    Directory.CreateDirectory(Path.Combine(ciljnaPutanja, "data01"));
                    Directory.CreateDirectory(Path.Combine(ciljnaPutanja, "F1"));
                }

                var data01Folder = Path.Combine(ciljnaPutanja, "data01");
                var folder01 = Path.Combine(ciljnaPutanja, "01");
                if (Directory.Exists(data01Folder))
                    KopirajDirektorijum(data01Folder, folder01);
                else
                    Directory.CreateDirectory(folder01);

                File.WriteAllText(Path.Combine(ciljnaPutanja, LokalniModMarkerFajl), DateTime.UtcNow.ToString("O"));

                // Resetuj LOZINKE.DBF â€” uvek kreira sveÅ¾u sa Admin/admin
                var data00Path = Path.Combine(ciljnaPutanja, "data00");
                foreach (var ime in new[] { "LOZINKE", "LOZINKEA" })
                {
                    foreach (var ext in new[] { ".DBF", ".CDX", ".FPT", ".dbf", ".cdx", ".fpt" })
                    {
                        var f = Path.Combine(data00Path, ime + ext);
                        if (File.Exists(f)) File.Delete(f);
                    }
                }
                FinWorkspaceResolver.EnsureLozinkeTables(ciljnaPutanja, out _, out _, out _);

                // Pri inicijalnoj instalaciji ostavi prazne samo trazene forme/tabele.
                var f1Path = Path.Combine(ciljnaPutanja, "F1");
                foreach (var tbl in new[]
                {
                    // RADNICI
                    "ldrad",

                    // KREDITI
                    "ldkred",
                    "ldkredr",

                    // PODACI O FIRMI / FIRME
                    "firma",
                    "firma3",

                    // PARTNERI
                    "an0",

                    // PLATNI SPISAK — template fajlovi (cuvaju strukturu za kreiranje novih)
                    "ld",
                    "ld00",
                    "ldspis"
                })
                {
                    PraznDbfTabelu(f1Path, tbl);
                }

                // Obrisi mesecne LD fajlove iz templatea — sadrze stare podatke
                // (LD01, LD02, LDP01, LDB01...). Template struktura ostaje u ld.dbf/ld00.dbf.
                ObrisiMesecneLdFajlove(f1Path);

                // Resetuj i LOZINKE u F1 (kopija koja moze imati stare korisnike)
                foreach (var ime in new[] { "LOZINKE", "LOZINKEA" })
                {
                    foreach (var ext in new[] { ".DBF", ".CDX", ".FPT", ".dbf", ".cdx", ".fpt" })
                    {
                        var f2 = Path.Combine(f1Path, ime + ext);
                        if (File.Exists(f2)) File.Delete(f2);
                    }
                }

                // Kreiraj SQLite bazu sa svim tabelama za F1 firmu
                var f1Folder = Path.Combine(ciljnaPutanja, "F1");
                var dbPath = ZaradePaths.GetDbPath(f1Folder);
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
                using var ctx = new FirmaDbContext(dbPath);
                await ctx.Database.EnsureCreatedAsync();
                await LdObracunSchemaBootstrapper.EnsureAsync(ctx);
                await LdPodSchemaBootstrapper.EnsureAsync(ctx);
                await LdSpisSchemaBootstrapper.EnsureAsync(ctx);
                await LdKnjizenjeSchemaBootstrapper.EnsureAsync(ctx);
                await LdParametriSchemaBootstrapper.EnsureAsync(ctx);
            });

            if (!_putanjaService.SnimiFinPutanju(ciljnaPutanja))
            {
                StatusPoruka = "Nije moguće postaviti lokalnu FIN putanju.";
                return;
            }

            OsveziPutanju();
            StatusPoruka = string.IsNullOrEmpty(templateRoot)
                ? "Kreirana je prazna FIN struktura."
                : "Kreirana je FIN struktura sa template tabelama.";

            OtvoriFolder(ciljnaPutanja);
        }
        catch (Exception ex)
        {
            StatusPoruka = $"Greska pri kreiranju lokalne instalacije: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    private static string? PronadjiTemplateRoot(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);

        while (current != null)
        {
            var templatesCandidate = Path.Combine(current.FullName, "templates");
            if (ImaSveTemplateFoldere(templatesCandidate))
                return templatesCandidate;

            var oldProjectCandidate = Path.Combine(current.FullName, "old-project");
            if (ImaSveTemplateFoldere(oldProjectCandidate))
                return oldProjectCandidate;

            current = current.Parent;
        }

        return null;
    }

    private static bool ImaSveTemplateFoldere(string root)
    {
        if (!Directory.Exists(root))
            return false;

        return Directory.Exists(Path.Combine(root, "data00"))
            && Directory.Exists(Path.Combine(root, "data01"))
            && Directory.Exists(Path.Combine(root, "F1"));
    }

    private static void KopirajDirektorijum(string sourceDirectory, string targetDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
            return;

        Directory.CreateDirectory(targetDirectory);

        foreach (var filePath in Directory.GetFiles(sourceDirectory))
        {
            var targetFilePath = Path.Combine(targetDirectory, Path.GetFileName(filePath));
            File.Copy(filePath, targetFilePath, overwrite: true);
        }

        foreach (var subDirectory in Directory.GetDirectories(sourceDirectory))
        {
            var targetSubDirectory = Path.Combine(targetDirectory, Path.GetFileName(subDirectory));
            KopirajDirektorijum(subDirectory, targetSubDirectory);
        }
    }

    private static void OtvoriFolder(string folderPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = folderPath,
            UseShellExecute = true
        });
    }

    private void OsveziPutanju()
    {
        FinPutanja = _putanjaService.DajFinPutanju() ?? string.Empty;
    }

    // Isprazni DBF tabelu (zadrzi strukturu/header, obrisi sve zapise) i obrisi CDX/FPT
    private static void PraznDbfTabelu(string folder, string tabelaBaseName)
    {
        if (!Directory.Exists(folder))
            return;

        var fajlovi = Directory.GetFiles(folder);

        foreach (var ext in new[] { ".CDX", ".FPT", ".cdx", ".fpt" })
        {
            var cdx = fajlovi.FirstOrDefault(f =>
                string.Equals(Path.GetFileNameWithoutExtension(f), tabelaBaseName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Path.GetExtension(f), ext, StringComparison.OrdinalIgnoreCase));
            if (cdx is not null)
                File.Delete(cdx);
        }

        var dbfPath = fajlovi.FirstOrDefault(f =>
            string.Equals(Path.GetFileNameWithoutExtension(f), tabelaBaseName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Path.GetExtension(f), ".dbf", StringComparison.OrdinalIgnoreCase));

        if (dbfPath is null)
            return;

        try
        {
            var bytes = File.ReadAllBytes(dbfPath);
            if (bytes.Length < 10)
                return;

            // Nuliramo broj zapisa (byte 4-7, little-endian uint32)
            bytes[4] = 0; bytes[5] = 0; bytes[6] = 0; bytes[7] = 0;

            // Velicina headera (byte 8-9, uint16 little-endian)
            var headerSize = (int)bytes[8] | ((int)bytes[9] << 8);
            if (headerSize < 32 || headerSize > bytes.Length)
                return;

            // Ostavi samo header + EOF marker (0x1A)
            var result = new byte[headerSize + 1];
            Array.Copy(bytes, result, headerSize);
            result[headerSize] = 0x1A;

            File.WriteAllBytes(dbfPath, result);
        }
        catch
        {
            // Ne prekidamo tok ako jedan fajl ne moze biti isprazn jen
        }
    }

    // Brise mesecne LD fajlove iz template F1 foldera:
    // LD01, LD02, ..., LDP01, LDB01 itd. (cisto-numericke sufikse).
    // Ne dize LD.DBF / LD00.DBF — ti ostaju kao prazni template fajlovi.
    private static void ObrisiMesecneLdFajlove(string folder)
    {
        if (!Directory.Exists(folder)) return;

        foreach (var fajl in Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly))
        {
            var ext = Path.GetExtension(fajl);
            if (!string.Equals(ext, ".dbf", StringComparison.OrdinalIgnoreCase)) continue;

            var baseName = Path.GetFileNameWithoutExtension(fajl).ToUpperInvariant();

            // Odredi prefiks i numericku sufiksnu vrednost
            string suffix;
            if (baseName.StartsWith("LDB") && baseName.Length > 3)
                suffix = baseName[3..];
            else if (baseName.StartsWith("LDP") && baseName.Length > 3)
                suffix = baseName[3..];
            else if (baseName.StartsWith("LD") && baseName.Length > 2)
                suffix = baseName[2..];
            else
                continue;

            // Samo fajlovi sa cisto-numerickim sufiksom (ne LDRAD, LDKRED, LDPREV...)
            if (suffix.Length == 0 || !suffix.All(char.IsDigit)) continue;

            // Preskoci LD0 i LD00 — template fajlovi, vec ispraznjeni
            if (baseName == "LD0" || baseName == "LD00") continue;

            try { File.Delete(fajl); } catch { }

            foreach (var compExt in new[] { ".CDX", ".FPT", ".cdx", ".fpt" })
            {
                var companion = Path.ChangeExtension(fajl, compExt);
                if (File.Exists(companion)) try { File.Delete(companion); } catch { }
            }
        }
    }
}
