using Algoritam.Infrastructure.Dbf;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Algoritam.Infrastructure.Services;

public static class FinWorkspaceResolver
{
    private static readonly Regex FirmaFolderRegex =
        new("^F\\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DataFolderRegex =
        new("^data\\d{2}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NumericFolderRegex =
        new("^\\d{2}$", RegexOptions.Compiled);

    private static readonly string[] FinMarkerFolders = ["data00", "databaze", "00FIN"];
    private static readonly string[] SidecarExtensions = [".dbf", ".cdx", ".fpt", ".dbt"];

    private static readonly DbfFieldDef[] LozinkeMinimalFields =
    [
        new("PAS", 'C', 2),
        new("KORISNIK", 'C', 20),
        new("LOZINKA", 'C', 20),
        new("KORIME", 'C', 30),
        new("AKTIVAN", 'C', 1),
        new("OPERATER", 'C', 1),
        new("PASSNIVO", 'N', 1, 0),
        new("PASSGK", 'L', 1),
        new("PASSAN", 'L', 1),
        new("PASSBL", 'L', 1),
        new("PASSTV", 'L', 1),
        new("PASSTM", 'L', 1),
        new("PASSUS", 'L', 1),
        new("PASSLD", 'L', 1),
        new("PASSOST", 'L', 1),
        new("PASSPRN", 'L', 1),
        new("PASSPRO", 'L', 1),
        new("PASSOS", 'L', 1),
        new("PASSPROF", 'L', 1),
        new("PASSDEL", 'L', 1),
        new("PASSTVRA", 'L', 1),
        new("PASSTVKAL", 'L', 1),
        new("PASSTVRAC", 'L', 1),
        new("PASSTVNIV", 'L', 1),
        new("PASSTMRA", 'L', 1),
        new("PASSTMKAL", 'L', 1),
        new("PASSTMRAC", 'L', 1),
        new("PASSTMNIV", 'L', 1),
        new("DATUM", 'D', 8),
        new("VREME0", 'C', 10),
        new("VREME1", 'C', 10),
        new("SLIKE", 'C', 2),
        new("MAGACIN", 'N', 2, 0),
        new("PUTANJA", 'C', 80),
        new("FOXY", 'C', 1),
        new("PDFPRINT", 'C', 1),
        new("PRENETO", 'C', 1),
        new("IDBR", 'N', 11, 0),
    ];

    private static readonly DbfFieldDef[] LozinkeaMinimalFields =
    [
        new("PAS", 'C', 2),
        new("KORISNIK", 'C', 20),
        new("AKTIVAN", 'C', 1),
        new("DATUM", 'D', 8),
        new("VREME0", 'C', 10),
        new("VREME1", 'C', 10),
        new("PRENETO", 'C', 1),
        new("IDBR", 'N', 11, 0),
    ];

    public static string NormalizeRootPath(string path)
    {
        var trimmed = (path ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return string.Empty;

        var full = Path.GetFullPath(trimmed);
        if (!Directory.Exists(full))
            return full;

        var name = Path.GetFileName(full);
        var parent = Directory.GetParent(full)?.FullName;
        if (string.IsNullOrWhiteSpace(parent))
            return full;

        if (FirmaFolderRegex.IsMatch(name) ||
            DataFolderRegex.IsMatch(name) ||
            NumericFolderRegex.IsMatch(name))
        {
            return parent;
        }

        return full;
    }

    public static bool IsValidWorkspaceRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var normalized = NormalizeRootPath(path);
        return Directory.Exists(normalized);
    }

    public static bool IsClassicFinRoot(string? path)
    {
        if (!IsValidWorkspaceRoot(path))
            return false;

        var root = NormalizeRootPath(path!);
        return FinMarkerFolders.Any(marker => Directory.Exists(Path.Combine(root, marker)));
    }

    public static bool LooksLikeWorkspaceStructure(string? path)
    {
        if (!IsValidWorkspaceRoot(path))
            return false;

        var root = NormalizeRootPath(path!);

        if (IsClassicFinRoot(root))
            return true;

        if (Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Any(name => !string.IsNullOrWhiteSpace(name) && FirmaFolderRegex.IsMatch(name)))
        {
            return true;
        }

        if (Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Any(name => !string.IsNullOrWhiteSpace(name) &&
                (DataFolderRegex.IsMatch(name) || NumericFolderRegex.IsMatch(name))))
        {
            return true;
        }

        return Directory.GetFiles(root, "*.dbf", SearchOption.TopDirectoryOnly).Length > 0;
    }

    public static string GetData00Path(string rootPath)
        => Path.Combine(NormalizeRootPath(rootPath), "data00");

    public static void EnsureWorkspaceInitialized(string rootPath)
    {
        var root = NormalizeRootPath(rootPath);
        if (string.IsNullOrWhiteSpace(root))
            throw new InvalidOperationException("Root putanja nije validna.");

        Directory.CreateDirectory(root);
        Directory.CreateDirectory(GetData00Path(root));
        Directory.CreateDirectory(Path.Combine(root, "data01"));
        Directory.CreateDirectory(Path.Combine(root, "01"));

        EnsureLozinkeTables(root, out _, out _, out _);
    }

    public static bool EnsureLozinkeTables(
        string rootPath,
        out string lozinkePath,
        out string lozinkeaPath,
        out string message)
    {
        lozinkePath = string.Empty;
        lozinkeaPath = string.Empty;
        message = string.Empty;

        var root = NormalizeRootPath(rootPath);
        if (string.IsNullOrWhiteSpace(root))
        {
            message = "Putanja nije validna.";
            return false;
        }

        try
        {
            Directory.CreateDirectory(root);
            var data00 = GetData00Path(root);
            Directory.CreateDirectory(data00);

            lozinkePath = Path.Combine(data00, "LOZINKE.DBF");
            lozinkeaPath = Path.Combine(data00, "LOZINKEA.DBF");

            EnsureTableExists(root, lozinkePath, "lozinke", LozinkeMinimalFields);
            EnsureTableExists(root, lozinkeaPath, "lozinkea", LozinkeaMinimalFields);

            EnsureDefaultAdminUser(lozinkePath);
            EnsureDefaultAdminAudit(lozinkeaPath);

            return true;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
        }
    }

    public static string? ResolveLozinkeTablePath(string? rootPath)
        => ResolveTablePath(rootPath, "lozinke");

    public static string? ResolveLozinkeaTablePath(string? rootPath)
        => ResolveTablePath(rootPath, "lozinkea");

    public static void EnsureDataFoldersForOperator(string rootPath, int operatorId)
    {
        if (operatorId < 1 || operatorId > 99)
            return;

        var root = NormalizeRootPath(rootPath);
        if (string.IsNullOrWhiteSpace(root))
            return;

        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "data01"));
        Directory.CreateDirectory(Path.Combine(root, "01"));

        var suffix = operatorId.ToString("00", CultureInfo.InvariantCulture);
        var dataTarget = Path.Combine(root, $"data{suffix}");
        var numericTarget = Path.Combine(root, suffix);

        var dataTemplate = Directory.Exists(Path.Combine(root, "data01"))
            ? Path.Combine(root, "data01")
            : null;

        var numericTemplate = Directory.Exists(Path.Combine(root, "01"))
            ? Path.Combine(root, "01")
            : dataTemplate;

        EnsureFolderFromTemplate(dataTarget, dataTemplate);
        EnsureFolderFromTemplate(numericTarget, numericTemplate);
    }

