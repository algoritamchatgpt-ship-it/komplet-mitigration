using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using Algoritam.WPF.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using DomainFirma   = Algoritam.Domain.Entities.Firma;
using DomainRadnik  = Algoritam.Domain.Entities.Radnik;
using DomainStavka  = Algoritam.Domain.Entities.LdObracunStavka;
using DomainParam   = Algoritam.Domain.Entities.LdParametar;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Forma za slanje isplatnih listića na email.
/// Čita radnike iz ldrad.dbf (EMAIL polje), generiše PDF po radniku,
/// otvara Outlook draft sa automatski dodatim PDF prilogom.
/// </summary>
public partial class EmailListiciViewModel : ObservableObject
{
    public sealed class RadnikEmailRed : ObservableObject
    {
        private bool _izabran = true;
        public bool   Izabran    { get => _izabran; set => SetProperty(ref _izabran, value); }
        public int    Broj       { get; init; }
        public string ImePrez    { get; init; } = string.Empty;
        public string Email      { get; init; } = string.Empty;

        private string _status = string.Empty;
        public string Status    { get => _status; set => SetProperty(ref _status, value); }

        public DomainStavka? Stavka  { get; init; }
        public DomainRadnik? Radnik  { get; init; }
    }

    private readonly AppState _appState;
    private DomainParam? _parametar;

    public event Action? ZatvaranjeZahtevano;

    [ObservableProperty] private ObservableCollection<RadnikEmailRed> _redovi = [];
    [ObservableProperty] private string _poruka = "Učitavanje...";
    [ObservableProperty] private string _outputFolder = string.Empty;
    [ObservableProperty] private string _mesecGodina = string.Empty;
    [ObservableProperty] private bool   _ucitava;

    public EmailListiciViewModel(AppState appState)
    {
        _appState = appState;
        OutputFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Algoritam", "EmailListici");
        UcitajPodatke();
    }

    private void UcitajPodatke()
    {
        Ucitava = true;
        Redovi.Clear();

        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            Ucitava = false;
            return;
        }

        try
        {
            // Parametri (mesec/isplata/godina)
            _parametar = UcitajParametar(folder);
            MesecGodina = _parametar != null
                ? $"Mesec: {_parametar.Mesec} / {_parametar.Godina}  Isplata: {_parametar.Isplata}"
                : string.Empty;

            // Radnici sa email-om iz ldrad.dbf
            var radnici = UcitajRadnike(folder);
            var saEmailom = radnici
                .Where(r => !string.IsNullOrWhiteSpace(r.Email))
                .ToDictionary(r => r.Broj);

            if (saEmailom.Count == 0)
            {
                Poruka = "Nijedan radnik nema unetu email adresu (polje EMAIL u evidenciji zaposlenih).";
                Ucitava = false;
                return;
            }

            // Stavke iz LD*.dbf za aktivan period
            var stavke = UcitajStavkePlatnog(folder, _parametar);
            var stavkePoKljucu = stavke
                .GroupBy(s => s.Broj)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var (broj, radnik) in saEmailom.OrderBy(kv => kv.Key))
            {
                stavkePoKljucu.TryGetValue(broj, out var stavka);
                Redovi.Add(new RadnikEmailRed
                {
                    Broj    = radnik.Broj,
                    ImePrez = radnik.ImePrezime ?? string.Empty,
                    Email   = radnik.Email.Trim(),
                    Stavka  = stavka,
                    Radnik  = radnik
                });
            }

