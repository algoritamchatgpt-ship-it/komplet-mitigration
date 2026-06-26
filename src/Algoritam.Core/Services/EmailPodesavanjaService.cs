using Algoritam.Core.Models;
using System.IO;
using System.Text.Json;

namespace Algoritam.Core.Services;

public class EmailPodesavanjaService
{
    private const string FileName = "email-podesavanja.json";
    private readonly string _filePath;
    private EmailPodesavanja _podaci;

    public EmailPodesavanjaService(string appFolderIme = "Algoritam")
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, appFolderIme);
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, FileName);
        _podaci = Ucitaj();
    }

    public EmailPodesavanja DajPodesavanja() => _podaci;

    public bool Snimi(EmailPodesavanja podesavanja)
    {
        _podaci = podesavanja;
        return SnimiNaDisk();
    }

    public bool JeKonfigurisano() =>
        !string.IsNullOrWhiteSpace(_podaci.SmtpServer) &&
        !string.IsNullOrWhiteSpace(_podaci.PosiljaoceEmail);

    private EmailPodesavanja Ucitaj()
    {
        try
        {
            if (!File.Exists(_filePath)) return new();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<EmailPodesavanja>(json) ?? new();
        }
        catch { return new(); }
    }

    private bool SnimiNaDisk()
    {
        try
        {
            var json = JsonSerializer.Serialize(_podaci, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
            return true;
        }
        catch { return false; }
    }
}
