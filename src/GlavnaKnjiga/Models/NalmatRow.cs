using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalmatRow : ObservableObject
{
    [ObservableProperty] private string _konto = string.Empty;
    [ObservableProperty] private decimal _cena;
    [ObservableProperty] private decimal _ulaz;
    [ObservableProperty] private decimal _izlaz;
    [ObservableProperty] private decimal _ukupnoD;
    [ObservableProperty] private decimal _ukupnoP;
    [ObservableProperty] private decimal _dug;
    [ObservableProperty] private decimal _pot;
    [ObservableProperty] private decimal _stanje;
    [ObservableProperty] private decimal _saldo;
    [ObservableProperty] private DateTime? _datdok;
    [ObservableProperty] private string _brnal = string.Empty;
    [ObservableProperty] private string _opis = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private decimal _idbr;

    public string NazivKonta { get; set; } = string.Empty;
    public decimal OdstupanjeDug => Dug - UkupnoD;
    public decimal OdstupanjePot => Pot - UkupnoP;
    public bool ImaOdstupanje => UkupnoD != Dug || UkupnoP != Pot;

    public NalmatRow Clone() => new()
    {
        Konto = Konto,
        Cena = Cena,
        Ulaz = Ulaz,
        Izlaz = Izlaz,
        UkupnoD = UkupnoD,
        UkupnoP = UkupnoP,
        Dug = Dug,
        Pot = Pot,
        Stanje = Stanje,
        Saldo = Saldo,
        Datdok = Datdok,
        Brnal = Brnal,
        Opis = Opis,
        Preneto = Preneto,
        Idbr = Idbr,
        NazivKonta = NazivKonta,
    };
}
