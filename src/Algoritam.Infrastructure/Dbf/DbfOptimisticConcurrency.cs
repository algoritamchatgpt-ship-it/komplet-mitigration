using System.Globalization;
using System.Text;

namespace Algoritam.Infrastructure.Dbf;

public static class DbfOptimisticConcurrency
{
    private static readonly TimeSpan DefaultStaleLockAge = TimeSpan.FromHours(8);

    public sealed record FileSnapshot(DateTime LastWriteUtc, long Length);

    public static FileSnapshot CaptureFileSnapshot(string path)
    {
        var info = new FileInfo(path);
        return new FileSnapshot(info.LastWriteTimeUtc, info.Exists ? info.Length : 0L);
    }

    public static bool HasFileChanged(string path, FileSnapshot snapshot)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
            return snapshot.Length != 0L || snapshot.LastWriteUtc != DateTime.MinValue;

        return info.LastWriteTimeUtc != snapshot.LastWriteUtc || info.Length != snapshot.Length;
    }

    public static string ComputeRecordSignature(IReadOnlyDictionary<string, object?> record)
    {
        var pairs = record
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kvp => $"{kvp.Key.ToUpperInvariant()}={NormalizeValue(kvp.Value)}");

        return string.Join("|", pairs);
    }

    public static bool TryAcquireRecordLock(
        string dbfPath,
        string recordKey,
        string ownerLabel,
        out RecordLockHandle? handle,
        out string message)
    {
        handle = null;
        message = string.Empty;

        if (string.IsNullOrWhiteSpace(dbfPath))
        {
            message = "Putanja DBF fajla nije validna.";
            return false;
        }

        var lockFilePath = BuildLockFilePath(dbfPath, recordKey);
        var lockDirectory = Path.GetDirectoryName(lockFilePath);
        if (!string.IsNullOrWhiteSpace(lockDirectory))
            Directory.CreateDirectory(lockDirectory);

        var owner = string.IsNullOrWhiteSpace(ownerLabel) ? "nepoznat korisnik" : ownerLabel.Trim();
        var token = Guid.NewGuid().ToString("N");
        var payload = $"{token}\n{owner}\n{Environment.MachineName}\n{DateTime.UtcNow:O}\n{Environment.ProcessId}";

        TryCleanupStaleLock(lockFilePath, DefaultStaleLockAge);

        try
        {
            using var stream = new FileStream(lockFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(payload);
            writer.Flush();

            handle = new RecordLockHandle(lockFilePath, token);
            message = "Zakljucavanje uspesno kreirano.";
            return true;
        }
        catch (IOException)
        {
            message = BuildLockConflictMessage(recordKey, lockFilePath);
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            message = $"Nema dozvole za kreiranje lock fajla: {lockFilePath}";
            return false;
        }
    }

    private static void TryCleanupStaleLock(string lockFilePath, TimeSpan staleAge)
    {
        if (!File.Exists(lockFilePath))
            return;

        try
        {
            var lines = File.ReadAllLines(lockFilePath);

            // Novi format: linija 5 sadrži PID — proveri da li je proces još živ
            if (lines.Length >= 5 && int.TryParse(lines[4].Trim(), out var pid))
            {
                var machine = lines.Length >= 3 ? lines[2].Trim() : string.Empty;
                var isLocalMachine = string.Equals(machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                                     || string.IsNullOrWhiteSpace(machine);
                if (isLocalMachine && !IsProcessRunning(pid))
                {
                    File.Delete(lockFilePath);
                    return;
                }
            }
            else
            {
                // Stari format bez PID-a: ako je sa ovog računara, obriši odmah (proces sigurno nije aktivan)
                var machine = lines.Length >= 3 ? lines[2].Trim() : string.Empty;
                var isLocalMachine = string.Equals(machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                                     || string.IsNullOrWhiteSpace(machine);
                if (isLocalMachine)
                {
                    File.Delete(lockFilePath);
                    return;
                }
            }

            // Fallback: obrisi ako je lock star vise od staleAge
            var info = new FileInfo(lockFilePath);
            if (DateTime.UtcNow - info.LastWriteTimeUtc > staleAge)
                File.Delete(lockFilePath);
        }
        catch
        {
            // Ako ne mozemo da obrisemo stale lock, ostavljamo ga pa ce korisnik dobiti poruku.
        }
    }

    private static bool IsProcessRunning(int pid)
    {
        try
        {
            var proc = System.Diagnostics.Process.GetProcessById(pid);
            return !proc.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildLockConflictMessage(string recordKey, string lockFilePath)
    {
        if (!File.Exists(lockFilePath))
            return $"Zapis '{recordKey}' je trenutno zakljucan od strane drugog korisnika.";

        try
        {
            var lines = File.ReadAllLines(lockFilePath);
            var owner = lines.Length > 1 ? lines[1].Trim() : "nepoznat korisnik";
            var machine = lines.Length > 2 ? lines[2].Trim() : "nepoznat racunar";
            var timestampRaw = lines.Length > 3 ? lines[3].Trim() : string.Empty;
            var when = DateTime.TryParse(timestampRaw, null, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.CurrentCulture)
                : "nepoznato vreme";

            return $"Zapis '{recordKey}' trenutno menja korisnik '{owner}' ({machine}, {when}). Pokusajte ponovo kasnije.";
        }
        catch
        {
            return $"Zapis '{recordKey}' je trenutno zakljucan od strane drugog korisnika.";
        }
    }

    private static string BuildLockFilePath(string dbfPath, string recordKey)
    {
        var directory = Path.GetDirectoryName(dbfPath) ?? ".";
        var dbfName = Path.GetFileName(dbfPath);
        var normalizedKey = string.Concat((recordKey ?? string.Empty)
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
        if (string.IsNullOrWhiteSpace(normalizedKey))
            normalizedKey = "RECORD";

        var lockDir = Path.Combine(directory, ".algoritam-locks");
        return Path.Combine(lockDir, $"{dbfName}.{normalizedKey}.lck");
    }

    private static string NormalizeValue(object? value)
    {
        if (value is null)
            return "<null>";

        return value switch
        {
            DateTime dt => dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }
}

public sealed class RecordLockHandle : IDisposable
{
    private readonly string _lockFilePath;
    private readonly string _token;
    private bool _released;

    internal RecordLockHandle(string lockFilePath, string token)
    {
        _lockFilePath = lockFilePath;
        _token = token;
    }

    public void Release()
    {
        if (_released)
            return;

        _released = true;

        try
        {
            if (!File.Exists(_lockFilePath))
                return;

            var lines = File.ReadAllLines(_lockFilePath);
            var token = lines.Length > 0 ? lines[0].Trim() : string.Empty;
            if (!string.Equals(token, _token, StringComparison.Ordinal))
                return;

            File.Delete(_lockFilePath);
        }
        catch
        {
            // Zakljucavanje je best-effort; ne propagiramo gresku pri otpustanju.
        }
    }

    public void Dispose() => Release();
}
