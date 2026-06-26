using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Xml;

namespace Algoritam.WPF.ViewModels;

public record BilansVrsta(string Kod, string Naziv, string DbfIme);

public partial class GkXmlBilansViewModel : ObservableObject
{
    private readonly string _folderPath;

    public string Naslov { get; }

    // Parameters from znxml.dbf
    [ObservableProperty] private string _jmb = "";
    [ObservableProperty] private string _pib = "";
    [ObservableProperty] private string _godina = "";
    [ObservableProperty] private string _period = "";
    [ObservableProperty] private string _mesto = "";
    [ObservableProperty] private bool _parametriUcitani;

    // Bilans type selection
    public List<BilansVrsta> TipoviBilansa { get; } =
    [
        new("BS",  "Bilans stanja",        "xmlbs.dbf"),
        new("BU",  "Bilans uspeha",        "xmlbu.dbf"),
        new("KAP", "Kapital",              "xmlkap.dbf"),
        new("TOK", "Tok gotovine",         "xmltok.dbf"),
        new("OST", "Ostale napomene",      "xmlost.dbf"),
        new("SI",  "Statistički izveštaj", "xmlsi.dbf"),
        new("POS", "Poslovanje",           "xmlpos.dbf"),
    ];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportXmlCommand))]
    private BilansVrsta? _izabraniTip;

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _pozicije = [];
    [ObservableProperty] private bool _ucitava;
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private string _filterTekst = "";

    private List<Dictionary<string, object?>> _svePozicije = [];

    public event Action? ZatvaranjeZahtevano;

    public GkXmlBilansViewModel(string folderPath, string naslov = "XML BILANSI — E-BILANS")
    {
        _folderPath = folderPath;
        Naslov = naslov;
        _ = UcitajParametreAsync();
        IzabraniTip = TipoviBilansa[0];
    }

    private async Task UcitajParametreAsync()
    {
        try
        {
            var dbfPath = NadjiDbf(_folderPath, "znxml.dbf");
            if (dbfPath is null) return;
            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));
            var p = zapisi.FirstOrDefault();
            if (p is null) return;

            Jmb    = p.TryGetValue("JMB",    out var j) ? j?.ToString()?.Trim() ?? "" : "";
            Pib    = p.TryGetValue("PIB",    out var pb) ? pb?.ToString()?.Trim() ?? "" : "";
            Godina = p.TryGetValue("GODINA", out var g) ? g?.ToString()?.Trim() ?? "" : "";
            Period = p.TryGetValue("PERIOD", out var pr) ? pr?.ToString()?.Trim() ?? "" : "";
            Mesto  = p.TryGetValue("MESTO",  out var m) ? m?.ToString()?.Trim() ?? "" : "";
            ParametriUcitani = !string.IsNullOrEmpty(Jmb) || !string.IsNullOrEmpty(Pib);
        }
        catch { }
    }

    partial void OnIzabraniTipChanged(BilansVrsta? value)
    {
        if (value is not null)
            _ = UcitajPozicijeAsync(value);
    }

    partial void OnFilterTekstChanged(string value) => PrimenjiFilter();

    private async Task UcitajPozicijeAsync(BilansVrsta tip)
    {
        Ucitava = true;
        StatusPoruka = $"Učitavanje {tip.Naziv}...";
        _svePozicije = [];
        Pozicije = [];

        try
        {
            var dbfPath = NadjiDbf(_folderPath, tip.DbfIme);
            if (dbfPath is null)
            {
                StatusPoruka = $"{tip.DbfIme} nije pronađen.";
                return;
            }

            _svePozicije = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));
            PrimenjiFilter();
            StatusPoruka = $"{tip.Naziv} — {_svePozicije.Count} pozicija.";
        }
        catch (Exception ex)
        {
            StatusPoruka = $"Greška: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    private void PrimenjiFilter()
    {
        var tekst = FilterTekst.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(tekst))
        {
            Pozicije = new ObservableCollection<Dictionary<string, object?>>(_svePozicije);
            return;
        }
        var filtrirane = _svePozicije.Where(r =>
            r.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(tekst) == true));
        Pozicije = new ObservableCollection<Dictionary<string, object?>>(filtrirane.ToList());
    }

    [RelayCommand]
    private void OdaberiTip(BilansVrsta? tip)
    {
        if (tip is not null)
            IzabraniTip = tip;
    }

    [RelayCommand(CanExecute = nameof(MozeEksportovati))]
    private void ExportXml()
    {
        if (IzabraniTip is null) return;

        var dlg = new SaveFileDialog
        {
            Title = $"Eksport — {IzabraniTip.Naziv}",
            Filter = "XML fajlovi (*.xml)|*.xml|Svi fajlovi (*.*)|*.*",
            FileName = $"bilans_{IzabraniTip.Kod.ToLower()}_{Godina}.xml",
            DefaultExt = ".xml"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var xml = GenerisiXml();
            File.WriteAllText(dlg.FileName, xml, new UTF8Encoding(false));
            StatusPoruka = $"XML sačuvan: {Path.GetFileName(dlg.FileName)}";
        }
        catch (Exception ex)
        {
            StatusPoruka = $"Greška pri eksportu: {ex.Message}";
        }
    }

    private bool MozeEksportovati() => IzabraniTip is not null && _svePozicije.Count > 0;

    private string GenerisiXml()
    {
        var tip = IzabraniTip!;
        var sb = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new UTF8Encoding(false),
            OmitXmlDeclaration = false
        };

        using var writer = XmlWriter.Create(sb, settings);
        writer.WriteStartDocument();
        writer.WriteStartElement("Bilans");
        writer.WriteAttributeString("vrsta", tip.Kod);
        if (!string.IsNullOrEmpty(Jmb))    writer.WriteAttributeString("jmb",    Jmb);
        if (!string.IsNullOrEmpty(Pib))    writer.WriteAttributeString("pib",    Pib);
        if (!string.IsNullOrEmpty(Godina)) writer.WriteAttributeString("godina", Godina);
        if (!string.IsNullOrEmpty(Period)) writer.WriteAttributeString("period", Period);
        if (!string.IsNullOrEmpty(Mesto))  writer.WriteAttributeString("mesto",  Mesto);

        foreach (var red in _svePozicije)
        {
            writer.WriteStartElement("Pozicija");
            foreach (var kv in red)
            {
                if (kv.Key is "PRENETO" or "IDBR") continue;
                var val = kv.Value?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(val))
                    writer.WriteAttributeString(kv.Key.ToLower(), val);
            }
            // Write PODATAK as text content if present
            if (red.TryGetValue("PODATAK", out var podatak) && podatak?.ToString()?.Trim() is { Length: > 0 } pStr)
                writer.WriteString(pStr);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
        return sb.ToString();
    }

    [RelayCommand]
    private void Osvezi()
    {
        if (IzabraniTip is not null)
            _ = UcitajPozicijeAsync(IzabraniTip);
        _ = UcitajParametreAsync();
    }

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

    private static string? NadjiDbf(string folderPath, string fileName)
    {
        foreach (var dir in new[] { folderPath,
            Path.Combine(folderPath, "data00"),
            Path.Combine(folderPath, "01"),
            Path.Combine(folderPath, "..") })
        {
            if (!Directory.Exists(dir)) continue;
            var f = Path.Combine(dir, fileName);
            if (File.Exists(f)) return f;
            try
            {
                var ci = Directory.GetFiles(dir, "*.dbf", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(x => Path.GetFileName(x).Equals(fileName, StringComparison.OrdinalIgnoreCase));
                if (ci is not null) return ci;
            }
            catch { }
        }
        return null;
    }
}
