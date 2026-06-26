using Algoritam.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Algoritam.Infrastructure.Data;

/// <summary>
/// Obezbedjuje da tabela LdParametri postoji i u bazama koje su kreirane
/// pre dodavanja podrške za LDPARAM.
/// </summary>
public static class LdParametriSchemaBootstrapper
{
    private const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS "LdParametri" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_LdParametri" PRIMARY KEY,
            "RedniBr" INTEGER NOT NULL,
            "Mesec" INTEGER NOT NULL,
            "Nazmes" TEXT NOT NULL,
            "Redispl" INTEGER NOT NULL,
            "Dana" INTEGER NOT NULL,
            "Cmes" INTEGER NOT NULL,
            "Cpraz" INTEGER NOT NULL,
            "Czakon" INTEGER NOT NULL,
            "Procnoc" TEXT NOT NULL,
            "Procprod" TEXT NOT NULL,
            "Procpraz" TEXT NOT NULL,
            "Procned" TEXT NOT NULL,
            "Procmin" TEXT NOT NULL,
            "Procsus" TEXT NOT NULL,
            "Ekoefs" TEXT NOT NULL,
            "Minnac" INTEGER NOT NULL,
            "Procbol" TEXT NOT NULL,
            "Procplac" TEXT NOT NULL,
            "Prosbruto" TEXT NOT NULL,
            "Minimal" TEXT NOT NULL,
            "Ben1" TEXT NOT NULL,
            "Ben2" TEXT NOT NULL,
            "Ben3" TEXT NOT NULL,
            "Ben4" TEXT NOT NULL,
            "Isplata" INTEGER NOT NULL,
            "Sisplata" INTEGER NOT NULL,
            "Doppr1" TEXT NOT NULL,
            "Dopzr1" TEXT NOT NULL,
            "Dopnr1" TEXT NOT NULL,
            "Doppf1" TEXT NOT NULL,
            "Dopzf1" TEXT NOT NULL,
            "Dopnf1" TEXT NOT NULL,
            "Doppr2" TEXT NOT NULL,
            "Dopzr2" TEXT NOT NULL,
            "Dopnr2" TEXT NOT NULL,
            "Doppf2" TEXT NOT NULL,
            "Dopzf2" TEXT NOT NULL,
            "Dopnf2" TEXT NOT NULL,
            "Doppr3" TEXT NOT NULL,
            "Dopzr3" TEXT NOT NULL,
            "Dopnr3" TEXT NOT NULL,
            "Doppf3" TEXT NOT NULL,
            "Dopzf3" TEXT NOT NULL,
            "Dopnf3" TEXT NOT NULL,
            "Doppr4" TEXT NOT NULL,
            "Dopzr4" TEXT NOT NULL,
            "Dopnr4" TEXT NOT NULL,
            "Doppf4" TEXT NOT NULL,
            "Dopzf4" TEXT NOT NULL,
            "Dopnf4" TEXT NOT NULL,
            "Doppr5" TEXT NOT NULL,
            "Dopzr5" TEXT NOT NULL,
            "Dopnr5" TEXT NOT NULL,
            "Doppf5" TEXT NOT NULL,
            "Dopzf5" TEXT NOT NULL,
            "Dopnf5" TEXT NOT NULL,
            "Procpor" TEXT NOT NULL,
            "S1" TEXT NOT NULL,
            "Sdin1" TEXT NOT NULL,
            "S3" TEXT NOT NULL,
            "Sdin3" TEXT NOT NULL,
            "S4" TEXT NOT NULL,
            "Sdin4" TEXT NOT NULL,
            "S5" TEXT NOT NULL,
            "Sdin5" TEXT NOT NULL,
            "S6" TEXT NOT NULL,
            "Sdin6" TEXT NOT NULL,
            "S71" TEXT NOT NULL,
            "Sdin71" TEXT NOT NULL,
            "S72" TEXT NOT NULL,
            "Sdin72" TEXT NOT NULL,
            "S8" TEXT NOT NULL,
            "Sdin8" TEXT NOT NULL,
            "Komoraj" TEXT NOT NULL,
            "Komoras" TEXT NOT NULL,
            "Komorar" TEXT NOT NULL,
            "Smesec" INTEGER NOT NULL,
            "Snazmes" TEXT NOT NULL,
            "Sredispl" INTEGER NOT NULL,
            "Cenarada" TEXT NOT NULL,
            "Kd1" TEXT NOT NULL,
            "Kd4" TEXT NOT NULL,
            "Kd9" TEXT NOT NULL,
            "Kd12" TEXT NOT NULL,
            "Kd20" TEXT NOT NULL,
            "Kd22" TEXT NOT NULL,
            "Kd24" TEXT NOT NULL,
            "Kd25" TEXT NOT NULL,
            "Kd27" TEXT NOT NULL,
            "Kd28" TEXT NOT NULL,
            "Dat1" TEXT NULL,
            "Dat2" TEXT NULL,
            "Dat3" TEXT NULL,
            "Dat4" TEXT NULL,
            "Godina" TEXT NOT NULL,
            "Nazp1" TEXT NOT NULL,
            "Nazp2" TEXT NOT NULL,
            "Nazp3" TEXT NOT NULL,
            "Nazp4" TEXT NOT NULL,
            "Nazp5" TEXT NOT NULL,
            "Nazp5ter" TEXT NOT NULL,
            "Nazo1" TEXT NOT NULL,
            "Nazo2" TEXT NOT NULL,
            "Nazo3" TEXT NOT NULL,
            "Nazo4" TEXT NOT NULL,
            "Nazo5" TEXT NOT NULL,
            "Nazo6" TEXT NOT NULL,
            "Neoporez" TEXT NOT NULL,
            "Neoporezp" TEXT NOT NULL,
            "Decimale" TEXT NOT NULL,
            "Aktivrac" INTEGER NOT NULL,
            "Datpocdel" TEXT NULL,
            "Brzap0" INTEGER NOT NULL,
            "Brzap1" INTEGER NOT NULL,
            "Brzap2" INTEGER NOT NULL,
            "Srazpor" TEXT NOT NULL,
            "Dinsat" TEXT NOT NULL,
            "Tosat" TEXT NOT NULL,
            "Regsat" TEXT NOT NULL,
            "Konacna" TEXT NOT NULL,
            "Vrstaplate" TEXT NOT NULL,
            "Arhiva" TEXT NOT NULL,
            "Arhiva2" TEXT NOT NULL,
            "Datod" TEXT NULL,
            "Datdo" TEXT NULL,
            "Solporod1" TEXT NOT NULL,
            "Solpordo1" TEXT NOT NULL,
            "Solproc1" TEXT NOT NULL,
            "Solporod2" TEXT NOT NULL,
            "Solpordo2" TEXT NOT NULL,
            "Solproc2" TEXT NOT NULL,
            "Bkproc" TEXT NOT NULL,
            "Bkzastita" TEXT NOT NULL,
            "Bknacin" TEXT NOT NULL,
            "Nakpos" TEXT NOT NULL,
            "Preneto" TEXT NOT NULL,
            "Idbr" INTEGER NOT NULL,
            "Priprav" TEXT NOT NULL,
            "Priprav1" TEXT NOT NULL,
            "Priprav2" TEXT NOT NULL,
            "Najosn" TEXT NOT NULL DEFAULT '0'
        );
        """;

    public static void Ensure(FirmaDbContext context)
    {
        context.Database.ExecuteSqlRaw(CreateTableSql);
        EnsureColumns(context);
        EnsureSeedRow(context);
    }

    public static async Task EnsureAsync(FirmaDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(CreateTableSql);
        EnsureColumns(context);
        await EnsureSeedRowAsync(context);
    }

    private static void EnsureColumns(FirmaDbContext context)
    {
        try { context.Database.ExecuteSqlRaw("ALTER TABLE \"LdParametri\" ADD COLUMN \"Najosn\" TEXT NOT NULL DEFAULT '0'"); } catch { }
        context.Database.ExecuteSqlRaw("UPDATE \"LdParametri\" SET \"Najosn\" = '40143' WHERE \"Najosn\" = '0' OR \"Najosn\" = '' OR \"Najosn\" IS NULL");
    }

    private static void EnsureSeedRow(FirmaDbContext context)
    {
        if (context.LdParametri.Any())
            return;

        context.LdParametri.Add(LdParametarDefaults.Kreiraj());
        context.SaveChanges();
    }

    private static async Task EnsureSeedRowAsync(FirmaDbContext context)
    {
        if (await context.LdParametri.AnyAsync())
            return;

        context.LdParametri.Add(LdParametarDefaults.Kreiraj());
        await context.SaveChangesAsync();
    }
}
