using Algoritam.Infrastructure.Dbf;

namespace Algoritam.Infrastructure.Tests;

public class DbfOptimisticConcurrencyTests
{
    [Fact]
    public void ComputeRecordSignature_KadaJeRazlicitRedosledPolja_VracaIstiPotpis()
    {
        var a = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["BROJ"] = 10,
            ["IME"] = "PERA"
        };

        var b = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["IME"] = "PERA",
            ["BROJ"] = 10
        };

        var potpisA = DbfOptimisticConcurrency.ComputeRecordSignature(a);
        var potpisB = DbfOptimisticConcurrency.ComputeRecordSignature(b);

        Assert.Equal(potpisA, potpisB);
    }

    [Fact]
    public void HasFileChanged_KadaSeFajlPromeni_VracaTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "algoritam-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "test.dbf");
        File.WriteAllText(path, "A");

        var snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(path);
        Assert.False(DbfOptimisticConcurrency.HasFileChanged(path, snapshot));

        File.WriteAllText(path, "AA");
        Assert.True(DbfOptimisticConcurrency.HasFileChanged(path, snapshot));
    }

    [Fact]
    public void TryAcquireRecordLock_DokJeZakljucan_DrugoZakljucavanjeNeuspesno()
    {
        var dir = Path.Combine(Path.GetTempPath(), "algoritam-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "test.dbf");
        File.WriteAllText(path, "x");

        var prvi = DbfOptimisticConcurrency.TryAcquireRecordLock(
            path,
            "BROJ:1",
            "korisnik1",
            out var prviHandle,
            out _);
        Assert.True(prvi);
        Assert.NotNull(prviHandle);

        var drugi = DbfOptimisticConcurrency.TryAcquireRecordLock(
            path,
            "BROJ:1",
            "korisnik2",
            out var drugiHandle,
            out _);

        Assert.False(drugi);
        Assert.Null(drugiHandle);

        prviHandle!.Dispose();

        var treci = DbfOptimisticConcurrency.TryAcquireRecordLock(
            path,
            "BROJ:1",
            "korisnik3",
            out var treciHandle,
            out _);
        Assert.True(treci);
        treciHandle!.Dispose();
    }
}
