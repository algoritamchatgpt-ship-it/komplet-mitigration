using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>
/// Transcripcija NALBROJK2.SCX — KARTICA NALOGA 2 (edit existing nalog).
/// Prikazuje i edituje 4 polja jednog NalbrojRow: VRNAL, BRNAL, DATUM, OPIS.
/// IZLAZ: REPLACE VRNAL/BRNAL/DATUM/OPIS nazad u red.
/// txtVrnal.LostFocus: STR(VAL,3,0), ako 0 → otvori izbor NALVRSTA.
/// txtBrnal.LostFocus: provjera duplikata.
/// </summary>
public partial class NalbrojK2ViewModel : ObservableObject
{
    private readonly NalbrojRow _red;
    private readonly IReadOnlyList<NalbrojRow> _sviRedovi;
    private readonly IReadOnlyDictionary<string, NalvrstaRow> _nalvrste;

    [ObservableProperty] private string   _vrnal = string.Empty;
    [ObservableProperty] private string   _brnal = string.Empty;
    [ObservableProperty] private DateTime? _datum;
    [ObservableProperty] private string   _opis  = string.Empty;

    public event Action<bool>? ZatvoriFormu;
    public event Action? IzborVrsteTrazena;
    internal IEnumerable<NalvrstaRow> DostupneVrste => _nalvrste.Values;

    public NalbrojK2ViewModel(
        NalbrojRow red,
        IReadOnlyList<NalbrojRow> sviRedovi,
        IReadOnlyDictionary<string, NalvrstaRow> nalvrste)
    {
        _red        = red;
        _sviRedovi  = sviRedovi;
        _nalvrste   = nalvrste;

        // Init: SELECT NALBROJ; SET RELATION TO; puni tekstualna polja
        Vrnal = red.Vrnal;
        Brnal = red.Brnal;
        Datum = red.Datum;
        Opis  = red.Opis;
    }

    // txtVrnal.LostFocus
    public void OnVrnalLostFocus()
    {
        // MVRNAL=str(val(alltrim(VRNAL)),3,0)
        var mvrnal = string.Empty;
        if (int.TryParse(Vrnal.Trim(), out var n))
        {
            if (n == 0)
            {
                Vrnal = string.Empty;
                IzborVrsteTrazena?.Invoke();
                return;
            }
            mvrnal = n.ToString().PadLeft(3);
        }
        else
            mvrnal = Vrnal.PadLeft(3);

        Vrnal = mvrnal;
    }

    [RelayCommand]
    private void IzaberiVrstu() => IzborVrsteTrazena?.Invoke();

    internal void PrimeniIzabranuVrstu(string vrnal)
    {
        Vrnal = vrnal.Trim().PadLeft(3);
    }

    // txtBrnal.LostFocus
    public void OnBrnalLostFocus()
    {
        var mb = Brnal.Trim();
        if (string.IsNullOrEmpty(mb))
        {
            Brnal = string.Empty;
            return;
        }

        if (int.TryParse(mb, out var n))
            Brnal = n.ToString().PadLeft(6);
        else
            Brnal = mb.PadLeft(6);

        // IF FOUND() AND RECNO()<>MREC → "VEĆ POSTOJI"
        var drugi = _sviRedovi.FirstOrDefault(r =>
            r != _red && r.Brnal.Trim() == Brnal.Trim() && !string.IsNullOrWhiteSpace(Brnal.Trim()));
        if (drugi != null)
            MessageBox.Show($"NALOG {Brnal.Trim()} VEĆ POSTOJI - OTVORITE NOVI",
                "NALBROJK2", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    // IZLAZ — REPLACE 4 polja nazad u originalni red
    [RelayCommand]
    private void Izlaz()
    {
        _red.Vrnal = Vrnal;
        _red.Brnal = Brnal;
        _red.Datum = Datum;
        _red.Opis  = Opis;
        ZatvoriFormu?.Invoke(true);
    }

    [RelayCommand]
    private void Odustani() => ZatvoriFormu?.Invoke(false);
}
