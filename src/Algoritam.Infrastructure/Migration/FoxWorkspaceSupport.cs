using Algoritam.Application;
using Serilog;
using System.Text.RegularExpressions;

namespace Algoritam.Infrastructure.Migration;

/// <summary>
/// Pomocni alati za rad sa FOX folderima (FIN povezani i standalone rezim).
/// </summary>
public static class FoxWorkspaceSupport
{
    private static readonly Regex FirmaFolderRegex =
        new("^F\\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> FoxExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dbf",
        ".cdx",
        ".fpt",
        ".dbt"
    };

    private static readonly HashSet<string> LdExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dbf",
        ".cdx",
        ".fpt",
        ".dbt"
    };

    public static string GetStandaloneRoot()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FinZarade",
            "standalone");

    public static string ResolveFirmaRoot(string? finRoot)
    {
        if (string.IsNullOrWhiteSpace(finRoot))
            return GetStandaloneRoot();

        var normalized = Algoritam.Infrastructure.Services.FinWorkspaceResolver.NormalizeRootPath(finRoot);
        return Directory.Exists(normalized) ? normalized : GetStandaloneRoot();
    }

    public static bool IsValidFinRoot(string? rootPath)
    {
        return Algoritam.Infrastructure.Services.FinWorkspaceResolver.IsValidWorkspaceRoot(rootPath);
    }

    public static bool IsFirmaUnderRoot(string firmaFolderPath, string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(firmaFolderPath) || string.IsNullOrWhiteSpace(rootPath))
            return false;

        try
        {
            var firmaFull = Path.GetFullPath(firmaFolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var rootFull = Path.GetFullPath(rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return firmaFull.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || string.Equals(firmaFull, rootFull, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static List<string> ListFirmaFolders(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            return [];

        return Directory.GetDirectories(rootPath, "F*", SearchOption.TopDirectoryOnly)
            .Where(d => FirmaFolderRegex.IsMatch(Path.GetFileName(d)))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string EnsureFirstFirmaFolder(string rootPath)
    {
        Directory.CreateDirectory(rootPath);
        var postojece = ListFirmaFolders(rootPath);
        if (postojece.Count > 0)
        {
            Directory.CreateDirectory(ZaradePaths.GetFirmaZaradeFolder(postojece[0]));
            return postojece[0];
        }

        var f1 = Path.Combine(rootPath, "F1");
        Directory.CreateDirectory(f1);
        Directory.CreateDirectory(ZaradePaths.GetFirmaZaradeFolder(f1));
        return f1;
    }

    public static string GetNextFirmaFolder(string rootPath)
    {
        var max = 0;
        foreach (var folder in ListFirmaFolders(rootPath))
        {
            var name = Path.GetFileName(folder);
            if (name.Length <= 1)
                continue;

            if (int.TryParse(name[1..], out var broj))
                max = Math.Max(max, broj);
        }

        return Path.Combine(rootPath, $"F{max + 1}");
    }

    public static string? FindTemplateF1(string? preferredRoot = null)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(preferredRoot))
        {
            candidates.Add(Path.Combine(preferredRoot!, "templates", "F1"));
            candidates.Add(Path.Combine(preferredRoot!, "F1"));
        }

        candidates.Add(Path.Combine(AppContext.BaseDirectory, "templates", "F1"));
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "old-project", "F1"));

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            candidates.Add(Path.Combine(current.FullName, "templates", "F1"));
            candidates.Add(Path.Combine(current.FullName, "old-project", "F1"));
            current = current.Parent;
        }

        return candidates.FirstOrDefault(c =>
            Directory.Exists(c) &&
            (File.Exists(Path.Combine(c, "firma.dbf")) || File.Exists(Path.Combine(c, "ld.dbf"))));
    }

    public static int CopyFoxFiles(string sourceDirectory, string targetDirectory, bool overwrite)
    {
        if (!Directory.Exists(sourceDirectory))
            return 0;

        Directory.CreateDirectory(targetDirectory);
        int copied = 0;

        var files = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.TopDirectoryOnly);
        Log.Debug("CopyFoxFiles — nasao {Total} fajlova u {Dir}", files.Length, sourceDirectory);

        foreach (var sourcePath in files)
        {
            var ext = Path.GetExtension(sourcePath);
            if (!FoxExtensions.Contains(ext))
                continue;

            var fileName = Path.GetFileName(sourcePath);
            var size = new FileInfo(sourcePath).Length;
            Log.Debug("CopyFoxFiles — kopiram {File} ({Size:N0} bytes)...", fileName, size);

            var targetPath = Path.Combine(targetDirectory, fileName);
            if (!overwrite && File.Exists(targetPath))
                continue;

            File.Copy(sourcePath, targetPath, overwrite);
            copied++;
        }

        return copied;
    }

    public static int CopyLdTablesFromTemplate(string templateF1Folder, string targetFolder, bool overwrite)
    {
        if (!Directory.Exists(templateF1Folder))
            return 0;

        Directory.CreateDirectory(targetFolder);
        int copied = 0;

        foreach (var sourcePath in Directory.GetFiles(templateF1Folder, "*.*", SearchOption.TopDirectoryOnly))
        {
            var ext = Path.GetExtension(sourcePath);
            if (!LdExtensions.Contains(ext))
                continue;

            var fileName = Path.GetFileName(sourcePath);
            if (!fileName.StartsWith("LD", StringComparison.OrdinalIgnoreCase))
                continue;

            var targetPath = Path.Combine(targetFolder, fileName);
            if (!overwrite && File.Exists(targetPath))
                continue;

            File.Copy(sourcePath, targetPath, overwrite);
            copied++;
        }

        return copied;
    }

    public static bool ContainsAnyLdTable(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return false;

        return Directory.GetFiles(folderPath, "LD*.DBF", SearchOption.TopDirectoryOnly).Length > 0
            || Directory.GetFiles(folderPath, "ld*.dbf", SearchOption.TopDirectoryOnly).Length > 0;
    }

    public static FoxSnapshotResult CreateFoxSnapshot(string sourceFolder, string targetSnapshotsRoot)
    {
        Log.Debug("CreateFoxSnapshot START — source={Source}, target={Target}", sourceFolder, targetSnapshotsRoot);

        if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
        {
            Log.Warning("CreateFoxSnapshot — source folder ne postoji");
            return new FoxSnapshotResult(false, null, 0, "Source folder ne postoji.");
        }

        try
        {
            Log.Debug("CreateFoxSnapshot — kreiram targetSnapshotsRoot...");
            Directory.CreateDirectory(targetSnapshotsRoot);
            var snapshotFolder = Path.Combine(targetSnapshotsRoot, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Log.Debug("CreateFoxSnapshot — kreiram snapshotFolder: {Folder}", snapshotFolder);
            Directory.CreateDirectory(snapshotFolder);

            Log.Debug("CreateFoxSnapshot — pocinjem CopyFoxFiles...");
            var copied = CopyFoxFiles(sourceFolder, snapshotFolder, overwrite: true);
            Log.Debug("CreateFoxSnapshot — kopirano {Count} fajlova", copied);

            return copied == 0
                ? new FoxSnapshotResult(false, snapshotFolder, 0, "Nema FOX tabela za snapshot.")
                : new FoxSnapshotResult(true, snapshotFolder, copied, null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CreateFoxSnapshot — GRESKA");
            return new FoxSnapshotResult(false, null, 0, ex.Message);
        }
    }

    public sealed record FoxSnapshotResult(bool Success, string? SnapshotPath, int CopiedFiles, string? Error);
}
