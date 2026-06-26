using Algoritam.Infrastructure.Migration;
using Algoritam.Infrastructure.Services;

namespace Algoritam.Infrastructure.Tests;

public class FoxWorkspaceSupportTests : IDisposable
{
    private readonly string _tmpRoot;

    public FoxWorkspaceSupportTests()
    {
        _tmpRoot = Path.Combine(Path.GetTempPath(), $"algws_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tmpRoot);
    }

    public void Dispose() => Directory.Delete(_tmpRoot, recursive: true);

    // ── FoxWorkspaceSupport.IsFirmaUnderRoot ──────────────────────────

    [Fact]
    public void IsFirmaUnderRoot_FirmaJePodfolder_VracaTrue()
    {
        var result = FoxWorkspaceSupport.IsFirmaUnderRoot(
            Path.Combine(_tmpRoot, "F1"),
            _tmpRoot);

        Assert.True(result);
    }

    [Fact]
    public void IsFirmaUnderRoot_FirmaJeRootSam_VracaTrue()
    {
        var result = FoxWorkspaceSupport.IsFirmaUnderRoot(_tmpRoot, _tmpRoot);

        Assert.True(result);
    }

    [Fact]
    public void IsFirmaUnderRoot_FirmaJeVanRoota_VracaFalse()
    {
        var result = FoxWorkspaceSupport.IsFirmaUnderRoot(
            @"C:\NestoSasvimDrugo\F1",
            _tmpRoot);

        Assert.False(result);
    }

    [Fact]
    public void IsFirmaUnderRoot_PraznaFirma_VracaFalse()
    {
        var result = FoxWorkspaceSupport.IsFirmaUnderRoot("", _tmpRoot);

        Assert.False(result);
    }

    // ── FoxWorkspaceSupport.ListFirmaFolders ─────────────────────────

    [Fact]
    public void ListFirmaFolders_PostojeF1F2_VracaOba()
    {
        Directory.CreateDirectory(Path.Combine(_tmpRoot, "F1"));
        Directory.CreateDirectory(Path.Combine(_tmpRoot, "F2"));

        var result = FoxWorkspaceSupport.ListFirmaFolders(_tmpRoot);

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Matches(@"[Ff]\d+$", Path.GetFileName(p)));
    }

    [Fact]
    public void ListFirmaFolders_NemaFoldera_VracaPraznu()
    {
        var result = FoxWorkspaceSupport.ListFirmaFolders(_tmpRoot);

        Assert.Empty(result);
    }

    [Fact]
    public void ListFirmaFolders_PostojiTekstFolder_NijeUkljucen()
    {
        Directory.CreateDirectory(Path.Combine(_tmpRoot, "F1"));
        Directory.CreateDirectory(Path.Combine(_tmpRoot, "podatci"));  // ne odgovara F\d+

        var result = FoxWorkspaceSupport.ListFirmaFolders(_tmpRoot);

        Assert.Single(result);
    }

    // ── FoxWorkspaceSupport.GetNextFirmaFolder ────────────────────────

    [Fact]
    public void GetNextFirmaFolder_PostojiF1_VracaF2()
    {
        Directory.CreateDirectory(Path.Combine(_tmpRoot, "F1"));

        var result = FoxWorkspaceSupport.GetNextFirmaFolder(_tmpRoot);

        Assert.Equal(Path.Combine(_tmpRoot, "F2"), result);
    }

    [Fact]
    public void GetNextFirmaFolder_PrazanRoot_VracaF1()
    {
        var result = FoxWorkspaceSupport.GetNextFirmaFolder(_tmpRoot);

        Assert.Equal(Path.Combine(_tmpRoot, "F1"), result);
    }

    // ── FinWorkspaceResolver.NormalizeRootPath ────────────────────────

    [Fact]
    public void NormalizeRootPath_PutanjaJeF1Podfolder_VracaRoditelja()
    {
        var f1 = Path.Combine(_tmpRoot, "F1");
        Directory.CreateDirectory(f1);

        var result = FinWorkspaceResolver.NormalizeRootPath(f1);

        Assert.Equal(Path.GetFullPath(_tmpRoot), result);
    }

    [Fact]
    public void NormalizeRootPath_PutanjaJeData00Podfolder_VracaRoditelja()
    {
        var data00 = Path.Combine(_tmpRoot, "data00");
        Directory.CreateDirectory(data00);

        var result = FinWorkspaceResolver.NormalizeRootPath(data00);

        Assert.Equal(Path.GetFullPath(_tmpRoot), result);
    }

    [Fact]
    public void NormalizeRootPath_PutanjaJeObicniFolder_VracaIstu()
    {
        var result = FinWorkspaceResolver.NormalizeRootPath(_tmpRoot);

        Assert.Equal(Path.GetFullPath(_tmpRoot), result);
    }

    [Fact]
    public void NormalizeRootPath_PrazanString_VracaPrazanString()
    {
        var result = FinWorkspaceResolver.NormalizeRootPath("");

        Assert.Equal(string.Empty, result);
    }

    // ── FinWorkspaceResolver.IsValidWorkspaceRoot ─────────────────────

    [Fact]
    public void IsValidWorkspaceRoot_PostojeciFolder_VracaTrue()
    {
        var result = FinWorkspaceResolver.IsValidWorkspaceRoot(_tmpRoot);

        Assert.True(result);
    }

    [Fact]
    public void IsValidWorkspaceRoot_NepostojeciFolder_VracaFalse()
    {
        var result = FinWorkspaceResolver.IsValidWorkspaceRoot(
            Path.Combine(_tmpRoot, "nepostoji_xyz"));

        Assert.False(result);
    }

    [Fact]
    public void IsValidWorkspaceRoot_Null_VracaFalse()
    {
        var result = FinWorkspaceResolver.IsValidWorkspaceRoot(null);

        Assert.False(result);
    }
}
