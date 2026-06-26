using Algoritam.Application;

namespace Algoritam.Application.Tests;

public class ZaradePathsTests
{
    [Fact]
    public void GetFirmaZaradeFolder_VracaZaradePodfolder()
    {
        var result = ZaradePaths.GetFirmaZaradeFolder(@"C:\FIN\F1");

        Assert.Equal(@"C:\FIN\F1\zarade", result);
    }

    [Fact]
    public void GetDbPath_VracaAlgoritmDbUZaradePodfolderu()
    {
        var result = ZaradePaths.GetDbPath(@"C:\FIN\F1");

        Assert.Equal(@"C:\FIN\F1\zarade\algoritam.db", result);
    }

    [Fact]
    public void ZaradeFolderName_KonstantaJeZarade()
    {
        Assert.Equal("zarade", ZaradePaths.ZaradeFolderName);
    }
}
