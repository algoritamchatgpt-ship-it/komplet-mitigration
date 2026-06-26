using System.IO;
using System.Text;
using Algoritam.Core.Services.Dbf;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

string dbfDir = @"C:\Workspace\mitigration\src\GlavnaKnjiga\NALTEMPLATE";
var dbfs = Directory.GetFiles(dbfDir, "*.dbf");

foreach (var path in dbfs.OrderBy(x => x))
{
    string name = Path.GetFileName(path);
    Console.WriteLine($"=== {name} ===");
    try
    {
        var reader = new SimpleDbfReader(path);
        Console.WriteLine($"Records: {reader.RecordCount}, Fields: {reader.Fields.Count}");
        foreach (var f in reader.Fields)
        {
            Console.WriteLine($"  {f.Name} ({f.Type}, len={f.Length}, dec={f.Decimals})");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: {ex.Message}");
    }
    Console.WriteLine();
}
