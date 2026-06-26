using Algoritam.Application;
using Algoritam.Infrastructure.Migration;
using System.Diagnostics;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("Upotreba: dbfmigrator <firma-folder> [opcije]");
    Console.WriteLine();
    Console.WriteLine("Argumenti:");
    Console.WriteLine("  <firma-folder>   Putanja do foldera firme (npr. C:\\FIN\\F1)");
    Console.WriteLine();
    Console.WriteLine("Opcije:");
    Console.WriteLine("  --dry-run        Proverava fajlove bez kreiranja baze");
    Console.WriteLine("  -h, --help       Prikazuje ovu poruku");
    Console.WriteLine();
    Console.WriteLine("Izlaz:");
    Console.WriteLine("  SQLite baza se kreira u <firma-folder>\\zarade\\algoritam.db");
    return 0;
}

var firmaFolder = args[0];
var dryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);

if (!Directory.Exists(firmaFolder))
{
    Console.Error.WriteLine($"Greška: folder ne postoji: {firmaFolder}");
    return 1;
}

var dbPath = ZaradePaths.GetDbPath(firmaFolder);
Console.WriteLine($"Izvor : {Path.GetFullPath(firmaFolder)}");
Console.WriteLine($"Izlaz : {dbPath}");
Console.WriteLine();

if (dryRun)
{
    var dbfovi = Directory.GetFiles(firmaFolder, "*.dbf", SearchOption.TopDirectoryOnly);
    Console.WriteLine($"[dry-run] Pronađeno {dbfovi.Length} DBF fajl(ova) u folderu.");
    foreach (var f in dbfovi.OrderBy(x => x))
        Console.WriteLine($"  {Path.GetFileName(f)}");
    Console.WriteLine();
    Console.WriteLine("[dry-run] Migracija nije izvršena.");
    return 0;
}

Console.WriteLine("Pokretanje migracije...");
var sw = Stopwatch.StartNew();

try
{
    await new DbfToSqliteMigrator().MigrujFirmuAsync(firmaFolder);
    sw.Stop();

    var velicina = new FileInfo(dbPath).Length;
    Console.WriteLine($"Uspešno! Trajanje: {sw.Elapsed.TotalSeconds:F1}s, veličina: {velicina / 1024:N0} KB");
    Console.WriteLine($"Baza: {dbPath}");
    return 0;
}
catch (Exception ex)
{
    sw.Stop();
    Console.Error.WriteLine($"Greška: {ex.Message}");
    return 1;
}
