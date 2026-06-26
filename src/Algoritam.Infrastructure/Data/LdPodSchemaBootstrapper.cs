using Microsoft.EntityFrameworkCore;

namespace Algoritam.Infrastructure.Data;

/// <summary>
/// Kreira LdPodStavke tabelu u postojećim bazama ako ne postoji.
/// </summary>
public static class LdPodSchemaBootstrapper
{
    private const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS "LdPodStavke" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_LdPodStavke" PRIMARY KEY AUTOINCREMENT,
            "Kod" TEXT NOT NULL,
            "Opis" TEXT NOT NULL,
            "S1a" TEXT NOT NULL,
            "Sv1a" TEXT NOT NULL,
            "S1b" TEXT NOT NULL,
            "Sv1b" TEXT NOT NULL,
            "S1c" TEXT NOT NULL,
            "Sv1c" TEXT NOT NULL,
            "S1u" TEXT NOT NULL,
            "Sv1u" TEXT NOT NULL,
            "S2a" TEXT NOT NULL,
            "Sv2a" TEXT NOT NULL,
            "S2b" TEXT NOT NULL,
            "Sv2b" TEXT NOT NULL,
            "S2c" TEXT NOT NULL,
            "Sv2c" TEXT NOT NULL,
            "S2u" TEXT NOT NULL,
            "Sv2u" TEXT NOT NULL,
            "S3a" TEXT NOT NULL,
            "Sv3a" TEXT NOT NULL,
            "S3b" TEXT NOT NULL,
            "Sv3b" TEXT NOT NULL,
            "S3c" TEXT NOT NULL,
            "Sv3c" TEXT NOT NULL,
            "S3u" TEXT NOT NULL,
            "Sv3u" TEXT NOT NULL,
            "S4a" TEXT NOT NULL,
            "Sv4a" TEXT NOT NULL,
            "S4b" TEXT NOT NULL,
            "Sv4b" TEXT NOT NULL,
            "S4c" TEXT NOT NULL,
            "Sv4c" TEXT NOT NULL,
            "S4u" TEXT NOT NULL,
            "Sv4u" TEXT NOT NULL,
            "Su" TEXT NOT NULL,
            "Svu" TEXT NOT NULL,
            "Mesec" INTEGER NOT NULL,
            "Isplata" INTEGER NOT NULL,
            "Vrsta" TEXT NOT NULL,
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