            var bezStavke = Redovi.Count(r => r.Stavka == null);
            Poruka = bezStavke == 0
                ? $"Pronađeno {Redovi.Count} radnika sa email adresom."
                : $"Pronađeno {Redovi.Count} radnika sa email adresom ({bezStavke} nema obračuna za aktivan period).";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri učitavanju: {ex.Message}";
        }

        Ucitava = false;
    }

    // ── Selekcija ────────────────────────────────────────────────────────────

    private static bool PokusajOtvoriOutlookDraft(string primaoc, string subject, string body, string pdfPutanja)
    {
        object? outlookApp = null;
        object? mailItem = null;
        object? attachments = null;

        try
        {
            var outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType == null)
                return false;

            outlookApp = Activator.CreateInstance(outlookType);
            if (outlookApp == null)
                return false;

            mailItem = outlookType.InvokeMember(
                "CreateItem",
                BindingFlags.InvokeMethod,
                null,
                outlookApp,
                [0]);
            if (mailItem == null)
                return false;

            var mailType = mailItem.GetType();
            mailType.InvokeMember("To", BindingFlags.SetProperty, null, mailItem, [primaoc]);
            mailType.InvokeMember("Subject", BindingFlags.SetProperty, null, mailItem, [subject]);
            mailType.InvokeMember("Body", BindingFlags.SetProperty, null, mailItem, [body]);

            attachments = mailType.InvokeMember("Attachments", BindingFlags.GetProperty, null, mailItem, null);
            if (attachments != null)
            {
                attachments.GetType().InvokeMember(
                    "Add",
                    BindingFlags.InvokeMethod,
                    null,
                    attachments,
                    [pdfPutanja]);
            }

            mailType.InvokeMember("Display", BindingFlags.InvokeMethod, null, mailItem, [false]);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (attachments != null && Marshal.IsComObject(attachments))
                Marshal.FinalReleaseComObject(attachments);
            if (mailItem != null && Marshal.IsComObject(mailItem))
                Marshal.FinalReleaseComObject(mailItem);
            if (outlookApp != null && Marshal.IsComObject(outlookApp))
                Marshal.FinalReleaseComObject(outlookApp);
        }
    }

    [RelayCommand]
    private void IzaberiSve()
    {
        foreach (var r in Redovi) r.Izabran = true;
    }

    [RelayCommand]
    private void PonistiSve()
    {
        foreach (var r in Redovi) r.Izabran = false;
    }

    // ── Promena foldera ──────────────────────────────────────────────────────

    [RelayCommand]
    private void PromeniFolder()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title            = "Izaberite folder za čuvanje PDF listića",
            InitialDirectory = Directory.Exists(OutputFolder) ? OutputFolder : string.Empty,
            Multiselect      = false
        };

        if (dlg.ShowDialog() == true)
            OutputFolder = dlg.FolderName;
    }

    // ── Generisanje PDF-ova ──────────────────────────────────────────────────

    [RelayCommand]
    private void GenerisiPdfove()
    {
        var izabrani = Redovi.Where(r => r.Izabran && r.Stavka != null).ToList();
        if (izabrani.Count == 0)
        {
            MessageBox.Show("Nema izabranih radnika sa obračunom za aktivan period.",
                "PDF listići", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try { Directory.CreateDirectory(OutputFolder); }
        catch (Exception ex)
        {
            MessageBox.Show($"Nije moguće kreirati folder:\n{OutputFolder}\n\n{ex.Message}",
                "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var firma = _appState.AktivnaFirma;
        var datumIsplate = DatumIsplate(_parametar);
        int generisano = 0;

        foreach (var red in izabrani)
        {
            try
            {
                var pdfPath = PdfPutanja(red);
                var lines   = Views.Zarade.JedanListicReportView.GenerirajPdfLinijeStat(
                    red.Stavka!, red.Radnik, _parametar, firma, datumIsplate);
                SimplePdfWriter.WriteTextPdf(pdfPath, "Isplatni listić", lines);
                generisano++;
            }
            catch (Exception ex)
            {
                red.Status = $"Greška: {ex.Message}";
            }
        }

        Poruka = $"Generisano {generisano} PDF-ova u: {OutputFolder}";

        // Otvori folder u Exploreru
        try
        {
            Process.Start(new ProcessStartInfo { FileName = OutputFolder, UseShellExecute = true });
        }
        catch { }

        MessageBox.Show(
            $"Generisano {generisano} PDF listića.\n\nSačuvani u:\n{OutputFolder}\n\n" +
            "Sledeći korak: kliknite POŠALJI SVIMA da otvorite email klijent za svakog radnika.",
            "PDF listići", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ── Slanje email-a ───────────────────────────────────────────────────────

    [RelayCommand]
    private void PostaljiJednom(RadnikEmailRed? red)
    {
        if (red == null) return;
        OtvoriEmail(red);
    }

    [RelayCommand]
    private async Task PostaljiSvimaAsync()
    {
        var izabrani = Redovi
            .Where(r => r.Izabran && !string.IsNullOrWhiteSpace(r.Email))
            .ToList();

        if (izabrani.Count == 0)
        {
            MessageBox.Show("Nema izabranih radnika sa email adresom.",
                "Slanje mejla", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Auto-generiši PDF ako ne postoji
        var bezPdf = izabrani.Where(r => r.Stavka != null && !File.Exists(PdfPutanja(r))).ToList();
        if (bezPdf.Count > 0)
        {
            var gen = MessageBox.Show(
                $"{bezPdf.Count} radnik(a) nema generisan PDF.\n\nGenerisati PDF-ove pre slanja?",
                "Slanje mejla", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (gen == MessageBoxResult.Cancel) return;
            if (gen == MessageBoxResult.Yes) GenerisiPdfove();
        }

        var pitanje = MessageBox.Show(
            $"Otvoriti email klijent za {izabrani.Count} radnika?\n\n" +
            "Za svakog radnika otvoriće se prozor za pisanje mejla.",
            "Slanje mejla", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (pitanje != MessageBoxResult.Yes) return;

        int otvoreno = 0;
        foreach (var red in izabrani)
        {
            if (OtvoriEmail(red))
                otvoreno++;

            await Task.Delay(200);
        }

        Poruka = $"Otvoren email klijent za {otvoreno}/{izabrani.Count} radnika.";
    }

    private string PdfPutanja(RadnikEmailRed red)
    {
        var bezbednoIme = new string(
            red.ImePrez.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray()).Trim();
        return Path.Combine(OutputFolder, $"Listic_{red.Broj}_{bezbednoIme}.pdf");
    }

    private bool OtvoriEmail(RadnikEmailRed red)
    {
        try
        {
            var subject = _parametar != null
                ? $"Isplatni listić {_parametar.Mesec:00}/{_parametar.Godina}"
                : "Isplatni listić";

            var pdfPutanja = PdfPutanja(red);

            var body = $"Poštovani/a {red.ImePrez},\r\n\r\n" +
                       $"U prilogu se nalazi Vaš isplatni listić.\r\n\r\n" +
                       "S poštovanjem";

            if (!File.Exists(pdfPutanja))
            {
                red.Status = "Greska: PDF nije pronadjen (kliknite GENERISI PDFOVE).";
                return false;
            }

            if (PokusajOtvoriOutlookDraft(red.Email, subject, body, pdfPutanja))
            {
                red.Status = "Email otvoren (PDF prilog dodat).";
                return true;
            }

            var mailtoUri = $"mailto:{Uri.EscapeDataString(red.Email)}" +
                            $"?subject={Uri.EscapeDataString(subject)}" +
                            $"&body={Uri.EscapeDataString(body)}";

            Process.Start(new ProcessStartInfo { FileName = mailtoUri, UseShellExecute = true });
            red.Status = "Email otvoren (bez automatskog priloga).";
            return true;
        }
        catch (Exception ex)
        {
            red.Status = $"Greška: {ex.Message}";
            return false;
        }
    }

    // ── Osvezi / Zatvori ─────────────────────────────────────────────────────

    [RelayCommand]
    private void Osvezi() => UcitajPodatke();

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DomainParam? UcitajParametar(string folder)
    {
        var path = LdObracunDbfReader.PronadjiDbf(folder, "ldparam.dbf");
        if (path == null) return null;

        try
        {
            var z = DbfReader.CitajSveZapise(path).FirstOrDefault();
            if (z == null) return null;

            return new DomainParam
            {
                Mesec    = Int(z, "MESEC"),
                Isplata  = Int(z, "ISPLATA"),
                Godina   = Str(z, "GODINA"),
                Nazmes   = Str(z, "NAZMES"),
                Redispl  = Int(z, "REDISPL"),
                Konacna  = Str(z, "KONACNA"),
                Vrstaplate = Str(z, "VRSTAPLATE"),
                Procpor  = Dec(z, "PROCPOR"),
                Neoporezp = Dec(z, "NEOPOREZP"),
                Dat1 = Dat(z, "DAT1"), Dat2 = Dat(z, "DAT2"),
                Dat3 = Dat(z, "DAT3"), Dat4 = Dat(z, "DAT4"),
                Doppr1 = Dec(z, "DOPPR1"), Dopzr1 = Dec(z, "DOPZR1"), Dopnr1 = Dec(z, "DOPNR1"),
                Doppf1 = Dec(z, "DOPPF1"), Dopzf1 = Dec(z, "DOPZF1"), Dopnf1 = Dec(z, "DOPNF1"),
            };
        }
        catch { return null; }
    }

    private static List<DomainRadnik> UcitajRadnike(string folder)
    {
        var path = LdObracunDbfReader.PronadjiDbf(folder, "ldrad.dbf");
        if (path == null) return [];

        try
        {
            return DbfReader.CitajSveZapise(path)
                .Where(z => !string.Equals(Str(z, "BRISANJE"), "D", StringComparison.OrdinalIgnoreCase))
                .Select(z => new DomainRadnik
                {
                    Broj        = Int(z, "BROJ"),
                    ImePrezime  = Str(z, "IME_PREZ"),
                    MaticniBroj = Str(z, "MATICNIBR"),
                    Posta       = Str(z, "POSTA"),
                    Mesto       = Str(z, "MESTO"),
                    Adresa      = Str(z, "ADRESA"),
                    Email       = Str(z, "EMAIL"),
                })
                .OrderBy(r => r.Broj)
                .ToList();
        }
        catch { return []; }
    }

    private static List<DomainStavka> UcitajStavkePlatnog(string folder, DomainParam? param)
    {
        if (param == null) return [];

        var prefix = param.Isplata switch { 2 => "LDP", 3 => "LDB", _ => "LD" };
        var kandidati = new[] { $"{prefix}.DBF", $"{prefix}{param.Mesec:D2}.DBF", $"{prefix}{param.Mesec}.DBF" };

        foreach (var naziv in kandidati)
        {
            var path = LdObracunDbfReader.PronadjiDbf(folder, naziv);
            if (path == null) continue;

            try
            {
                var zapisi = DbfReader.CitajSveZapise(path)
                    .Where(z => Int(z, "MESEC") == param.Mesec)
                    .ToList();

                if (zapisi.Count == 0) continue;

                return zapisi.Select(z => new DomainStavka
                {
                    Broj      = Int(z, "BROJ"),
                    ImePrez   = Str(z, "IME_PREZ"),
                    Maticnibr = Str(z, "MATICNIBR"),
                    Mesec     = Int(z, "MESEC"),
                    Isplata   = Int(z, "ISPLATA"),
                    Godina    = Str(z, "GODINA"),
                    Nazmes    = Str(z, "NAZMES"),
                    Casuk     = Dec(z, "CASUK"),
                    Dinuk     = Dec(z, "DINUK"),
                    Dinuc     = Dec(z, "DINUC"),
                    Dinvr     = Dec(z, "DINVR"),
                    Bruto     = Dec(z, "BRUTO"),
                    Neto      = Dec(z, "NETO"),
                    Porez     = Dec(z, "POREZ"),
                    Dopsocr   = Dec(z, "DOPSOCR"),
                    Dopsocf   = Dec(z, "DOPSOCF"),
                    Doppr     = Dec(z, "DOPPR"),
                    Dopzr     = Dec(z, "DOPZR"),
                    Dopnr     = Dec(z, "DOPNR"),
                    Doppf     = Dec(z, "DOPPF"),
                    Dopzf     = Dec(z, "DOPZF"),
                    Dopnf     = Dec(z, "DOPNF"),
                    Krediti   = Dec(z, "KREDITI"),
                    Kreditia  = Dec(z, "KREDITIA"),
                    Akontac   = Dec(z, "AKONTAC"),
                    Prevoz    = Dec(z, "PREVOZ"),
                    Kasa      = Dec(z, "KASA"),
                    Kasarata  = Dec(z, "KASARATA"),
                    Samodopr  = Dec(z, "SAMODOPR"),
                    Sindikat1 = Dec(z, "SINDIKAT1"),
                    Sindikat2 = Dec(z, "SINDIKAT2"),
                    Solidarn  = Dec(z, "SOLIDARN"),
                    Aliment   = Dec(z, "ALIMENT"),
                    Obust1    = Dec(z, "OBUST1"),
                    Obust2    = Dec(z, "OBUST2"),
                    Obust3    = Dec(z, "OBUST3"),
                    Obust4    = Dec(z, "OBUST4"),
                    Ukobust   = Dec(z, "UKOBUST"),
                    Zaisplatu = Dec(z, "ZAISPLATU"),
                    Netoprev  = Dec(z, "NETOPREV"),
                    Benproc   = Dec(z, "BENPROC"),
                    Bendin    = Dec(z, "BENDIN"),
                    Topli     = Dec(z, "TOPLI"),
                    Regres    = Dec(z, "REGRES"),
                    Terenski  = Dec(z, "TERENSKI"),
                    Fiksna    = Dec(z, "FIKSNA"),
                    Dotacija  = Dec(z, "DOTACIJA"),
                    Porezs    = Dec(z, "POREZS"),
                    Porezu    = Dec(z, "POREZU"),
                    Porumanj  = Dec(z, "PORUMANJ"),
                    Osnovica  = Dec(z, "OSNOVICA") != 0 ? Dec(z, "OSNOVICA") : Dec(z, "BRUTO"),
                    Propisana = Dec(z, "PROPISANA"),
                    Solpor    = Dec(z, "SOLPOR"),
                    Neto2     = Dec(z, "NETO2"),
                    Dinnoc    = Dec(z, "DINNOC"),
                    Dinprod   = Dec(z, "DINPROD"),
                    Dinradnap = Dec(z, "DINRADNAP"),
                    Dinned    = Dec(z, "DINNED"),
                    Dinpraz   = Dec(z, "DINPRAZ"),
                    Dinbol    = Dec(z, "DINBOL"),
                    Dinbol2   = Dec(z, "DINBOL2"),
                    Dinplac   = Dec(z, "DINPLAC"),
                    Dinplac2  = Dec(z, "DINPLAC2"),
                    Dingod    = Dec(z, "DINGOD"),
                    Dindor    = Dec(z, "DINDOR"),
                    Dinsl     = Dec(z, "DINSL"),
                    Dinvv     = Dec(z, "DINVV"),
                    Din1      = Dec(z, "DIN1"),
                    Din2      = Dec(z, "DIN2"),
                    Din3      = Dec(z, "DIN3"),
                    Dinsus    = Dec(z, "DINSUS"),
                    Dinmin    = Dec(z, "DINMIN"),
                    Dopumanj  = Dec(z, "DOPUMANJ"),
                    Pioumanjr = Dec(z, "PIOUMANJR"),
                    Pioumanjf = Dec(z, "PIOUMANJF"),
                    Doppru    = Dec(z, "DOPPRU"),
                    Doppfu    = Dec(z, "DOPPFU"),
                    Netosve   = Dec(z, "NETOSVE"),
                    Netoost   = Dec(z, "NETOOST"),
                    Dinpriprav = Dec(z, "DINPRIPRAV"),
                    Caspriprav = Dec(z, "CASPRIPRAV"),
                    Casuc     = Dec(z, "CASUC"),
                    Casvr     = Dec(z, "CASVR"),
                    Casnoc    = Dec(z, "CASNOC"),
                    Casprod   = Dec(z, "CASPROD"),
                    Casradnap = Dec(z, "CASRADNAP"),
                    Casned    = Dec(z, "CASNED"),
                    Casdor    = Dec(z, "CASDOR"),
                    Cslput    = Dec(z, "CSLPUT"),
                    Caspraz   = Dec(z, "CASPRAZ"),
                    Casbol    = Dec(z, "CASBOL"),
                    Casbol2   = Dec(z, "CASBOL2"),
                    Casplac   = Dec(z, "CASPLAC"),
                    Casplac2  = Dec(z, "CASPLAC2"),
                    Casgod    = Dec(z, "CASGOD"),
                    Casvv     = Dec(z, "CASVV"),
                    Casneplac = Dec(z, "CASNEPLAC"),
                    Cas1      = Dec(z, "CAS1"),
                    Cas2      = Dec(z, "CAS2"),
                    Cas3      = Dec(z, "CAS3"),
                    Cassus    = Dec(z, "CASSUS"),
                }).ToList();
            }
            catch { continue; }
        }

        return [];
    }

    private static DateTime? DatumIsplate(DomainParam? p)
    {
        if (p == null) return null;
        return p.Redispl switch { 1 => p.Dat1, 2 => p.Dat2, 3 => p.Dat3, 4 => p.Dat4, _ => p.Dat1 };
    }

    private static string Str(Dictionary<string, object?> z, string k)
        => z.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static int Int(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v is null) return 0;
        return v switch { decimal d => (int)d, int i => i, long l => (int)l, _ => 0 };
    }

    private static decimal Dec(Dictionary<string, object?> z, string k)
        => z.TryGetValue(k, out var v) && v is decimal d ? d : 0m;

    private static DateTime? Dat(Dictionary<string, object?> z, string k)
        => z.TryGetValue(k, out var v) && v is DateTime dt ? dt : null;
}
