using System.IO;

namespace Algoritam.Application;

public static class ZaradePaths
{
    public const string ZaradeFolderName = "zarade";

    public static string GetFirmaZaradeFolder(string firmaFolderPath)
        => Path.Combine(firmaFolderPath, ZaradeFolderName);

    public static string GetDbPath(string firmaFolderPath)
        => Path.Combine(GetFirmaZaradeFolder(firmaFolderPath), "algoritam.db");
}
