using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Algoritam.Core.Services;
using Algoritam.Core.Services.Dbf;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Algoritam.Core.ViewModels;

public partial class PocetniViewModel : ObservableObject
{
    private const string LokalniModMarker = ".algoritam-local-no-password";
    private static readonly Regex FolderRegex = new(@"^F\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IPutanjaService _putanjaService;

    public PocetniViewModel(IPutanjaService putanjaService)
    {
        _putanjaService = putanjaService;
        OsveziPutanju();
        ArhivaPutanja = _putanjaService.DajArhivaPutanju() ?? string.Empty;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PutanjaPrikazana))]
    [NotifyPropertyChangedFor(nameof(PutanjaPostavljena))]
    [NotifyPropertyChangedFor(nameof(PutanjaBoja))]
    [NotifyPropertyChangedFor(nameof(PutanjaIkonica))]
    private string _finPutanja = string.Empty;

    [ObservableProperty]
    private string _statusPoruka = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ArhivaPutanjaPrikazana))]
    [NotifyPropertyChangedFor(nameof(ArhivaPutanjaPostavljena))]
    [NotifyPropertyChangedFor(nameof(ArhivaPutanjaBoja))]
    [NotifyPropertyChangedFor(nameof(ArhivaPutanjaIkonica))]
    private string _arhivaPutanja = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PostaviPutanjuCommand))]
    [NotifyCanExecuteChangedFor(nameof(KreirajLokalnuInstalacijuCommand))]
    private bool _ucitava;

    public string PutanjaPrikazana =>
        string.IsNullOrEmpty(FinPutanja) ? "Putanja nije postavljena" : FinPutanja;

    public bool PutanjaPostavljena =>
        !string.IsNullOrEmpty(FinPutanja) && _putanjaService.JeValidanFinFolder(FinPutanja);

    public string PutanjaBoja => PutanjaPostavljena ? "#A5D6A7" : "#EF9A9A";
    public string PutanjaIkonica => PutanjaPostavljena ? "OK" : "!";

    public string ArhivaPutanjaPrikazana =>
        string.IsNullOrEmpty(ArhivaPutanja) ? "Putanja nije postavljena" : ArhivaPutanja;

    public bool ArhivaPutanjaPostavljena => !string.IsNullOrEmpty(ArhivaPutanja);
    public string ArhivaPutanjaBoja => ArhivaPutanjaPostavljena ? "#A5D6A7" : "#EF9A9A";
    public string ArhivaPutanjaIkonica => ArhivaPutanjaPostavljena ? "OK" : "!";

    private bool MozeAkcija() => !Ucitava;

    [RelayCommand(CanExecute = nameof(MozeAkcija))]
    private void PostaviPutanju()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Izaberi root folder FIN instalacije (sadrži F1, F2, data00...)",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;
        var odabrana = dialog.FolderName;

        if (!_putanjaService.JeValidanFinFolder(odabrana))
        {
            StatusPoruka = "Izabrani folder nije validan FIN folder (nema F1, F2 ni data00).";
            return;
        }

        if (_putanjaService.SnimiFinPutanju(odabrana))
        {
            OsveziPutanju();
            StatusPoruka = string.Empty;
        }
        else
        {
            StatusPoruka = "Nije moguće sačuvati putanju.";
        }
    }

    [RelayCommand]
    private void PostaviArhivaPutanju()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Izaberite folder za arhiviranje firmi",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        if (_putanjaService.SnimiArhivaPutanju(dialog.FolderName))
        {
            ArhivaPutanja = dialog.FolderName;
            StatusPoruka = string.Empty;
        }
        else
        {
            StatusPoruka = "Nije moguće sačuvati arhiv putanju.";
        }
    }

    [RelayCommand(CanExecute = nameof(MozeAkcija))]
    private async Task KreirajLokalnuInstalaciju()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Izaberite folder za lokalnu instalaciju (biće kreirani F1, data00, data01)",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        var ciljnaPutanja = dialog.FolderName;
        Ucitava = true;
        StatusPoruka = "Kreiram strukturu foldera...";

        try
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(Path.Combine(ciljnaPutanja, "data00"));
                Directory.CreateDirectory(Path.Combine(ciljnaPutanja, "data01"));
                var f1Folder = Path.Combine(ciljnaPutanja, "F1");
                Directory.CreateDirectory(f1Folder);

                KreirajDbfTabliceUFolderu(f1Folder);
                FinWorkspaceResolver.EnsureFirmaDbf(f1Folder);
                FinWorkspaceResolver.EnsureLozinkeTables(ciljnaPutanja,
                    out _, out _, out _);

                KopirajParametarskuTabelu("mesta", ciljnaPutanja);

                // Marker za lokalni mod bez lozinke
                File.WriteAllText(
                    Path.Combine(ciljnaPutanja, LokalniModMarker),
                    DateTime.UtcNow.ToString("O"));
            });

            if (!_putanjaService.SnimiFinPutanju(ciljnaPutanja))
            {
                StatusPoruka = "Nije moguće postaviti putanju.";
                return;
            }

            OsveziPutanju();
            StatusPoruka = "Lokalna instalacija uspešno kreirana — tabele su prazne, spremne za unos.";
            OtvoriFolder(ciljnaPutanja);
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

    private void OsveziPutanju()
    {
        FinPutanja = _putanjaService.DajFinPutanju() ?? string.Empty;
    }

    private static void OtvoriFolder(string folderPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = folderPath, UseShellExecute = true });
        }
        catch { }
    }

    // Kopira dbf+cdx u novi data00 — traži u više lokacija da ne zavisi od mjesta exe-a
    private static void KopirajParametarskuTabelu(string baseIme, string ciljnaPutanja)
    {
        var ciljDir = Path.Combine(ciljnaPutanja, "data00");

        var pretragaDirs = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "data00"),
            Path.Combine(AppContext.BaseDirectory, "DATA00"),
            Path.Combine(Path.GetDirectoryName(Environment.ProcessPath ?? "") ?? "", "data00"),
        };

        foreach (var izvorDir in pretragaDirs)
        {
            var izvor = DbfHelper.NadjiDbfUFolderu(izvorDir, baseIme + ".dbf");
            if (izvor == null || !DbfHelper.ImaZapisaDbf(izvor)) continue;

            Directory.CreateDirectory(ciljDir);
            foreach (var ext in new[] { ".dbf", ".cdx", ".fpt", ".dbt" })
            {
                var src = Path.ChangeExtension(izvor, ext);
                if (!File.Exists(src)) continue;
                var dst = Path.Combine(ciljDir, Path.GetFileName(src));
                if (!File.Exists(dst))
                    File.Copy(src, dst);
            }
            return;
        }
    }

    private static void KreirajDbfTabliceUFolderu(string folder)
    {
        void Kreiraj(string ime, IReadOnlyList<DbfKreator.PoljeDbf> sema)
        {
            var p = Path.Combine(folder, ime);
            if (!File.Exists(p))
                DbfKreator.KreirajPrazanDbf(p, sema);
        }

        Kreiraj("os0.dbf",      DbfKreator.SemaOs0());
        Kreiraj("os.dbf",       DbfKreator.SemaOsEvidencija());
        Kreiraj("osa.dbf",      DbfKreator.SemaOsEvidencija());
        Kreiraj("osoa.dbf",     DbfKreator.SemaOsoa());
        Kreiraj("ospodaci.dbf", DbfKreator.SemaOspodaci());
        Kreiraj("an0.dbf",      DbfKreator.SemaAn0());
        Kreiraj("mesta.dbf",    DbfKreator.SemaMesta());
        Kreiraj("osvrsta.dbf",  DbfKreator.SemaOsVrsta());
        Kreiraj("osag.dbf",     DbfKreator.SemaOsAg());
        Kreiraj("osagpod.dbf",  DbfKreator.SemaOsAgPod());
        Kreiraj("osizvorf.dbf", DbfKreator.SemaOsIzvorF());
        Kreiraj("ososnk.dbf",   DbfKreator.SemaOsOsnK());
        Kreiraj("ospopis.dbf",  DbfKreator.SemaOspopis());
        Kreiraj("konto.dbf",    DbfKreator.SemaKonto());
    }
}
