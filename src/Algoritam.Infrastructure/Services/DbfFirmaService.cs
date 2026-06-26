using Algoritam.Application;
using Algoritam.Application.Services;
using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Dbf;
using Algoritam.Infrastructure.Migration;
using System.Text.RegularExpressions;

namespace Algoritam.Infrastructure.Services;

/// <summary>
/// Ucitava firme iz FOX foldera. Ako FIN nije postavljen, radi u standalone rezimu
/// i koristi lokalni root pod AppData\FinZarade\standalone.
/// </summary>
public class DbfFirmaService : IFirmaService
{
    private static readonly Regex FirmaFolderRegex = new("^F(?<broj>\\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly HashSet<string> DozvoljeneTemplateEkstenzije = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dbf",
        ".cdx",
        ".fpt",
        ".dbt"
    };

    private readonly IPutanjaService _putanjaService;

    public DbfFirmaService(IPutanjaService putanjaService)
    {
        _putanjaService = putanjaService;
    }

    public Task<IReadOnlyList<Firma>> DajSveFirmeAsync()
    {
        var firme = UcitajFirme();
        return Task.FromResult<IReadOnlyList<Firma>>(firme);
    }

    public Task<Firma?> DajFirmuAsync(int id)
    {
        var firme = UcitajFirme();
        return Task.FromResult(firme.FirstOrDefault(f => f.Id == id));
    }

    public Task<Firma?> DodajFirmuAsync()
    {
        var root = ResolveFirmaRoot();
        Directory.CreateDirectory(root);

        var noviFolder = FoxWorkspaceSupport.GetNextFirmaFolder(root);
        if (Directory.Exists(noviFolder))
            throw new InvalidOperationException($"Folder vec postoji: {noviFolder}");

        Directory.CreateDirectory(noviFolder);

        // Kopiramo samo mali set kljucnih DBF fajlova (sema tabela),
        // ne ceo template folder (koji moze biti 50MB+ i spor na mrezi)
        var templateFolder = FoxWorkspaceSupport.FindTemplateF1(root);
        if (!string.IsNullOrWhiteSpace(templateFolder) && Directory.Exists(templateFolder))
            KopirajSamoKljucneTabele(templateFolder, noviFolder);

        Directory.CreateDirectory(ZaradePaths.GetFirmaZaradeFolder(noviFolder));

        var firma = UcitajFirme().FirstOrDefault(f =>
            string.Equals(f.FolderPath, noviFolder, StringComparison.OrdinalIgnoreCase));

        if (firma is not null)
            return Task.FromResult<Firma?>(firma);

        return Task.FromResult<Firma?>(KreirajFallbackFirmu(0, noviFolder));
    }

    private List<Firma> UcitajFirme()
    {
        var root = ResolveFirmaRoot();
        Directory.CreateDirectory(root);

        if (!FoxWorkspaceSupport.IsValidFinRoot(root))
            ObezbediPodrazumevanuStandaloneFirmu(root);

        var result = new List<Firma>();
        int nextId = 1;

        var podfolderi = Directory.GetDirectories(root)
            .Where(d => FirmaFolderRegex.IsMatch(Path.GetFileName(d)))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var folder in podfolderi)
        {
            var firmaDbf = PronadjiDbfCaseInsensitive(folder, "firma.dbf");

            if (firmaDbf is null)
            {
                result.Add(KreirajFallbackFirmu(nextId++, folder));
                continue;
            }

            try
            {
                var records = DbfReader.CitajSveZapise(firmaDbf);
                if (records.Count == 0)
                {
                    result.Add(KreirajFallbackFirmu(nextId++, folder));
                    continue;
                }

                var rec = records[0];
                var firma = MapDbfToFirma(rec, nextId++, folder);

                if (string.IsNullOrWhiteSpace(firma.Naziv))
                    firma.Naziv = Path.GetFileName(folder);

                result.Add(firma);
            }
            catch
            {
                result.Add(KreirajFallbackFirmu(nextId++, folder));
            }
        }