    public static bool TryAcquireUserSessionLock(
        string rootPath,
        string korisnikIme,
        out IDisposable? handle,
        out string message)
    {
        handle = null;
        message = string.Empty;

        var root = NormalizeRootPath(rootPath);
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return true;

        var lockBasePath = Path.Combine(GetData00Path(root), "LOZINKE.DBF");
        var key = $"SESSION_{SanitizeIdentifier(korisnikIme)}";
        var owner = string.IsNullOrWhiteSpace(korisnikIme) ? Environment.UserName : korisnikIme.Trim();

        if (!DbfOptimisticConcurrency.TryAcquireRecordLock(lockBasePath, key, owner, out var lockHandle, out var rawMessage))
        {
            message = $"Korisnik '{owner}' je vec prijavljen sa drugog racunara. {rawMessage}";
            return false;
        }

        handle = lockHandle;
        return true;
    }

    private static string? ResolveTablePath(string? rootPath, string tableBaseName)
    {
        if (!IsValidWorkspaceRoot(rootPath))
            return null;

        var root = NormalizeRootPath(rootPath!);
        var tableFileName = $"{tableBaseName}.dbf";

        var data00 = Path.Combine(root, "data00");
        var direct = FindCaseInsensitiveFile(data00, tableFileName);
        if (!string.IsNullOrWhiteSpace(direct))
            return direct;

        var databaze = Path.Combine(root, "databaze");
        direct = FindCaseInsensitiveFile(databaze, tableFileName);
        if (!string.IsNullOrWhiteSpace(direct))
            return direct;

        direct = FindCaseInsensitiveFile(root, tableFileName);
        if (!string.IsNullOrWhiteSpace(direct))
            return direct;

        foreach (var firma in Directory.GetDirectories(root, "F*", SearchOption.TopDirectoryOnly))
        {
            if (!FirmaFolderRegex.IsMatch(Path.GetFileName(firma)))
                continue;

            direct = FindCaseInsensitiveFile(firma, tableFileName);
            if (!string.IsNullOrWhiteSpace(direct))
                return direct;
        }

        return null;
    }

