using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;

namespace GlavnaKnjiga.ViewModels;

/// <summary>
/// Transcripcija NALP2KART.SCX — analytics entry dialog.
/// Otvara se kada: konto je u nalpdefk, VRSTA='AN', DUG=POT=0.
/// Parametri odgovaraju: MADEVIZNO, maimetab, MAVRSTA, mAsifprod, madp iz nalp2kart.prg.
/// </summary>
public partial class Nalp2KartViewModel : ObservableObject
{
    private readonly NalpRow _row;
    private readonly string  _firmPath;
    private readonly IReadOnlyDictionary<string, string>                      _an0Naz;
    private readonly IReadOnlyDictionary<string, string>                      _mtrNaz;
    private readonly IReadOnlyDictionary<string, (string Mesto, string Mp)>   _mesta;

    // ── PROPERTIES VIDLJIVI U VIEW ──────────────────────────
    public string  KontoKod     => _row.Konto.Trim();
    public string  KontoNaziv   { get; private set; } = string.Empty;
    public string  BrNal        => _row.Brnal.Trim();
    public string  Opis         => _row.Opis;
    public bool    DeviznoVidlj => _row.Devizno.Trim() == "D";

    [ObservableProperty] private string  _sifra   = string.Empty;
    [ObservableProperty] private string  _an0Naziv = string.Empty;
    [ObservableProperty] private decimal _dug;
    [ObservableProperty] private decimal _pot;
    [ObservableProperty] private decimal _mtrKod;
    [ObservableProperty] private string  _mtrNaziv = string.Empty;
    [ObservableProperty] private decimal _mpKod;
    [ObservableProperty] private string  _mpNaziv  = string.Empty;
    [ObservableProperty] private string  _dev      = string.Empty;
    [ObservableProperty] private decimal _devkurs;
    [ObservableProperty] private decimal _devdug;
    [ObservableProperty] private decimal _devpot;

    public event Action? ZatvoriFormu;

    public Nalp2KartViewModel(
        NalpRow row,
        string firmPath,
        IReadOnlyDictionary<string, string>                    an0Naz,
        IReadOnlyDictionary<string, string>                    mtrNaz,
        IReadOnlyDictionary<string, (string Mesto, string Mp)> mesta)
    {
        _row      = row;
        _firmPath = firmPath;
        _an0Naz   = an0Naz;
        _mtrNaz   = mtrNaz;
        _mesta    = mesta;

        // init iz reda (odgovara Init procedure u SCX)
        Sifra   = row.Sifra.Trim();
        Dug     = row.Dug;
        Pot     = row.Pot;
        MtrKod  = row.Mtr;
        MpKod   = row.Mp;
        Dev     = row.Dev.Trim();
        Devkurs = row.Devkurs;
        Devdug  = row.Devdug;
        Devpot  = row.Devpot;

        // lookup nazivi
        An0Naziv = _an0Naz.TryGetValue(Sifra, out var a) ? a : string.Empty;
        MtrNaziv = _mtrNaz.TryGetValue(((int)MtrKod).ToString().PadLeft(5), out var mn)
                   ? mn : string.Empty;
        MpNaziv  = _mesta.TryGetValue(((int)MpKod).ToString().PadLeft(2), out var m)
                   ? m.Mesto : string.Empty;

        // konto naziv
        try
        {
            var kpath = System.IO.Path.Combine(_firmPath, "konto.dbf");
            if (System.IO.File.Exists(kpath))
            {
                var kr = new SimpleDbfReader(kpath);
                foreach (var rec in kr.Zapisi())
                    if (rec.DajString("KONTO").Trim() == KontoKod)
                    { KontoNaziv = rec.DajString("NAZIV"); break; }
            }
        }
        catch { }
    }

    // ── TXTSIFRAME LostFocus — traži partnera u AN0 ──────────
    partial void OnSifraChanged(string value)
    {
        var key = value.Trim().PadLeft(5, '0');
        An0Naziv = _an0Naz.TryGetValue(key.TrimStart('0'), out var n)
                   || _an0Naz.TryGetValue(key, out n)
                   ? n : string.Empty;
        _row.Sifra = key;
    }

    // ── TXTDUG LostFocus ──────────────────────────────────────
    partial void OnDugChanged(decimal value)
    {
        if (value != 0) { _row.Pot = 0; Pot = 0; }
        _row.Dug = value;
    }

    // ── TXTPOT LostFocus ──────────────────────────────────────
    partial void OnPotChanged(decimal value)
    {
        if (value != 0) { _row.Dug = 0; Dug = 0; }
        _row.Pot = value;
    }

    // ── TXTMTR LostFocus ─────────────────────────────────────
    partial void OnMtrKodChanged(decimal value)
    {
        var key = ((int)value).ToString().PadLeft(5);
        MtrNaziv = _mtrNaz.TryGetValue(key.Trim(), out var n) ? n : string.Empty;
        _row.Mtr = value;
    }

    // ── TXTMP LostFocus ──────────────────────────────────────
    partial void OnMpKodChanged(decimal value)
    {
        var key = ((int)value).ToString().PadLeft(2);
        MpNaziv = _mesta.TryGetValue(key.Trim(), out var m) ? m.Mesto : string.Empty;
        _row.Mp = value;
    }

    // ── TXTDEV LostFocus — traži kurs iz dev.dbf ─────────────
    partial void OnDevChanged(string value)
    {
        _row.Dev = value;
        if (string.IsNullOrWhiteSpace(value)) return;
        try
        {
            var devPath = System.IO.Path.Combine(_firmPath, "dev.dbf");
            if (!System.IO.File.Exists(devPath)) return;
            var datKey  = _row.Datdok?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd");
            var searchK = value.Trim() + datKey;
            var dr = new SimpleDbfReader(devPath);
            foreach (var rec in dr.Zapisi())
            {
                var k2 = rec.DajString("DEV").Trim() + rec.DajString("DATDOK").Trim();
                if (string.Equals(k2, searchK, StringComparison.OrdinalIgnoreCase))
                {
                    Devkurs      = rec.DajDecimal("KURS");
                    _row.Devkurs = Devkurs;
                    return;
                }
            }
        }
        catch { }
    }

    // ── TXTDEVDUG LostFocus ───────────────────────────────────
    partial void OnDevdugChanged(decimal value)
    {
        if (value != 0)
        {
            _row.Dug    = value * Devkurs;
            _row.Devpot = 0; _row.Pot = 0;
            Dug = _row.Dug; Devpot = 0; Pot = 0;
        }
        _row.Devdug = value;
    }

    // ── TXTDEVPOT LostFocus ───────────────────────────────────
    partial void OnDevpotChanged(decimal value)
    {
        if (value != 0)
        {
            _row.Pot    = value * Devkurs;
            _row.Devdug = 0; _row.Dug = 0;
            Pot = _row.Pot; Devdug = 0; Dug = 0;
        }
        _row.Devpot = value;
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();
}
