using Algoritam.Application;

namespace Algoritam.Infrastructure.Migration;

/// <summary>
/// Odrzava SQLite bazu firme sinhronizovanom sa FOX DBF izvorom.
/// Poziva migraciju samo kada baza ne postoji ili su DBF/FPT fajlovi noviji od baze.
/// </summary>
public static class FoxDataSync
{
    public static bool TryEnsureFresh(string firmaFolderPath, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(firmaFolderPath) || !Directory.Exists(firmaFolderPath))
            return false;

        try
        {
            var dbPath = ZaradePaths.GetDbPath(firmaFolderPath);
            if (!TrebaOsvezitiMigraciju(firmaFolderPath, dbPath))
                return false;

            new DbfToSqliteMigrator().MigrujFirmuAsync(firmaFolderPath).GetAwaiter().GetResult();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TrebaOsvezitiMigraciju(string folderPath, string dbPath)
    {
        if (!File.Exists(dbPath))
            return true;

        DateTime sqliteVreme;
        try
        {
            sqliteVreme = File.GetLastWriteTimeUtc(dbPath);
        }
        catch
        {
            return true;
        }

        try
        {
            var sourceFajlovi = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(path =>
                {
                    var ext = Path.GetExtension(path);
                    return ext.Equals(".dbf", StringComparison.OrdinalIgnoreCase)
                        || ext.Equals(".fpt", StringComparison.OrdinalIgnoreCase);
                });

            foreach (var sourceFajl in sourceFajlovi)
            {
                if (File.GetLastWriteTimeUtc(sourceFajl) > sqliteVreme)
                    return true;
            }
        }
        catch
        {
            // Ako timestamp provera ne uspe, sigurnije je da uradimo osvezavanje.
            return true;
        }

        return false;
    }
}