    private static void EnsureTableExists(
        string rootPath,
        string targetDbfPath,
        string tableBaseName,
        IReadOnlyList<DbfFieldDef> fallbackFields)
    {
        if (File.Exists(targetDbfPath))
            return;

        var existingPath = ResolveTablePath(rootPath, tableBaseName);
        if (!string.IsNullOrWhiteSpace(existingPath))
        {
            CopyDbfFamily(existingPath, targetDbfPath);
            return;
        }

        var templatePath = FindTemplateTablePath(rootPath, tableBaseName);
        if (!string.IsNullOrWhiteSpace(templatePath))
        {
            CopyDbfFamily(templatePath, targetDbfPath);
            return;
        }

        CreateMinimalDbf(targetDbfPath, fallbackFields);
    }

    private static string? FindTemplateTablePath(string rootPath, string tableBaseName)
    {
        var fileName = $"{tableBaseName}.dbf";
        var candidates = new List<string>();

        var root = NormalizeRootPath(rootPath);
        if (!string.IsNullOrWhiteSpace(root))
        {
            candidates.Add(Path.Combine(root, "data00", fileName));
            candidates.Add(Path.Combine(root, "databaze", fileName));
            candidates.Add(Path.Combine(root, "F1", fileName));
        }

        var baseDir = AppContext.BaseDirectory;
        candidates.Add(Path.Combine(baseDir, "data00", fileName));
        candidates.Add(Path.Combine(baseDir, "databaze", fileName));
        candidates.Add(Path.Combine(baseDir, "old-project", "data00", fileName));
        candidates.Add(Path.Combine(baseDir, "old-project", "databaze", fileName));

        var dir = new DirectoryInfo(baseDir);
        while (dir is not null)
        {
            candidates.Add(Path.Combine(dir.FullName, "old-project", "data00", fileName));
            candidates.Add(Path.Combine(dir.FullName, "old-project", "databaze", fileName));
            dir = dir.Parent;
        }

        return candidates.FirstOrDefault(File.Exists);
    }

    private static void EnsureDefaultAdminUser(string lozinkePath)
    {
        var existingRows = SafeReadRows(lozinkePath);
        if (existingRows.Count > 0)
            return;

        var schema = DbfTableWriter.LoadSchema(lozinkePath);
        var admin = CreateDefaultAdminRow();

        DbfTableWriter.WriteTable(
            lozinkePath,
            schema,
            [admin],
            static (row, fieldName) => row.TryGetValue(fieldName, out var value) ? value : null);
    }

    private static void EnsureDefaultAdminAudit(string lozinkeaPath)
    {
        var existingRows = SafeReadRows(lozinkeaPath);
        if (existingRows.Count > 0)
            return;

        var schema = DbfTableWriter.LoadSchema(lozinkeaPath);
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["PAS"] = "01",
            ["KORISNIK"] = "admin",
            ["AKTIVAN"] = "D",
            ["DATUM"] = DateTime.Today,
            ["VREME0"] = string.Empty,
            ["VREME1"] = string.Empty,
            ["PRENETO"] = " ",
            ["IDBR"] = 1m,
        };

