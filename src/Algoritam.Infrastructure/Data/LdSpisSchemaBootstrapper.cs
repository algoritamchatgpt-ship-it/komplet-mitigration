using Microsoft.EntityFrameworkCore;

namespace Algoritam.Infrastructure.Data;

/// <summary>
/// Kreira LdSpisStavke tabelu u postojećim bazama ako ne postoji.
/// </summary>
public static class LdSpisSchemaBootstrapper
{
    private const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS "LdSpisStavke" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_LdSpisStavke" PRIMARY KEY AUTOINCREMENT,
            "Broj" INTEGER NOT NULL,
            "ImePrez" TEXT NOT NULL,
            "Partija" TEXT NOT NULL,
            "Iznos" TEXT NOT NULL,
            "Sifra" TEXT NOT NULL,
            "Preneto" TEXT NOT NULL,
            "Idbr" INTEGER NOT NULL
        );
        """;

    public static void Ensure(FirmaDbContext context)
    {
        context.Database.ExecuteSqlRaw(CreateTableSql);
    }

    public static Task EnsureAsync(FirmaDbContext context)
    {
        return context.Database.ExecuteSqlRawAsync(CreateTableSql);
    }
}
