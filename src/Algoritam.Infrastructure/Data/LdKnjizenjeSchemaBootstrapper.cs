using Microsoft.EntityFrameworkCore;

namespace Algoritam.Infrastructure.Data;

/// <summary>
/// Kreira SQL tabele za knjiženje zarada ako ne postoje.
/// </summary>
public static class LdKnjizenjeSchemaBootstrapper
{
    private const string CreateSablonSql = """
        CREATE TABLE IF NOT EXISTS "LdKontoSablonStavke" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_LdKontoSablonStavke" PRIMARY KEY AUTOINCREMENT,
            "Vrsta" TEXT NOT NULL,
            "Kod" TEXT NOT NULL,
            "Opis" TEXT NOT NULL,
            "Konto" TEXT NOT NULL,
            "Kontop" TEXT NOT NULL,
            "Preneto" TEXT NOT NULL,
            "Idbr" INTEGER NOT NULL
        );
        """;

    private const string CreateKnjizenjeSql = """
        CREATE TABLE IF NOT EXISTS "LdKnjizenjeStavke" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_LdKnjizenjeStavke" PRIMARY KEY AUTOINCREMENT,
            "Vrsta" TEXT NOT NULL,
            "Kod" TEXT NOT NULL,
            "Opis" TEXT NOT NULL,
            "Konto" TEXT NOT NULL,
            "Kontop" TEXT NOT NULL,
            "Iznos" TEXT NOT NULL,
            "Datdok" TEXT NULL,
            "Brnal" TEXT NOT NULL,
            "Mp" TEXT NOT NULL,
            "Mtr" INTEGER NOT NULL,
            "Preneto" TEXT NOT NULL,
            "Idbr" INTEGER NOT NULL
        );
        """;

    public static void Ensure(FirmaDbContext context)
    {
        context.Database.ExecuteSqlRaw(CreateSablonSql);
        context.Database.ExecuteSqlRaw(CreateKnjizenjeSql);
    }

    public static async Task EnsureAsync(FirmaDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(CreateSablonSql);
        await context.Database.ExecuteSqlRawAsync(CreateKnjizenjeSql);
    }
}
