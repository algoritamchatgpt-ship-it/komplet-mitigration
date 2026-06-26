using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalmtrRow : ObservableObject
{
    [ObservableProperty] private string   _konto   = string.Empty;
    [ObservableProperty] private DateTime? _datdok;
    [ObservableProperty] private string   _brnal   = string.Empty;
    [ObservableProperty] private decimal  _dug;
    [ObservableProperty] private decimal  _pot;
    [ObservableProperty] private decimal  _saldo;

    [ObservableProperty] private decimal _iznos01;
    [ObservableProperty] private decimal _iznos02;
    [ObservableProperty] private decimal _iznos03;
    [ObservableProperty] private decimal _iznos04;
    [ObservableProperty] private decimal _iznos05;
    [ObservableProperty] private decimal _iznos06;
    [ObservableProperty] private decimal _iznos07;
    [ObservableProperty] private decimal _iznos08;
    [ObservableProperty] private decimal _iznos09;
    [ObservableProperty] private decimal _iznos10;
    [ObservableProperty] private decimal _iznos11;
    [ObservableProperty] private decimal _iznos12;
    [ObservableProperty] private decimal _iznos13;
    [ObservableProperty] private decimal _iznos14;
    [ObservableProperty] private decimal _iznos15;
    [ObservableProperty] private decimal _iznos16;
    [ObservableProperty] private decimal _iznos17;
    [ObservableProperty] private decimal _iznos18;
    [ObservableProperty] private decimal _iznos19;
    [ObservableProperty] private decimal _iznos20;
    [ObservableProperty] private decimal _iznos21;
    [ObservableProperty] private decimal _iznos22;
    [ObservableProperty] private decimal _iznos23;
    [ObservableProperty] private decimal _iznos24;
    [ObservableProperty] private decimal _iznos25;
    [ObservableProperty] private decimal _iznos26;
    [ObservableProperty] private decimal _iznos27;
    [ObservableProperty] private decimal _iznos28;
    [ObservableProperty] private decimal _iznos29;
    [ObservableProperty] private decimal _iznos30;

    [ObservableProperty] private string  _naziv   = string.Empty;
    [ObservableProperty] private string  _opis    = string.Empty;
    [ObservableProperty] private decimal _ukupno;
    [ObservableProperty] private decimal _razlika;
    [ObservableProperty] private string  _arhiva  = string.Empty;

    // called when any IZNOS field changes
    partial void OnIznos01Changed(decimal value) => RecalcUkupno();
    partial void OnIznos02Changed(decimal value) => RecalcUkupno();
    partial void OnIznos03Changed(decimal value) => RecalcUkupno();
    partial void OnIznos04Changed(decimal value) => RecalcUkupno();
    partial void OnIznos05Changed(decimal value) => RecalcUkupno();
    partial void OnIznos06Changed(decimal value) => RecalcUkupno();
    partial void OnIznos07Changed(decimal value) => RecalcUkupno();
    partial void OnIznos08Changed(decimal value) => RecalcUkupno();
    partial void OnIznos09Changed(decimal value) => RecalcUkupno();
    partial void OnIznos10Changed(decimal value) => RecalcUkupno();
    partial void OnIznos11Changed(decimal value) => RecalcUkupno();
    partial void OnIznos12Changed(decimal value) => RecalcUkupno();
    partial void OnIznos13Changed(decimal value) => RecalcUkupno();
    partial void OnIznos14Changed(decimal value) => RecalcUkupno();
    partial void OnIznos15Changed(decimal value) => RecalcUkupno();
    partial void OnIznos16Changed(decimal value) => RecalcUkupno();
    partial void OnIznos17Changed(decimal value) => RecalcUkupno();
    partial void OnIznos18Changed(decimal value) => RecalcUkupno();
    partial void OnIznos19Changed(decimal value) => RecalcUkupno();
    partial void OnIznos20Changed(decimal value) => RecalcUkupno();
    partial void OnIznos21Changed(decimal value) => RecalcUkupno();
    partial void OnIznos22Changed(decimal value) => RecalcUkupno();
    partial void OnIznos23Changed(decimal value) => RecalcUkupno();
    partial void OnIznos24Changed(decimal value) => RecalcUkupno();
    partial void OnIznos25Changed(decimal value) => RecalcUkupno();
    partial void OnIznos26Changed(decimal value) => RecalcUkupno();
    partial void OnIznos27Changed(decimal value) => RecalcUkupno();
    partial void OnIznos28Changed(decimal value) => RecalcUkupno();
    partial void OnIznos29Changed(decimal value) => RecalcUkupno();
    partial void OnIznos30Changed(decimal value) => RecalcUkupno();

    public void RecalcUkupno()
    {
        Ukupno = Iznos01 + Iznos02 + Iznos03 + Iznos04 + Iznos05 +
                 Iznos06 + Iznos07 + Iznos08 + Iznos09 + Iznos10 +
                 Iznos11 + Iznos12 + Iznos13 + Iznos14 + Iznos15 +
                 Iznos16 + Iznos17 + Iznos18 + Iznos19 + Iznos20 +
                 Iznos21 + Iznos22 + Iznos23 + Iznos24 + Iznos25 +
                 Iznos26 + Iznos27 + Iznos28 + Iznos29 + Iznos30;
        Razlika = Saldo - Ukupno;
    }
}