        return result;
    }

    private string ResolveFirmaRoot()
    {
        var finPutanja = _putanjaService.DajFinPutanju();
        return FoxWorkspaceSupport.ResolveFirmaRoot(finPutanja);
    }

    private static void ObezbediPodrazumevanuStandaloneFirmu(string root)
    {
        var postojeci = FoxWorkspaceSupport.ListFirmaFolders(root);
        if (postojeci.Count > 0)
        {
            foreach (var folder in postojeci)
                Directory.CreateDirectory(ZaradePaths.GetFirmaZaradeFolder(folder));
            return;
        }

        var f1 = FoxWorkspaceSupport.EnsureFirstFirmaFolder(root);
        var templateFolder = FoxWorkspaceSupport.FindTemplateF1(root);
        if (!string.IsNullOrWhiteSpace(templateFolder) && Directory.Exists(templateFolder))
            KopirajTemplateTabele(templateFolder, f1);
    }

    private static Firma MapDbfToFirma(Dictionary<string, object?> rec, int id, string folderPath)
    {
        return new Firma
        {
            Id = id,
            Naziv = Str(rec, "FIME"),
            Naziv2 = Str(rec, "FIME2"),
            PostanskiBroj = Str(rec, "FPOS"),
            Mesto = Str(rec, "FMES"),
            Ulica = Str(rec, "FUL"),
            BrojUlice = Str(rec, "FULBR"),
            Drzava = Str(rec, "FDRZAVA") is { Length: > 0 } d ? d : "Srbija",
            ZiroRacun = Str(rec, "FZIRO"),
            Maticni = Str(rec, "FMAT"),
            Pib = Str(rec, "FPOR"),
            Aktivna = true,
            FolderPath = folderPath
        };
    }

    private static Firma KreirajFallbackFirmu(int id, string folderPath)
    {
        var folderName = Path.GetFileName(folderPath);
        var resolvedId = id > 0 ? id : 1;

        return new Firma
        {
            Id = resolvedId,
            Naziv = $"Lokalna firma {folderName}",
            Naziv2 = $"Lokalna firma {folderName}",
            Aktivna = true,
            Drzava = "Srbija",
            FolderPath = folderPath
        };
    }

    private static string Str(Dictionary<string, object?> rec, string key)
        => rec.TryGetValue(key, out var v) && v is string s ? s : string.Empty;

    // Kljucni DBF fajlovi koji definisu semu — sve ostalo se kreira lazily
    private static readonly HashSet<string> KljucniDbfFajlovi = new(StringComparer.OrdinalIgnoreCase)
    {
        "firma.dbf", "firma.cdx",
        "ldparam.dbf", "ldparam.cdx",
        "lozinke.dbf", "lozinke.cdx",
    };

    private static void KopirajSamoKljucneTabele(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var filePath in Directory.GetFiles(sourceDirectory))
        {
            var fileName = Path.GetFileName(filePath);
            if (!KljucniDbfFajlovi.Contains(fileName))
                continue;

            var ext = Path.GetExtension(filePath);
            if (!DozvoljeneTemplateEkstenzije.Contains(ext))
                continue;

            var targetFilePath = Path.Combine(targetDirectory, fileName);
            File.Copy(filePath, targetFilePath, overwrite: true);
        }
    }

    private static void KopirajTemplateTabele(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var filePath in Directory.GetFiles(sourceDirectory))
        {
            var ext = Path.GetExtension(filePath);
            if (!DozvoljeneTemplateEkstenzije.Contains(ext))
                continue;

            var targetFilePath = Path.Combine(targetDirectory, Path.GetFileName(filePath));
            File.Copy(filePath, targetFilePath, overwrite: true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDirectory))
        {
            var targetSubDir = Path.Combine(targetDirectory, Path.GetFileName(subDir));
            KopirajTemplateTabele(subDir, targetSubDir);
        }
    }

    public Task<bool> ObrisiFirmuAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return Task.FromResult(false);

        Directory.Delete(folderPath, recursive: true);
        return Task.FromResult(true);
    }

    private static string? PronadjiDbfCaseInsensitive(string folderPath, string dbfName)
    {
        if (!Directory.Exists(folderPath))
            return null;

        return Directory.GetFiles(folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => string.Equals(Path.GetFileName(f), dbfName, StringComparison.OrdinalIgnoreCase));
    }
}
