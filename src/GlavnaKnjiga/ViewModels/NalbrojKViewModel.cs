using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>
/// Transcripcija NALBROJK.SCX — ODREDJIVANJE NALOGA (DODAJ).
/// Kada korisnik unese VRNAL:
///   1. Normalizuje na STR(VAL,3,0)
///   2. Traži u nalvrsta.dbf → dobija POCSIF/ZNAKOVI/NAZIV
///   3. Skenira _redovi za VRNAL → nalazi zadnji BRNAL → izračunava sledeći broj
///   4. Datum: zadnji_datum+1 (preskaočući vikend)
///   5. Opis: NAZIV + ' ' + broj
/// Dugme "Odredi": upisuje 4 polja u _currentRow.
/// </summary>
public partial class NalbrojKViewModel : ObservableObject
{
    private readonly string   _firmPath;
    private readonly IReadOnlyList<NalbrojRow> _sviRedovi;
    private readonly NalbrojRow _currentRow;
    private readonly IReadOnlyDictionary<string, NalvrstaRow> _nalvrste;

    [ObservableProperty] private string   _vrnal = string.Empty;
    [ObservableProperty] private string   _brnal = string.Empty;
    [ObservableProperty] private DateTime? _datum = DateTime.Today.AddDays(1);
    [ObservableProperty] private string   _opis  = string.Empty;

    public event Action<bool>? ZatvoriFormu; // true = potvrda, false = odustani
    public event Action? IzborVrsteTrazena;
    internal IEnumerable<NalvrstaRow> DostupneVrste => _nalvrste.Values;

    public NalbrojKViewModel(
        string firmPath,
        IReadOnlyList<NalbrojRow> sviRedovi,
        NalbrojRow currentRow,
        IReadOnlyDictionary<string, NalvrstaRow> nalvrste)
    {
        _firmPath   = firmPath;
        _sviRedovi  = sviRedovi;
        _currentRow = currentRow;
        _nalvrste   = nalvrste;
    }

    // ── txtVrnal.LostFocus ────────────────────────────────────────────
    // Normalizuje VRNAL, traži u nalvrsta, auto-numerira BRNAL+DATUM+OPIS.
    public void OnVrnalLostFocus()
    {
        // STR(VAL(VRNAL),3,0)
        var mvrnal = string.Empty;
        if (int.TryParse(Vrnal.Trim(), out var n) && n != 0)
            mvrnal = n.ToString().PadLeft(3);
        else if (string.IsNullOrWhiteSpace(Vrnal) || n == 0)
        {
            Vrnal = string.Empty;
            IzborVrsteTrazena?.Invoke();
            return;
        }
        else
            mvrnal = Vrnal.PadLeft(3);

        Vrnal = mvrnal;

        if (!_nalvrste.TryGetValue(mvrnal.Trim(), out var vrsta))
        {
            MessageBox.Show(
                $"Vrsta naloga '{mvrnal.Trim()}' ne postoji u nalvrsta.dbf.\nUnesite ispravan VRNAL.",
                "NALBROJK", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AutoNumerisi(mvrnal.Trim(), vrsta);
    }

    [RelayCommand]
    private void IzaberiVrstu() => IzborVrsteTrazena?.Invoke();

    internal void PrimeniIzabranuVrstu(string vrnal)
    {
        Vrnal = vrnal.Trim().PadLeft(3);
        if (_nalvrste.TryGetValue(vrnal.Trim(), out var vrsta))
            AutoNumerisi(vrnal.Trim(), vrsta);
    }

    // ── txtBrnal.LostFocus ──────────────────────────────────────────
    // Ako BRNAL=prazno ili 0 → ostavi na TXTBRNAL (SetFocus ekvivalent → nema ništa).
    // Ako postoji u _sviRedovi (drugačiji record) → "VEĆ POSTOJI".
    public void OnBrnalLostFocus()
    {
        var mb = Brnal.Trim();
        if (string.IsNullOrEmpty(mb) || mb == "0")
        {
            Brnal = string.Empty;
            return;
        }

        // pad na 6
        if (int.TryParse(mb, out var n))
            Brnal = n.ToString().PadLeft(6);
        else
            Brnal = mb.PadLeft(6);

        // provjera duplikata (iskljuci _currentRow)
        var duplikat = _sviRedovi.FirstOrDefault(r =>
            r != _currentRow && r.Brnal.Trim() == Brnal.Trim() && !string.IsNullOrWhiteSpace(r.Brnal.Trim()));
        if (duplikat != null)
            MessageBox.Show($"NALOG {Brnal.Trim()} VEĆ POSTOJI - OTVORITE NOVI",
                "NALBROJK", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    // ── ODREDI (Command1.Click) ──────────────────────────────────────
    [RelayCommand]
    private void Odredi()
    {
        if (string.IsNullOrWhiteSpace(Brnal.Trim()))
        {
            MessageBox.Show("Broj naloga (BRNAL) nije definisan.", "NALBROJK",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _currentRow.Brnal = Brnal;
        _currentRow.Vrnal = Vrnal;
        _currentRow.Datum = Datum;
        _currentRow.Opis  = Opis;

        ZatvoriFormu?.Invoke(true);
    }

    [RelayCommand]
    private void Odustani() => ZatvoriFormu?.Invoke(false);

    // ── AUTO-NUMERISANJE ─────────────────────────────────────────────
    private void AutoNumerisi(string mvrnal, NalvrstaRow vrsta)
    {
        var pocsif = vrsta.Pocsif.Trim();
        var naziv  = vrsta.Naziv.Trim();
        var len1   = pocsif.Length; // dužina prefiksa

        // Nađi zadnji BRNAL za ovaj VRNAL (iskljuci _currentRow)
        var matching = _sviRedovi
            .Where(r => r != _currentRow && r.Vrnal.Trim() == mvrnal)
            .OrderBy(r => r.Brnal)
            .ToList();

        int sledeciBroj;
        DateTime mdatum;

        if (matching.Count > 0)
        {
            var zadnji   = matching.Last();
            var brnal    = zadnji.Brnal.Trim();
            var numDeo   = brnal.Length > len1 ? brnal[len1..] : "0";
            int.TryParse(numDeo, out var zadnjiBroj);
            sledeciBroj = zadnjiBroj + 1;
            mdatum      = SledecaRadnaDatum(zadnji.Datum ?? DateTime.Today);
        }
        else
        {
            sledeciBroj = 1;
            mdatum      = DateTime.Today.AddDays(1);
        }

        var sledBrojStr = sledeciBroj.ToString();
        var nule        = new string('0', Math.Max(0, 6 - len1 - sledBrojStr.Length));
        var mnbrnal     = pocsif + nule + sledBrojStr;

        Brnal = mnbrnal;
        Datum = mdatum;
        Opis  = $"{naziv} {sledeciBroj}";
    }

    // FoxPro: DOW(d)=6(Pet)→+3, DOW(d)=7(Sub)→+2, ostali→+1
    private static DateTime SledecaRadnaDatum(DateTime d) =>
        d.DayOfWeek switch
        {
            DayOfWeek.Friday   => d.AddDays(3),
            DayOfWeek.Saturday => d.AddDays(2),
            _                  => d.AddDays(1)
        };
}
