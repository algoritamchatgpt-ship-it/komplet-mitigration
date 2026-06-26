using Algoritam.Application;
using Algoritam.Infrastructure.Migration;
using Microsoft.Data.Sqlite;

namespace Algoritam.Infrastructure.Tests;

public class DbfToSqliteMigratorTests
{
    [Fact]
    public async Task MigrujFirmuAsync_PrazanFolder_KreiraBazuSaTabelama()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), $"algtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            await new DbfToSqliteMigrator().MigrujFirmuAsync(tmpDir);

            var dbPath = ZaradePaths.GetDbPath(tmpDir);
            Assert.True(File.Exists(dbPath), $"SQLite baza nije kreirana: {dbPath}");
            Assert.True(new FileInfo(dbPath).Length > 0, "SQLite baza je prazna");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public async Task MigrujFirmuAsync_FolderNijePronadjen_BacaDirectoryNotFoundException()
    {
        var nepostojeci = Path.Combine(Path.GetTempPath(), $"algtest_ne_{Guid.NewGuid():N}");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => new DbfToSqliteMigrator().MigrujFirmuAsync(nepostojeci));
    }

    [Fact]
    public void FoxDataSync_TryEnsureFresh_NepostojeFajlovi_VracaFalse()
    {
        var result = FoxDataSync.TryEnsureFresh("", out var error);

        Assert.False(result);
    }
}
