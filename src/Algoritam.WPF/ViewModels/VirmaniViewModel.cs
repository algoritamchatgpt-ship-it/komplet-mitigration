using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

public partial class VirmaniViewModel : ObservableObject
{
    private readonly AppState _appState;
    private DbfTableWriter.DbfSchema? _schema;
    private DbfOptimisticConcurrency.FileSnapshot? _snapshot;

    [ObservableProperty] private ObservableCollection<VirmanStavka> _stavke = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ObrisiSelektovanCommand))]
    private VirmanStavka? _selektovana;
    [ObservableProperty] private string _naslov = "VIRMANI";
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private bool _imaNeacuvana;
    [ObservableProperty] private string _ukupnoText = "0,00";
    [ObservableProperty] private int _brojVirmana;

    public event Action? ZatvaranjeZahtevano;

    public VirmaniViewModel(AppState appState)
    {
        _appState = appState;
        Ucitaj();
    }

    private string FolderPath => _appState.AktivnaFirma?.FolderPath ?? string.Empty;

    private void Ucitaj()
    {
        foreach (var s in Stavke) s.PropertyChanged -= OnStavkaChanged;
        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(FolderPath)) { Poruka = "Nije izabrana firma."; return; }

        var dbfPath = LdObracunDbfReader.PronadjiDbf(FolderPath, "ldvirm.dbf");
        if (dbfPath is null) { Poruka = "ldvirm.dbf nije pronađen."; return; }

        try
        {
            _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(dbfPath);
            _schema = DbfTableWriter.LoadSchema(dbfPath);
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi)
            {
                var st = VirmanStavka.IzZapisa(z);
                st.PropertyChanged += OnStavkaChanged;
                Stavke.Add(st);
            }
            Selektovana = Stavke.FirstOrDefault();
            ImaNeacuvana = false;
            AzurirajZbirove();
            Poruka = $"Učitano {Stavke.Count} virmana.";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    private void OnStavkaChanged(object? sender, PropertyChangedEventArgs e)
    {
        ImaNeacuvana = true;
        if (e.PropertyName is nameof(VirmanStavka.Dug)) AzurirajZbirove();
    }

    private void AzurirajZbirove()
    {
        BrojVirmana = Stavke.Count;
        UkupnoText = Stavke.Sum(s => s.Dug).ToString("N2", CultureInfo.CurrentCulture);
    }

    // ── CRUD ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Dodaj()
    {
        var firma = _appState.AktivnaFirma;
        var st = new VirmanStavka
        {
            DatDok = DateTime.Today,
            DatVal = DateTime.Today,
            Valuta = "941",
            NazRacZ = (firma?.Naziv ?? string.Empty).Trim(),
            ZiroRacZ = (firma?.ZiroRacun ?? string.Empty).Trim(),
            Stampa = "*",
        };
        st.PropertyChanged += OnStavkaChanged;
        Stavke.Add(st);
        Selektovana = st;
        ImaNeacuvana = true;
        AzurirajZbirove();
        Poruka = "Dodat novi virman.";
    }

    private bool MozeObrisiSelektovan() => Selektovana != null;

    [RelayCommand(CanExecute = nameof(MozeObrisiSelektovan))]
    private void ObrisiSelektovan()
    {
        if (Selektovana is null) return;
        Selektovana.PropertyChanged -= OnStavkaChanged;
        var idx = Stavke.IndexOf(Selektovana);
        Stavke.Remove(Selektovana);
        Selektovana = idx < Stavke.Count ? Stavke[idx] : Stavke.LastOrDefault();
        ImaNeacuvana = true;
        AzurirajZbirove();
        Poruka = "Obrisana selektovana stavka.";
    }

    [RelayCommand]
    private void ObrisiSve()
    {
        if (Stavke.Count == 0) { Poruka = "Nema virmana za brisanje."; return; }
        var r = MessageBox.Show($"Brisanje svih {Stavke.Count} virmana?", "BRISANJE SVE",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (r != MessageBoxResult.Yes) return;
        foreach (var s in Stavke) s.PropertyChanged -= OnStavkaChanged;
        Stavke.Clear();
        Selektovana = null;
        ImaNeacuvana = true;
        AzurirajZbirove();
        Poruka = "Obrisani svi virmani.";
    }

    [RelayCommand]
    private void ObrisiPrveNe()
    {
        var prvi = Stavke.FirstOrDefault(s => s.Stampa.Trim() != "*");
        if (prvi is null) { Poruka = "Nema neodštampanih virmana."; return; }
        prvi.PropertyChanged -= OnStavkaChanged;
        Stavke.Remove(prvi);
        Selektovana = Stavke.FirstOrDefault();
        ImaNeacuvana = true;
        AzurirajZbirove();
        Poruka = "Obrisan prvi neodštampani virman.";
    }

    // ── POZIVI ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void PozivOdobrenje()
    {
        int cnt = 0;
        foreach (var s in Stavke.Where(v => !string.IsNullOrWhiteSpace(v.ZiroRac)))
        {
            var poziv = GenerisiPoziv(s.ZiroRac, s.DatDok);
            if (s.PozivO != poziv) { s.PozivO = poziv; cnt++; }
        }
        ImaNeacuvana = cnt > 0;
        Poruka = cnt > 0 ? $"Poziv na br. odobrenja generisan za {cnt} virmana." : "Nema promena.";
    }

    [RelayCommand]
    private void PozivZaduzenje()
    {
        int cnt = 0;
        foreach (var s in Stavke.Where(v => !string.IsNullOrWhiteSpace(v.ZiroRacZ)))
        {
            var poziv = GenerisiPoziv(s.ZiroRacZ, s.DatDok);
            if (s.PozivZ != poziv) { s.PozivZ = poziv; cnt++; }
        }
        ImaNeacuvana = cnt > 0;
        Poruka = cnt > 0 ? $"Poziv na br. zaduženja generisan za {cnt} virmana." : "Nema promena.";
    }

    private static string GenerisiPoziv(string ziroRac, DateTime datDok)
    {
        // Format: 97-GGMM-poslednje4cifre žiro računa (srpski standard)
        var god = datDok != DateTime.MinValue ? datDok.Year % 100 : DateTime.Today.Year % 100;
        var mes = datDok != DateTime.MinValue ? datDok.Month : DateTime.Today.Month;
        var ciste = new string(ziroRac.Where(char.IsDigit).ToArray());
        var sufiks = ciste.Length >= 4 ? ciste[^4..] : ciste.PadLeft(4, '0');
        return $"97-{god:D2}{mes:D2}-{sufiks}";
    }

    // ── ZAOKRUŽI ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ZaokruziIznose()
    {
        int cnt = 0;
        foreach (var s in Stavke)
        {
            var zaokr = Math.Round(s.Dug, 0, MidpointRounding.AwayFromZero);
            if (zaokr != s.Dug) { s.Dug = zaokr; cnt++; }
        }
        ImaNeacuvana = cnt > 0;
        Poruka = cnt > 0 ? $"Zaokruženo {cnt} iznosa." : "Svi iznosi su već celi.";
        AzurirajZbirove();
    }

    // ── SAČUVAJ ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Sacuvaj()
    {
        var dbfPath = LdObracunDbfReader.PronadjiDbf(FolderPath, "ldvirm.dbf");
        if (dbfPath is null) { Poruka = "ldvirm.dbf nije pronađen."; return; }

        if (_snapshot != null && DbfOptimisticConcurrency.HasFileChanged(dbfPath, _snapshot))
        {
            var r = MessageBox.Show(
                "Fajl ldvirm.dbf je izmenjen od strane drugog korisnika.\nNastaviti sa čuvanjem (prepisati)?",
                "Upozorenje — dual korisnici", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r != MessageBoxResult.Yes) return;
        }

        try
        {
            var schema = _schema ?? DbfTableWriter.LoadSchema(dbfPath);
            DbfTableWriter.WriteTable(dbfPath, schema, Stavke.ToList(),
                static (s, fn) => fn switch
                {
                    "STAMPA"   => s.Stampa,
                    "VRSTA"    => s.Vrsta,
                    "ZIRORAC"  => s.ZiroRac,
                    "MODELO"   => s.Modelo,
                    "POZIVO"   => s.PozivO,
                    "DATDOK"   => s.DatDok != DateTime.MinValue ? s.DatDok : (object?)null,
                    "DUG"      => s.Dug,
                    "SIF1"     => s.Sif1,
                    "MODELZ"   => s.ModelZ,
                    "POZIVZ"   => s.PozivZ,
                    "NAZRAC"   => s.NazRac,
                    "SVRHA"    => s.Svrha,
                    "RAZ"      => s.Raz,
                    "NAZRACZ"  => s.NazRacZ,
                    "ZIRORACZ" => s.ZiroRacZ,
                    "UKUPNO"   => s.Ukupno,
                    "DATVAL"   => s.DatVal != DateTime.MinValue ? s.DatVal : (object?)null,
                    "VALUTA"   => s.Valuta,
                    "PLAC"     => s.Plac,
                    "MESTO"    => s.Mesto,
                    "PRENETO"  => s.Preneto,
                    "IDBR"     => (decimal)s.IdBr,
                    _ => null
                });
            _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(dbfPath);
            ImaNeacuvana = false;
            Poruka = $"Sačuvano {Stavke.Count} virmana u ldvirm.dbf.";
        }
        catch (Exception ex) { Poruka = $"Greška pri čuvanju: {ex.Message}"; }
    }

    // ── OSVEZI ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Osvezi()
    {
        if (ImaNeacuvana)
        {
            var r = MessageBox.Show("Ima nesačuvanih izmena. Učitati ponovo sa diska?",
                "Osvežavanje", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;
        }
        Ucitaj();
    }

    // ── PREUZIMANJE (F2) ────────────────────────────────────────────────────

    [RelayCommand]
    private void Preuzimanje()
    {
        if (Stavke.Count > 0)
        {
            var r = MessageBox.Show(
                $"Postoji {Stavke.Count} virmana. Preuzimanje briše sve i puni iznova.\n\nNastaviti?",
                "PREUZIMANJE F2", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r != MessageBoxResult.Yes) return;
        }

        try
        {
            var novi = GenerisiVirmane();
            foreach (var s in Stavke) s.PropertyChanged -= OnStavkaChanged;
            Stavke.Clear();
            foreach (var s in novi)
            {
                s.PropertyChanged += OnStavkaChanged;
                Stavke.Add(s);
            }
            Selektovana = Stavke.FirstOrDefault();
            ImaNeacuvana = true;
            AzurirajZbirove();
            Poruka = $"Preuzimanje završeno — {Stavke.Count} virmana generisano.";
        }
        catch (Exception ex) { Poruka = $"Greška pri preuzimanju: {ex.Message}"; }
    }

    private List<VirmanStavka> GenerisiVirmane()
    {
        var rezultat = new List<VirmanStavka>();
        var folder = FolderPath;
        var firma = _appState.AktivnaFirma;
        if (string.IsNullOrWhiteSpace(folder) || firma is null) return rezultat;

        // 1. Parametri
        var (mesec, isplata, nazmes, godina, datDok, ziroracz, konacna, svrhaBase) =
            UcitajParametre(folder, firma);

        // 2. Neto zarade po bankama (ldspis + an0)
        rezultat.AddRange(GenerisiZaradeVirmane(folder, firma, datDok, ziroracz, nazmes, godina, isplata, svrhaBase));

        // 3. Krediti po bankama (ldkred + an0)
        rezultat.AddRange(GenerisiKreditVirmane(folder, firma, datDok, ziroracz, nazmes, godina, isplata, konacna, svrhaBase));

        // 4. Samodoprinos (ldsamod)
        rezultat.AddRange(GenerisiSamodoprinosVirmane(folder, firma, datDok, ziroracz, mesec, isplata, nazmes, godina));

        // 5. Ukloni DUG=0
        return rezultat.Where(v => v.Dug != 0m).ToList();
    }

    private (int mesec, int isplata, string nazmes, string godina, DateTime datDok,
             string ziroracz, string konacna, string svrhaBase)
        UcitajParametre(string folder, Algoritam.Domain.Entities.Firma firma)
    {
        int mesec = DateTime.Now.Month, isplata = 1;
        string nazmes = "", godina = DateTime.Now.Year.ToString(), konacna = "N";
        DateTime datDok = DateTime.Today;

        var paramPath = LdObracunDbfReader.PronadjiDbf(folder, "ldparam.dbf");
        if (paramPath != null)
        {
            var par = DbfReader.CitajSveZapise(paramPath).FirstOrDefault();
            if (par != null)
            {
                mesec   = IntZ(par, "MESEC");
                isplata = IntZ(par, "ISPLATA");
                nazmes  = StrZ(par, "NAZMES");
                godina  = StrZ(par, "GODINA");
                konacna = StrZ(par, "KONACNA");
                var aktivrac = IntZ(par, "AKTIVRAC");
                var dat = isplata switch
                {
                    2 => DatZ(par, "DAT2"),
                    3 => DatZ(par, "DAT3"),
                    4 => DatZ(par, "DAT4"),
                    _ => DatZ(par, "DAT1"),
                };
                if (dat != DateTime.MinValue) datDok = dat;

                // Žiro račun firme po aktivnom računu
            }
        }

        var ziroracz = isplata == 3
            ? firma.ZiroRacunBolovanje.Trim()
            : UcitajParametre_ZiroRac(folder, firma, isplata);

        if (string.IsNullOrWhiteSpace(ziroracz))
            ziroracz = firma.ZiroRacun.Trim();

        var naz = nazmes.Trim();
        var god = godina.Trim();
        var svrhaBase = $"ZARADA ZA {naz} {god} {isplata}".TrimEnd();

        return (mesec, isplata, naz, god, datDok, ziroracz, konacna, svrhaBase);
    }

    private static string UcitajParametre_ZiroRac(string folder,
        Algoritam.Domain.Entities.Firma firma, int isplata)
    {
        var paramPath = LdObracunDbfReader.PronadjiDbf(folder, "ldparam.dbf");
        if (paramPath is null) return firma.ZiroRacun.Trim();
        var par = DbfReader.CitajSveZapise(paramPath).FirstOrDefault();
        if (par is null) return firma.ZiroRacun.Trim();
        var aktivrac = IntZ(par, "AKTIVRAC");
        return aktivrac switch
        {
            2 => firma.ZiroRacun2.Trim(),
            3 => firma.ZiroRacun3.Trim(),
            4 => firma.ZiroRacun4.Trim(),
            5 => firma.ZiroRacun5.Trim(),
            6 => firma.ZiroRacun6.Trim(),
            _ => firma.ZiroRacun.Trim(),
        };
    }

    private static List<VirmanStavka> GenerisiZaradeVirmane(
        string folder, Algoritam.Domain.Entities.Firma firma,
        DateTime datDok, string ziroracz,
        string nazmes, string godina, int isplata, string svrhaBase)
    {
        var rezultat = new List<VirmanStavka>();

        var spisPath = LdObracunDbfReader.PronadjiDbf(folder, "ldspis.dbf");
        if (spisPath is null) return rezultat;

        var banke = UcitajBanke(folder);
        var spis = DbfReader.CitajSveZapise(spisPath);

        // Grupiši po SIFRA (banka), sumiraj IZNOS
        var grupe = spis
            .GroupBy(z => StrZ(z, "SIFRA"))
            .Where(g => !string.IsNullOrWhiteSpace(g.Key));

        foreach (var g in grupe)
        {
            var iznos = g.Sum(z => DecZ(z, "IZNOS"));
            if (iznos == 0m) continue;

            banke.TryGetValue(g.Key, out var banka);
            var nazrac = banka is not null
                ? $"{banka.Naziv} {banka.Mesto} - ZARADA".TrimEnd()
                : $"BANKA {g.Key} - ZARADA";

            var naz2 = nazmes.PadRight(9)[..Math.Min(9, nazmes.Length)];
            var svrha = $"ISPLATA ZA {naz2} {godina} {isplata} ISPLATA".TrimEnd();

            rezultat.Add(new VirmanStavka
            {
                Stampa   = "*",
                Vrsta    = "7",
                Sif1     = "240",
                ZiroRac  = banka?.Ziro ?? string.Empty,
                NazRac   = nazrac,
                Svrha    = svrha,
                Dug      = iznos,
                DatDok   = datDok,
                DatVal   = datDok,
                Valuta   = "941",
                ZiroRacZ = ziroracz,
                NazRacZ  = firma.Naziv.Trim(),
            });
        }

        return rezultat;
    }

    private static List<VirmanStavka> GenerisiKreditVirmane(
        string folder, Algoritam.Domain.Entities.Firma firma,
        DateTime datDok, string ziroracz,
        string nazmes, string godina, int isplata, string konacna, string svrhaBase)
    {
        var rezultat = new List<VirmanStavka>();

        var kredPath = LdObracunDbfReader.PronadjiDbf(folder, "ldkred.dbf");
        if (kredPath is null) return rezultat;

        var banke = UcitajBanke(folder);
        var krediti = DbfReader.CitajSveZapise(kredPath)
            .Where(z => StrZ(z, "ZAODBITAK") == "*" && StrZ(z, "ARHIVA") != "*");

        // Grupiši po ŠIFRA (partner/banka)
        var grupe = krediti
            .GroupBy(z => StrZ(z, "SIFRA"))
            .Where(g => !string.IsNullOrWhiteSpace(g.Key));

        foreach (var g in grupe)
        {
            var iznos = konacna.Trim().ToUpperInvariant() == "D"
                ? g.Sum(z => DecZ(z, "AKTIVRATA"))
                : g.Sum(z => DecZ(z, "AKONTRATA"));
            if (iznos == 0m) continue;

            // Uzmi prvi zapis za modelo/partija
            var prvi = g.First();
            var modelo = StrZ(prvi, "MODELO");
            var partija = StrZ(prvi, "PARTIJA");

            banke.TryGetValue(g.Key, out var banka);
            var nazrac = banka is not null
                ? $"{banka.Naziv} {banka.Mesto} - KREDIT".TrimEnd()
                : $"BANKA {g.Key} - KREDIT";

            var naz2 = nazmes.PadRight(10)[..Math.Min(10, nazmes.Length)];
            var svrha = $"KREDIT ZA {naz2} {godina} {isplata} ISPLATA".TrimEnd();

            rezultat.Add(new VirmanStavka
            {
                Stampa   = "*",
                Vrsta    = "8",
                Sif1     = "241",
                ZiroRac  = banka?.Ziro ?? string.Empty,
                NazRac   = nazrac,
                Svrha    = svrha,
                PozivO   = partija,
                Modelo   = modelo,
                Dug      = iznos,
                DatDok   = datDok,
                DatVal   = datDok,
                Valuta   = "941",
                ZiroRacZ = ziroracz,
                NazRacZ  = firma.Naziv.Trim(),
            });
        }

        return rezultat;
    }

    private static List<VirmanStavka> GenerisiSamodoprinosVirmane(
        string folder, Algoritam.Domain.Entities.Firma firma,
        DateTime datDok, string ziroracz,
        int mesec, int isplata, string nazmes, string godina)
    {
        var rezultat = new List<VirmanStavka>();

        var samPath = LdObracunDbfReader.PronadjiDbf(folder, "ldsamod.dbf");
        if (samPath is null) return rezultat;

        var zapisi = DbfReader.CitajSveZapise(samPath)
            .Where(z => IntZ(z, "MESEC") == mesec && IntZ(z, "ISPLATA") == isplata);

        foreach (var z in zapisi)
        {
            var iznos = DecZ(z, "SAMODOP");
            if (iznos == 0m) continue;

            var samoNaz = StrZ(z, "SAMONAZ");
            var ziroRac = StrZ(z, "ZIRORAC");
            var naz2 = nazmes.PadRight(10)[..Math.Min(10, nazmes.Length)];
            var svrha = $"SAMOD. ZA {naz2} {godina} {isplata} ISPLATA".TrimEnd();

            rezultat.Add(new VirmanStavka
            {
                Stampa   = "*",
                Vrsta    = "6",
                ZiroRac  = ziroRac,
                NazRac   = $"MESNI SAMODOPRINOS - {samoNaz}".TrimEnd(),
                Svrha    = svrha,
                Dug      = iznos,
                DatDok   = datDok,
                DatVal   = datDok,
                Valuta   = "941",
                ZiroRacZ = ziroracz,
                NazRacZ  = firma.Naziv.Trim(),
            });
        }

        return rezultat;
    }

    // ── JEDAN VIRMAN POPUNI ─────────────────────────────────────────────────

    [RelayCommand]
    private void JedanVirmanPopuni()
    {
        if (Selektovana is null)
        {
            MessageBox.Show("Izaberite virman iz liste.", "Jedan virman",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var firma = _appState.AktivnaFirma;
        var vm = new VirmanJedanViewModel(
            Selektovana,
            firma?.Naziv ?? string.Empty,
            firma?.ZiroRacun ?? string.Empty,
            firma?.Mesto ?? string.Empty);

        var view = new Views.Zarade.VirmanJedanView { DataContext = vm };
        vm.ZatvaranjeZahtevano += () => view.Close();
        view.ShowDialog();

        if (vm.Potvrdjeno)
        {
            vm.CopyBackTo(Selektovana);
            ImaNeacuvana = true;
            AzurirajZbirove();
            Poruka = "Virman ažuriran.";
        }
    }

    // ── JEDAN KOMPLET (ŠTAMPA) ──────────────────────────────────────────────

    [RelayCommand]
    private void JedanKomplet()
    {
        if (Selektovana is null || Selektovana.Dug == 0)
        {
            MessageBox.Show("Izaberite virman sa iznosom > 0.", "Jedan komplet",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var firma = _appState.AktivnaFirma;
        var vm = new VirmanJedanViewModel(
            Selektovana,
            firma?.Naziv ?? string.Empty,
            firma?.ZiroRacun ?? string.Empty,
            firma?.Mesto ?? string.Empty);

        var dlg = new System.Windows.Controls.PrintDialog();
        if (dlg.ShowDialog() != true) return;

        var fd = vm.GradeFlowDocument();
        var pag = ((System.Windows.Documents.IDocumentPaginatorSource)fd).DocumentPaginator;
        pag.PageSize = new System.Windows.Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);
        dlg.PrintDocument(pag, "Nalog za prenos");
        Poruka = "Virman poslat na štampu.";
    }

    // ── ŠTAMPA KOMPLET (sve) ────────────────────────────────────────────────

    [RelayCommand]
    private void StampaKomplet()
    {
        var zaStampu = Stavke.Where(s => s.Dug > 0m).ToList();
        if (zaStampu.Count == 0)
        {
            MessageBox.Show("Nema virmana sa iznosom > 0 za štampu.", "Štampa komplet",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new System.Windows.Controls.PrintDialog();
        if (dlg.ShowDialog() != true) return;

        var firma = _appState.AktivnaFirma;
        int stampano = 0;
        foreach (var v in zaStampu)
        {
            var vm = new VirmanJedanViewModel(
                v,
                firma?.Naziv ?? string.Empty,
                firma?.ZiroRacun ?? string.Empty,
                firma?.Mesto ?? string.Empty);

            var fd = vm.GradeFlowDocument();
            var pag = ((System.Windows.Documents.IDocumentPaginatorSource)fd).DocumentPaginator;
            pag.PageSize = new System.Windows.Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);
            dlg.PrintDocument(pag, $"Nalog za prenos {++stampano}/{zaStampu.Count}");
        }
        Poruka = $"Štampano {stampano} virmana.";
    }

    // ── IZVOZ RAIFFEISEN ────────────────────────────────────────────────────

    [RelayCommand]
    private void IzvozRaiffeisen()
    {
        if (Stavke.Count == 0) { Poruka = "Nema virmana za izvoz."; return; }

        var dlg = new SaveFileDialog
        {
            Title = "Izvoz Raiffeisen",
            Filter = "Tekstualni fajl (*.txt)|*.txt|Svi fajlovi (*.*)|*.*",
            FileName = $"raiffeisen_{DateTime.Now:yyyyMMdd}.txt",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var sb = new StringBuilder();
            foreach (var v in Stavke.Where(s => s.Dug > 0m))
            {
                // Format kompatibilan sa Raiffeisen IPS standardom
                var iznos = v.Dug.ToString("F2", CultureInfo.InvariantCulture);
                var dat   = v.DatDok != DateTime.MinValue
                    ? v.DatDok.ToString("dd.MM.yyyy") : DateTime.Today.ToString("dd.MM.yyyy");
                sb.AppendLine(
                    $"{v.ZiroRacZ.Trim()}\t{v.NazRacZ.Trim()}\t" +
                    $"{v.ZiroRac.Trim()}\t{v.NazRac.Trim()}\t" +
                    $"{iznos}\t{dat}\t{v.Svrha.Trim()}\t" +
                    $"{v.Modelo.Trim()}\t{v.PozivO.Trim()}\t{v.Valuta.Trim()}");
            }
            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            Poruka = $"Izvoz završen: {dlg.FileName}";
        }
        catch (Exception ex) { Poruka = $"Greška pri izvozu: {ex.Message}"; }
    }

    // ── IZVOZ ASSECO ────────────────────────────────────────────────────────

    [RelayCommand]
    private void IzvozAsseco()
    {
        if (Stavke.Count == 0) { Poruka = "Nema virmana za izvoz."; return; }

        var dlg = new SaveFileDialog
        {
            Title = "Izvoz Asseco (TXT)",
            Filter = "Tekstualni fajl (*.txt)|*.txt|Svi fajlovi (*.*)|*.*",
            FileName = $"asseco_{DateTime.Now:yyyyMMdd}.txt",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var sb = new StringBuilder();
            int redni = 1;
            foreach (var v in Stavke.Where(s => s.Dug > 0m))
            {
                var iznos = v.Dug.ToString("N2", new CultureInfo("sr-Latn-RS"));
                var dat = v.DatDok != DateTime.MinValue
                    ? v.DatDok.ToString("dd.MM.yyyy") : DateTime.Today.ToString("dd.MM.yyyy");
                var poziv = string.IsNullOrWhiteSpace(v.PozivO) ? "/" : v.PozivO.Trim();
                var model = string.IsNullOrWhiteSpace(v.Modelo) ? "97" : v.Modelo.Trim();
                // Asseco semicolon-delimited format
                sb.AppendLine(string.Join(";",
                    redni++,
                    v.ZiroRacZ.Trim(),
                    v.NazRacZ.Trim(),
                    v.ZiroRac.Trim(),
                    v.NazRac.Trim(),
                    iznos,
                    v.Valuta.Trim().PadRight(3)[..3],
                    dat,
                    v.Svrha.Trim(),
                    model,
                    poziv,
                    "289"));
            }
            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.GetEncoding(1250));
            Poruka = $"Izvoz Asseco završen: {dlg.FileName}";
        }
        catch (Exception ex) { Poruka = $"Greška pri izvozu Asseco: {ex.Message}"; }
    }

    // ── IZVOZ HALCOM ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void IzvozHalcom()
    {
        if (Stavke.Count == 0) { Poruka = "Nema virmana za izvoz."; return; }

        var dlg = new SaveFileDialog
        {
            Title = "Izvoz Halcom (TXT)",
            Filter = "Tekstualni fajl (*.txt)|*.txt|Svi fajlovi (*.*)|*.*",
            FileName = $"halcom_{DateTime.Now:yyyyMMdd}.txt",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var sb = new StringBuilder();
            foreach (var v in Stavke.Where(s => s.Dug > 0m))
            {
                var iznos = v.Dug.ToString("F2", CultureInfo.InvariantCulture);
                var dat = v.DatDok != DateTime.MinValue
                    ? v.DatDok.ToString("yyyyMMdd") : DateTime.Today.ToString("yyyyMMdd");
                var poziv = string.IsNullOrWhiteSpace(v.PozivO) ? "/" : v.PozivO.Trim();
                var model = string.IsNullOrWhiteSpace(v.Modelo) ? "97" : v.Modelo.Trim();
                // Halcom pipe-delimited format
                sb.AppendLine(string.Join("|",
                    v.ZiroRacZ.Trim(),
                    v.NazRacZ.Trim(),
                    v.ZiroRac.Trim(),
                    v.NazRac.Trim(),
                    iznos,
                    v.Valuta.Trim().PadRight(3)[..3],
                    dat,
                    v.Svrha.Trim(),
                    $"{model}/{poziv}",
                    string.Empty));
            }
            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.GetEncoding(1250));
            Poruka = $"Izvoz Halcom završen: {dlg.FileName}";
        }
        catch (Exception ex) { Poruka = $"Greška pri izvozu Halcom: {ex.Message}"; }
    }

    // ── ZATVORI ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Zatvori()
    {
        if (ImaNeacuvana)
        {
            var r = MessageBox.Show("Ima nesačuvanih izmena. Sačuvati pre zatvaranja?",
                "Zatvaranje", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (r == MessageBoxResult.Cancel) return;
            if (r == MessageBoxResult.Yes) Sacuvaj();
        }
        ZatvaranjeZahtevano?.Invoke();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private sealed record BankaInfo(string Naziv, string Mesto, string Ziro);

    private static Dictionary<string, BankaInfo> UcitajBanke(string folder)
    {
        var map = new Dictionary<string, BankaInfo>(StringComparer.OrdinalIgnoreCase);
        var an0 = LdObracunDbfReader.PronadjiDbf(folder, "an0.dbf");
        if (an0 is null) return map;
        foreach (var z in DbfReader.CitajSveZapise(an0))
        {
            var sifra = StrZ(z, "SIFRA");
            if (string.IsNullOrWhiteSpace(sifra)) continue;
            var ziro = StrZ(z, "ZIRO");
            if (string.IsNullOrWhiteSpace(ziro)) ziro = StrZ(z, "TEKRAC");
            map[sifra] = new BankaInfo(StrZ(z, "NAZIV"), StrZ(z, "MESTO"), ziro);
        }
        return map;
    }

    private static string StrZ(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static int IntZ(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0;
        if (v is decimal d) return (int)d;
        if (v is int i) return i;
        return 0;
    }

    private static decimal DecZ(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;

    private static DateTime DatZ(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is DateTime dt ? dt : DateTime.MinValue;
}

// ── Model ────────────────────────────────────────────────────────────────────

public partial class VirmanStavka : ObservableObject
{
    [ObservableProperty] private string _stampa = string.Empty;
    [ObservableProperty] private string _vrsta = string.Empty;
    [ObservableProperty] private string _ziroRac = string.Empty;
    [ObservableProperty] private string _modelo = string.Empty;
    [ObservableProperty] private string _pozivO = string.Empty;
    [ObservableProperty] private DateTime _datDok = DateTime.MinValue;
    [ObservableProperty] private decimal _dug;
    [ObservableProperty] private string _sif1 = string.Empty;
    [ObservableProperty] private string _modelZ = string.Empty;
    [ObservableProperty] private string _pozivZ = string.Empty;
    [ObservableProperty] private string _nazRac = string.Empty;
    [ObservableProperty] private string _svrha = string.Empty;
    [ObservableProperty] private string _raz = string.Empty;
    [ObservableProperty] private string _nazRacZ = string.Empty;
    [ObservableProperty] private string _ziroRacZ = string.Empty;
    [ObservableProperty] private decimal _ukupno;
    [ObservableProperty] private DateTime _datVal = DateTime.MinValue;
    [ObservableProperty] private string _valuta = string.Empty;
    [ObservableProperty] private decimal _plac;
    [ObservableProperty] private string _mesto = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private long _idBr;

    public static VirmanStavka IzZapisa(Dictionary<string, object?> z) => new()
    {
        Stampa   = Str(z, "STAMPA"),
        Vrsta    = Str(z, "VRSTA"),
        ZiroRac  = Str(z, "ZIRORAC"),
        Modelo   = Str(z, "MODELO"),
        PozivO   = Str(z, "POZIVO"),
        DatDok   = Dat(z, "DATDOK"),
        Dug      = Dec(z, "DUG"),
        Sif1     = Str(z, "SIF1"),
        ModelZ   = Str(z, "MODELZ"),
        PozivZ   = Str(z, "POZIVZ"),
        NazRac   = Str(z, "NAZRAC"),
        Svrha    = Str(z, "SVRHA"),
        Raz      = Str(z, "RAZ"),
        NazRacZ  = Str(z, "NAZRACZ"),
        ZiroRacZ = Str(z, "ZIRORACZ"),
        Ukupno   = Dec(z, "UKUPNO"),
        DatVal   = Dat(z, "DATVAL"),
        Valuta   = Str(z, "VALUTA"),
        Plac     = Dec(z, "PLAC"),
        Mesto    = Str(z, "MESTO"),
        Preneto  = Str(z, "PRENETO"),
        IdBr     = Lng(z, "IDBR"),
    };

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s : string.Empty;
    private static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;
    private static DateTime Dat(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is DateTime dt ? dt : DateTime.MinValue;
    private static long Lng(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0L;
        if (v is decimal d) return (long)d;
        if (v is long l) return l;
        return 0L;
    }
}
