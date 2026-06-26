using System.IO;

namespace OsnovnaSredstva.Services;

public static class BackupService
{
    public readonly record struct BackupRezultat(string OdredisteFolder, int BrojFajlova, long UkupnoBytes);

    public static async Task<BackupRezultat> NapraviBackupAsync(
        string izvorFolder,
        string arhivFolder,
        string firmaIme,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(izvorFolder))
            throw new DirectoryNotFoundException($"Folder firme ne postoji: {izvorFolder}");

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safe = string.Concat(firmaIme.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_'));
        var destFolder = Path.Combine(arhivFolder, $"{safe}_{timestamp}");

        Directory.CreateDirectory(destFolder);

        var fajlovi = Directory.GetFiles(izvorFolder, "*", SearchOption.AllDirectories);
        long ukupno = 0;
        int broj = 0;

        foreach (var src in fajlovi)
        {
            ct.ThrowIfCancellationRequested();

            var relativni = Path.GetRelativePath(izvorFolder, src);
            var dst = Path.Combine(destFolder, relativni);
            var dstDir = Path.GetDirectoryName(dst)!;

            if (!Directory.Exists(dstDir))
                Directory.CreateDirectory(dstDir);

            await Task.Run(() => File.Copy(src, dst, overwrite: true), ct);

            ukupno += new FileInfo(dst).Length;
            broj++;
        }

        return new BackupRezultat(destFolder, broj, ukupno);
    }

    public static string FormatVelicine(long bytes) => bytes switch
    {
        < 1024          => $"{bytes} B",
        < 1024 * 1024   => $"{bytes / 1024.0:0.0} KB",
        _               => $"{bytes / (1024.0 * 1024):0.0} MB"
    };
}