        DbfTableWriter.WriteTable(
            lozinkeaPath,
            schema,
            [row],
            static (record, fieldName) => record.TryGetValue(fieldName, out var value) ? value : null);
    }

    private static Dictionary<string, object?> CreateDefaultAdminRow()
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["PAS"] = "01",
            ["KORISNIK"] = "admin",
            ["LOZINKA"] = "admin",
            ["KORIME"] = "Administrator",
            ["AKTIVAN"] = "D",
            ["OPERATER"] = "1",
            ["PASSNIVO"] = 1m,
            ["PASSGK"] = true,
            ["PASSAN"] = true,
            ["PASSBL"] = true,
            ["PASSTV"] = true,
            ["PASSTM"] = true,
            ["PASSUS"] = true,
            ["PASSLD"] = true,
            ["PASSOST"] = true,
            ["PASSPRN"] = true,
            ["PASSPRO"] = true,
            ["PASSOS"] = true,
            ["PASSPROF"] = true,
            ["PASSDEL"] = true,
            ["PASSTVRA"] = true,
            ["PASSTVKAL"] = true,
            ["PASSTVRAC"] = true,
            ["PASSTVNIV"] = true,
            ["PASSTMRA"] = true,
            ["PASSTMKAL"] = true,
            ["PASSTMRAC"] = true,
            ["PASSTMNIV"] = true,
            ["DATUM"] = DateTime.Today,
            ["VREME0"] = string.Empty,
            ["VREME1"] = string.Empty,
            ["SLIKE"] = string.Empty,
            ["MAGACIN"] = 0m,
            ["PUTANJA"] = string.Empty,
            ["FOXY"] = string.Empty,
            ["PDFPRINT"] = string.Empty,
            ["PRENETO"] = " ",
            ["IDBR"] = 1m,
        };
    }

    private static List<Dictionary<string, object?>> SafeReadRows(string dbfPath)
    {
        try
        {
            return DbfReader.CitajSveZapise(dbfPath)
                .Select(row => new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static void EnsureFolderFromTemplate(string targetFolder, string? templateFolder)
    {
        if (Directory.Exists(targetFolder))
            return;

        Directory.CreateDirectory(targetFolder);

        if (string.IsNullOrWhiteSpace(templateFolder) || !Directory.Exists(templateFolder))
            return;

        CopyDirectoryContent(templateFolder, targetFolder);
    }

    private static void CopyDirectoryContent(string sourceFolder, string targetFolder)
    {
        Directory.CreateDirectory(targetFolder);

        foreach (var filePath in Directory.GetFiles(sourceFolder, "*", SearchOption.TopDirectoryOnly))
        {
            var targetFile = Path.Combine(targetFolder, Path.GetFileName(filePath));
            File.Copy(filePath, targetFile, overwrite: true);
        }

        foreach (var sourceSubFolder in Directory.GetDirectories(sourceFolder, "*", SearchOption.TopDirectoryOnly))
        {
            var targetSubFolder = Path.Combine(targetFolder, Path.GetFileName(sourceSubFolder));
            CopyDirectoryContent(sourceSubFolder, targetSubFolder);
        }
    }

    private static void CopyDbfFamily(string sourceDbfPath, string targetDbfPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetDbfPath)!);

        var sourceBase = Path.Combine(Path.GetDirectoryName(sourceDbfPath)!, Path.GetFileNameWithoutExtension(sourceDbfPath));
        var targetBase = Path.Combine(Path.GetDirectoryName(targetDbfPath)!, Path.GetFileNameWithoutExtension(targetDbfPath));

        foreach (var ext in SidecarExtensions)
        {
            var sourceFile = sourceBase + ext;
            if (!File.Exists(sourceFile))
                continue;

            File.Copy(sourceFile, targetBase + ext, overwrite: true);
        }
    }

    private static string? FindCaseInsensitiveFile(string? directory, string fileName)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return null;

        return Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(path =>
                string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase));
    }

    private static string SanitizeIdentifier(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "KORISNIK";

        var buffer = new StringBuilder(input.Length);
        foreach (var ch in input.Trim())
        {
            buffer.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        }

        return buffer.Length == 0 ? "KORISNIK" : buffer.ToString();
    }

    private static void CreateMinimalDbf(string targetPath, IReadOnlyList<DbfFieldDef> fields)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        var recordLength = 1 + fields.Sum(field => field.Length);
        var headerLength = 32 + fields.Count * 32 + 1;

        using var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        var now = DateTime.Now;
        writer.Write((byte)0x03); // dBASE III without memo
        writer.Write((byte)(now.Year - 1900));
        writer.Write((byte)now.Month);
        writer.Write((byte)now.Day);
        writer.Write((uint)0); // broj zapisa
        writer.Write((ushort)headerLength);
        writer.Write((ushort)recordLength);
        writer.Write(new byte[16]); // rezervisano
        writer.Write((byte)0x00);   // table flags
        writer.Write((byte)0xC8);   // CP1250
        writer.Write((ushort)0x0000);

        foreach (var field in fields)
        {
            var descriptor = new byte[32];
            var nameBytes = Encoding.ASCII.GetBytes(field.Name.Trim().ToUpperInvariant());
            var copyLength = Math.Min(nameBytes.Length, 11);
            Array.Copy(nameBytes, descriptor, copyLength);
            descriptor[11] = (byte)field.Type;
            descriptor[16] = field.Length;
            descriptor[17] = field.Decimals;
            writer.Write(descriptor);
        }

        writer.Write((byte)0x0D); // kraj opisa polja
        writer.Write((byte)0x1A); // EOF marker
    }

    private readonly record struct DbfFieldDef(string Name, char Type, byte Length, byte Decimals = 0);
}
