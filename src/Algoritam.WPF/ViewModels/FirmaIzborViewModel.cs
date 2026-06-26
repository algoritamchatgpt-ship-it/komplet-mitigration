using Algoritam.Application;
using Algoritam.Application.Services;
using Algoritam.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

public partial class FirmaIzborViewModel : ObservableObject
{
    private readonly IFirmaService _firmaService;
    private readonly IPutanjaService _putanjaService;
    private readonly AppState _appState;

    public FirmaIzborViewModel(IFirmaService firmaService, IPutanjaService putanjaService, AppState appState)
    {
        _firmaService = firmaService;
        _putanjaService = putanjaService;
        _appState = appState;
    }

    [ObservableProperty]
    private ObservableCollection<Firma> _firme = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PotvrdiCommand))]
    [NotifyCanExecuteChangedFor(nameof(ObrisiFirmuCommand))]
    [NotifyCanExecuteChangedFor(nameof(ArhivirajFirmuCommand))]
    private Firma? _selektovanaFirma;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PotvrdiCommand))]
    [NotifyCanExecuteChangedFor(nameof(DodajFirmuCommand))]
    [NotifyCanExecuteChangedFor(nameof(ObrisiFirmuCommand))]
    [NotifyCanExecuteChangedFor(nameof(ArhivirajFirmuCommand))]
    private bool _ucitava = true;

    public string KorisnikInfo =>
        $"Prijavljeni ste kao: {_appState.TrenutniKorisnik?.KorisnikIme ?? "—"}";

    public event Action? FirmaIzabrana;
    public event Action? OtkacenoPrijavljeni;
    public event Action<string>? GradoviTrazeni;

    public async Task UcitajAsync()
    {
        Ucitava = true;
        Log.Debug("UcitajAsync — ucitavam firme...");
        var lista = await Task.Run(() => _firmaService.DajSveFirmeAsync());
        Log.Debug("UcitajAsync — ucitano {Count} firmi", lista.Count);
        Firme = new ObservableCollection<Firma>(lista);
        SelektovanaFirma = lista.FirstOrDefault(f => f.Aktivna) ?? lista.FirstOrDefault();
        Ucitava = false;
    }

    [ObservableProperty]
    private string _migrationStatus = string.Empty;

    private bool MozePotvrdi() => SelektovanaFirma != null && !Ucitava;

    [RelayCommand(CanExecute = nameof(MozePotvrdi))]
    private void Potvrdi()
    {
        if (SelektovanaFirma is null) return;

        Log.Information("Potvrdi kliknuto za firmu: {Naziv} ({Folder})",
            SelektovanaFirma.Naziv, SelektovanaFirma.FolderPath);

        _appState.PostaviFirmu(SelektovanaFirma, standalone: false);
        MigrationStatus = "Firma izabrana (DBF rezim).";

        Log.Information("Potvrdi zavrsen — prelazim na FirmaIzabrana");
        FirmaIzabrana?.Invoke();
    }

    [RelayCommand]
    private void Otkaci()
    {
        _appState.Odjavi();
        OtkacenoPrijavljeni?.Invoke();
    }

    private bool MozeBrisiIliDodaj() => !Ucitava && SelektovanaFirma != null;
    private bool MozeDodajFirmu() => !Ucitava;

    [RelayCommand(CanExecute = nameof(MozeDodajFirmu))]
    private async Task DodajFirmu()
    {
        Log.Information("DodajFirmu — pocetak");
        Ucitava = true;
        MigrationStatus = "Kreiranje foldera nove firme...";
        try
        {
            Log.Debug("DodajFirmu — pozivam DodajFirmuAsync...");
            var novaFirma = await Task.Run(() => _firmaService.DodajFirmuAsync());
            Log.Debug("DodajFirmu — DodajFirmuAsync zavrsen: {Folder}", novaFirma?.FolderPath);

            MigrationStatus = "Učitavam listu firmi...";
            Log.Debug("DodajFirmu — pozivam UcitajAsync...");
            await UcitajAsync();
            Log.Debug("DodajFirmu — UcitajAsync zavrsen");

            if (novaFirma is null)
            {
                Log.Warning("DodajFirmu — novaFirma je null");
                MigrationStatus = "Firma nije dodata.";
                return;
            }

            SelektovanaFirma = Firme.FirstOrDefault(f =>
                string.Equals(f.FolderPath, novaFirma.FolderPath, StringComparison.OrdinalIgnoreCase));

            Log.Information("DodajFirmu — firma kreirana: {Folder}", novaFirma.FolderPath);
            MigrationStatus = $"Kreirana je nova firma u folderu {Path.GetFileName(novaFirma.FolderPath)}.";
            MessageBox.Show(
                $"Kreirana je nova firma u folderu {Path.GetFileName(novaFirma.FolderPath)}.",
                "Dodavanje firme",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DodajFirmu — GRESKA");
            MigrationStatus = $"Greska pri dodavanju firme: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
            Log.Debug("DodajFirmu — zavrsen");
        }
    }

    [RelayCommand(CanExecute = nameof(MozeBrisiIliDodaj))]
    private async Task ObrisiFirmu()
    {
        if (SelektovanaFirma is null) return;

        var potvrda = MessageBox.Show(
            $"Da li ste sigurni da želite da obrišete firmu:\n\n\"{SelektovanaFirma.Naziv}\"\n\nFolder: {SelektovanaFirma.FolderPath}\n\nOva akcija je NEPOVRATNA — svi podaci će biti obrisani!",
            "Brisanje firme",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (potvrda != MessageBoxResult.Yes) return;

        Log.Information("ObrisiFirmu — brišem: {Naziv} ({Folder})", SelektovanaFirma.Naziv, SelektovanaFirma.FolderPath);
        Ucitava = true;
        MigrationStatus = $"Brišem firmu \"{SelektovanaFirma.Naziv}\"...";

        try
        {
            var folder = SelektovanaFirma.FolderPath;
            var uspelo = await Task.Run(() => _firmaService.ObrisiFirmuAsync(folder));
            Log.Information("ObrisiFirmu — rezultat: {Uspelo}", uspelo);

            if (!uspelo)
            {
                MigrationStatus = "Brisanje nije uspelo.";
                MessageBox.Show("Brisanje firme nije uspelo.\nProvjerite da li su fajlovi zaključani.",
                    "Brisanje firme", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await UcitajAsync();
            MigrationStatus = "Firma je obrisana.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ObrisiFirmu — GRESKA");
            MigrationStatus = $"Greška pri brisanju: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand(CanExecute = nameof(MozeBrisiIliDodaj))]
    private async Task ArhivirajFirmu()
    {
        if (SelektovanaFirma is null) return;

        var arhivaPutanja = _putanjaService.DajArhivaPutanju();
        if (string.IsNullOrWhiteSpace(arhivaPutanja))
        {
            MessageBox.Show(
                "Putanja za arhiviranje nije postavljena.\n\nPostavite je na početnom ekranu (Postavi arhivu...).",
                "Arhiviranje",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var potvrda = MessageBox.Show(
            $"Da li želite da arhivirate podatke za firmu:\n\n\"{SelektovanaFirma.Naziv}\"\n\nFolder: {SelektovanaFirma.FolderPath}\n\nArhiva će biti sačuvana u:\n{arhivaPutanja}",
            "Arhiviranje firme",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (potvrda != MessageBoxResult.Yes) return;

        Log.Information("ArhivirajFirmu — pocinjem: {Naziv} ({Folder})", SelektovanaFirma.Naziv, SelektovanaFirma.FolderPath);
        Ucitava = true;
        MigrationStatus = "Arhiviranje u toku...";

        try
        {
            var firma = SelektovanaFirma;
            var zipPath = await Task.Run(() => KreirajArhivu(firma.FolderPath, arhivaPutanja));
            Log.Information("ArhivirajFirmu — ZIP kreiran: {Zip}", zipPath);

            MigrationStatus = $"Arhiva sačuvana: {Path.GetFileName(zipPath)}";
            MessageBox.Show(
                $"Arhiviranje završeno.\n\nFajl: {zipPath}",
                "Arhiviranje završeno",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ArhivirajFirmu — GRESKA");
            MigrationStatus = $"Greška pri arhiviranju: {ex.Message}";
            MessageBox.Show($"Greška pri arhiviranju:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Ucitava = false;
        }
    }

    private static string KreirajArhivu(string firmaFolder, string arhivaPutanja)
    {
        Directory.CreateDirectory(arhivaPutanja);

        var folderName = Path.GetFileName(firmaFolder.TrimEnd(Path.DirectorySeparatorChar));
        var timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
        var zipName = $"{folderName}_{timestamp}.zip";
        var zipPath = Path.Combine(arhivaPutanja, zipName);

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        DodajFolderUZip(zip, firmaFolder, folderName);
        return zipPath;
    }

    private static void DodajFolderUZip(ZipArchive zip, string sourceFolder, string zipEntryPrefix)
    {
        foreach (var filePath in Directory.GetFiles(sourceFolder))
        {
            var entryName = $"{zipEntryPrefix}/{Path.GetFileName(filePath)}";
            var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);

            // FileShare.ReadWrite dozvoljava kopiranje fajlova koje drži SQLite ili drugi procesi
            using var source = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var dest = entry.Open();
            source.CopyTo(dest);
        }

        foreach (var subDir in Directory.GetDirectories(sourceFolder))
        {
            var subPrefix = $"{zipEntryPrefix}/{Path.GetFileName(subDir)}";
            DodajFolderUZip(zip, subDir, subPrefix);
        }
    }

    [RelayCommand]
    private void OtvoriGradove()
    {
        var finPutanja = _putanjaService.DajFinPutanju();
        if (string.IsNullOrWhiteSpace(finPutanja))
        {
            MigrationStatus = "FIN putanja nije postavljena.";
            return;
        }

        GradoviTrazeni?.Invoke(finPutanja);
    }

}
