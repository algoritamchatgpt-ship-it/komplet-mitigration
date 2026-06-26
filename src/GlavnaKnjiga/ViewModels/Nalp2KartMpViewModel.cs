using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>
/// Transcripcija NALP2KARTMP.SCX — "Knjiženje pazara" dialog za VRSTA='MP'.
/// Otvara se kada: konto je u nalpdefk, VRSTA='MP', DUG=POT=0.
/// </summary>
public partial class Nalp2KartMpViewModel : ObservableObject
{
    private readonly NalpRow _row;
    private readonly IReadOnlyDictionary<string, NalpDefkRow> _nalpdefk;
    private readonly string _firmPath;

    // ── HEADER (readonly, binding to constructor-computed values) ──
    public string KontoKod   => _row.Konto.Trim();
    public string KontoNaziv { get; private set; } = string.Empty;
    public string BrNal      => _row.Brnal.Trim();
    public string Opis       => _row.Opis;

    // ── EDITABLE FIELDS ───────────────────────────────────────────
    [ObservableProperty] private DateTime? _datdok;
    // TXTREDDOK: user types a number; OnChange → DOK='M'+number, lookup nalpdefk
    [ObservableProperty] private int       _redDok;
    // DOK field (auto-filled by TXTREDDOK, readonly display)
    [ObservableProperty] private string    _dok      = string.Empty;
    // LBLPNAZIV (auto-filled from nalpdefk, readonly display)
    [ObservableProperty] private string    _pNaziv   = string.Empty;
    // txtPot — "Uplaćeno"
    [ObservableProperty] private decimal   _pot;
    // txtDatrazduz — "Datum razduženja"
    [ObservableProperty] private DateTime? _datrazduz;
    // txtopisu — "Opis uplate"
    [ObservableProperty] private string    _opisu    = string.Empty;
    // txtdinrazduz — "Iznos razduženja"
    [ObservableProperty] private decimal   _dinrazduz;

    public event Action? ZatvoriFormu;
    public event Action? IzborOpisaTrazena;

    public Nalp2KartMpViewModel(
        NalpRow row,
        IReadOnlyDictionary<string, NalpDefkRow> nalpdefk,
        string firmPath)
    {
        _row      = row;
        _nalpdefk = nalpdefk;
        _firmPath = firmPath;

        // init backing fields directly — avoids triggering partial handlers during ctor
        _datdok    = row.Datdok;
        _pot       = row.Pot;
        _datrazduz = row.Datrazduz;
        _opisu     = row.Opisu;
        _dinrazduz = row.Dinrazduz;

        // parse current DOK → TXTREDDOK value (strip leading 'M')
        var dokStr = row.Dok.Trim();
        _dok = dokStr;
        if (dokStr.StartsWith("M", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(dokStr[1..].Trim(), out var red))
            _redDok = red;

        // initial LBLPNAZIV from nalpdefk.DOK match
        var match = nalpdefk.Values.FirstOrDefault(
            d => string.Equals(d.Dok.Trim(), dokStr, StringComparison.OrdinalIgnoreCase));
        _pNaziv = match?.Pnaziv ?? string.Empty;

        // load konto naziv
        try
        {
            var kpath = Path.Combine(firmPath, "konto.dbf");
            if (File.Exists(kpath))
            {
                var kr = new SimpleDbfReader(kpath);
                foreach (var rec in kr.Zapisi())
                    if (rec.DajString("KONTO").Trim() == KontoKod)
                    { KontoNaziv = rec.DajString("NAZIV"); break; }
            }
        }
        catch { }
    }

    // ── TXTREDDOK LostFocus ───────────────────────────────────────
    // DOK='M'+str(value) → LOCATE nalpdefk FOR DOK=MREDDOK → set LBLPNAZIV + IMETABELE
    partial void OnRedDokChanged(int value)
    {
        var mReddok = "M" + value;
        Dok      = mReddok;
        _row.Dok = mReddok;

        var match = _nalpdefk.Values.FirstOrDefault(
            d => string.Equals(d.Dok.Trim(), mReddok, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            PNaziv         = match.Pnaziv;
            _row.Imetabele = match.Imetabele;
        }
        else
        {
            PNaziv         = "NEMA";
            _row.Imetabele = string.Empty;
        }
    }

    // ── txtDatdok LostFocus ───────────────────────────────────────
    partial void OnDatdokChanged(DateTime? value) => _row.Datdok = value;

    // ── txtPot LostFocus — DATRAZDUZ=DATDOK-1 if empty; DINRAZDUZ=POT ─
    partial void OnPotChanged(decimal value)
    {
        _row.Pot = value;
        if (_row.Datrazduz == null)
        {
            var d = (_row.Datdok ?? DateTime.Today).AddDays(-1);
            _row.Datrazduz = d;
            Datrazduz      = d;
        }
        _row.Dinrazduz = value;
        // assign backing field directly to avoid triggering OnDinrazduzChanged feedback loop
        _dinrazduz = value;
        OnPropertyChanged(nameof(Dinrazduz));
    }

    // ── txtDatrazduz LostFocus ────────────────────────────────────
    partial void OnDatrazduzChanged(DateTime? value) => _row.Datrazduz = value;

    // ── txtOpisu LostFocus — ako je prazno, pozovi NALP2TMMENI2 ──
    partial void OnOpisuChanged(string value)
    {
        _row.Opisu = value;
        if (string.IsNullOrWhiteSpace(value))
            IzborOpisaTrazena?.Invoke();
    }

    [RelayCommand]
    private void IzaberiOpis() => IzborOpisaTrazena?.Invoke();

    internal void OtvoriTmMeni()
    {
        var vm = new Nalp2TmMeniViewModel(this, _firmPath);
        new Views.Nalp2TmMeniWindow(vm).ShowDialog();
    }

    internal void PrimeniTmOpis(string opcija)
    {
        var opis = Nalp2TmMeniViewModel.FormirajOpis(
            opcija, Datdok, out var prazniOpisKnjizenja);
        if (prazniOpisKnjizenja)
        {
            _row.Opis = string.Empty;
            OnPropertyChanged(nameof(Opis));
            return;
        }

        if (opis != null)
            Opisu = opis;
    }

    // ── txtDinrazduz LostFocus — DATRAZDUZ=DATDOK-1 if empty ─────
    partial void OnDinrazduzChanged(decimal value)
    {
        _row.Dinrazduz = value;
        if (_row.Datrazduz == null)
        {
            var d = (_row.Datdok ?? DateTime.Today).AddDays(-1);
            _row.Datrazduz = d;
            Datrazduz      = d;
        }
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();
}
