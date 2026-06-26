using Algoritam.Application;
using Algoritam.Application.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Algoritam.Infrastructure.Services;

public class PutanjaService : IPutanjaService
{
    private static readonly string KonfigFajl = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FinZarade",
        "config.json");

    private static readonly Regex FirmaFolderRegex =
        new("^F\\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private string? _kesiranaPutanja;

    public string? DajFinPutanju()
    {
        if (_kesiranaPutanja != null)
            return _kesiranaPutanja;

        try
        {
            if (!File.Exists(KonfigFajl))
                return null;

            var json = File.ReadAllText(KonfigFajl);
            var config = JsonSerializer.Deserialize<FinConfig>(json);
            var putanja = config?.FinPutanja;
            if (string.IsNullOrWhiteSpace(putanja))
                return null;

            _kesiranaPutanja = FinWorkspaceResolver.NormalizeRootPath(putanja);
            return _kesiranaPutanja;
        }
        catch
        {
            return null;
        }
    }

    public bool SnimiFinPutanju(string putanja)
    {
        var normalized = FinWorkspaceResolver.NormalizeRootPath(putanja);
        if (!JeValidanFinFolder(normalized))
            return false;

        try
        {
            FinWorkspaceResolver.EnsureWorkspaceInitialized(normalized);
            KreirajZaradeFoldereZaFirme(normalized);

            Directory.CreateDirectory(Path.GetDirectoryName(KonfigFajl)!);

            var config = UcitajIliNapraviConfig();
            config.FinPutanja = normalized;

            File.WriteAllText(
                KonfigFajl,
                JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

            _kesiranaPutanja = normalized;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool JeValidanFinFolder(string putanja)
    {
        if (string.IsNullOrWhiteSpace(putanja))
            return false;

        var normalized = FinWorkspaceResolver.NormalizeRootPath(putanja);
        return Directory.Exists(normalized);
    }

    public bool PutanjaPostavljena =>
        DajFinPutanju() is string putanja && JeValidanFinFolder(putanja);

    public string? DajArhivaPutanju()
    {
        try
        {
            if (!File.Exists(KonfigFajl))
                return null;

            var json = File.ReadAllText(KonfigFajl);
            var config = JsonSerializer.Deserialize<FinConfig>(json);
            return config?.ArhivaPutanja;
        }
        catch
        {
            return null;
        }
    }

    public bool SnimiArhivaPutanju(string putanja)
    {
        try
        {
            var config = UcitajIliNapraviConfig();
            config.ArhivaPutanja = putanja;

            Directory.CreateDirectory(Path.GetDirectoryName(KonfigFajl)!);
            Directory.CreateDirectory(putanja);

            File.WriteAllText(
                KonfigFajl,
                JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void KreirajZaradeFoldereZaFirme(string finRoot)
    {
        foreach (var dir in Directory.GetDirectories(finRoot))
        {
            var name = Path.GetFileName(dir);
            if (!FirmaFolderRegex.IsMatch(name))
                continue;

            Directory.CreateDirectory(ZaradePaths.GetFirmaZaradeFolder(dir));
        }
    }

    private static FinConfig UcitajIliNapraviConfig()
    {
        if (!File.Exists(KonfigFajl))
            return new FinConfig();

        var text = File.ReadAllText(KonfigFajl);
        return JsonSerializer.Deserialize<FinConfig>(text) ?? new FinConfig();
    }

    public string? DajIzvozPutanju()
    {
        try
        {
            if (!File.Exists(KonfigFajl))
                return null;

            var json = File.ReadAllText(KonfigFajl);
            var config = JsonSerializer.Deserialize<FinConfig>(json);
            return config?.IzvozPutanja;
        }
        catch
        {
            return null;
        }
    }

    public bool SnimiIzvozPutanju(string putanja)
    {
        try
        {
            var config = UcitajIliNapraviConfig();
            config.IzvozPutanja = putanja;

            Directory.CreateDirectory(Path.GetDirectoryName(KonfigFajl)!);

            File.WriteAllText(
                KonfigFajl,
                JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private class FinConfig
    {
        public string? FinPutanja { get; set; }
        public string? ArhivaPutanja { get; set; }
        public string? IzvozPutanja { get; set; }
    }
}
