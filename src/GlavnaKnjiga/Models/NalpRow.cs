using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalpRow : ObservableObject
{
    // Column1
    [ObservableProperty] private string _konto = string.Empty;
    // Column2
    [ObservableProperty] private decimal _dug;
    // Column3
    [ObservableProperty] private decimal _pot;
    // Column4
    [ObservableProperty] private string _opis = string.Empty;
    // Column5
    [ObservableProperty] private DateTime? _datdok;
    // Column6
    [ObservableProperty] private string _brnal = string.Empty;
    // Column7
    [ObservableProperty] private string _dok = string.Empty;
    // Column8
    [ObservableProperty] private decimal _mp;
    // Column9
    [ObservableProperty] private decimal _mtr;
    // Column10
    [ObservableProperty] private string _dev = string.Empty;
    // Column11
    [ObservableProperty] private decimal _devkurs;
    // Column12
    [ObservableProperty] private decimal _devdug;
    // Column13
    [ObservableProperty] private decimal _devpot;
    // Column14
    [ObservableProperty] private string _brdok = string.Empty;
    // Column15
    [ObservableProperty] private string _napomena1 = string.Empty;
    // Column16
    [ObservableProperty] private string _napomena2 = string.Empty;
    // Column17
    [ObservableProperty] private decimal _kurs;
    // Column18
    [ObservableProperty] private decimal _kursdug;
    // Column19
    [ObservableProperty] private decimal _kurspot;
    // Column20
    [ObservableProperty] private decimal _dpsaldo;
    // Column21
    [ObservableProperty] private decimal _cena;
    // Column22
    [ObservableProperty] private decimal _ulaz;
    // Column23
    [ObservableProperty] private decimal _izlaz;
    // Column24
    [ObservableProperty] private decimal _ukupnoD;
    // Column25
    [ObservableProperty] private decimal _ukupnoP;
    // Column26
    [ObservableProperty] private string _sifra = string.Empty;
    // Column27
    [ObservableProperty] private string _brrac = string.Empty;
    // Column28
    [ObservableProperty] private string _valuta = string.Empty;
    // Column29
    [ObservableProperty] private DateTime? _datpri;
    // Column30
    [ObservableProperty] private DateTime? _datpdv;
    // Column31
    [ObservableProperty] private decimal _stanje;
    // Column32
    [ObservableProperty] private decimal _saldo;
    // Column33
    [ObservableProperty] private string _oznaka = string.Empty;
    // Column34
    [ObservableProperty] private DateTime? _datum;
    // Column35
    [ObservableProperty] private string _vreme = string.Empty;
    // Column36
    [ObservableProperty] private decimal _skonto;
    // Column37
    [ObservableProperty] private string _automnal = string.Empty;
    // Column38
    [ObservableProperty] private string _oper = string.Empty;
    // Column39
    [ObservableProperty] private string _probni = string.Empty;
    // Column40
    [ObservableProperty] private string _gkonto = string.Empty;
    // Column41
    [ObservableProperty] private string _arhiva = string.Empty;
    // Column42
    [ObservableProperty] private string _arhiva2 = string.Empty;
    // Column43
    [ObservableProperty] private string _devizno = string.Empty;
    // Column44
    [ObservableProperty] private string _vrsta = string.Empty;
    // Column45
    [ObservableProperty] private string _imetabele = string.Empty;
    // Column46
    [ObservableProperty] private DateTime? _datrazduz;
    // Column47
    [ObservableProperty] private string _opisu = string.Empty;
    // Column48
    [ObservableProperty] private decimal _dinrazduz;
    // Column49
    [ObservableProperty] private decimal _sifprod;
    // Column50
    [ObservableProperty] private string _dp = string.Empty;
    // Column51
    [ObservableProperty] private decimal _doddug;
    // Column52
    [ObservableProperty] private decimal _dodpot;
    // Column53
    [ObservableProperty] private string _preneto = string.Empty;
    // Column54
    [ObservableProperty] private decimal _numred;
    // Column55
    [ObservableProperty] private decimal _idbr;

    public NalpRow Clone() => new()
    {
        Konto     = Konto,     Dug      = Dug,      Pot     = Pot,
        Opis      = Opis,      Datdok   = Datdok,   Brnal   = Brnal,
        Dok       = Dok,       Mp       = Mp,        Mtr     = Mtr,
        Dev       = Dev,       Devkurs  = Devkurs,   Devdug  = Devdug,
        Devpot    = Devpot,    Brdok    = Brdok,     Napomena1 = Napomena1,
        Napomena2 = Napomena2, Kurs     = Kurs,      Kursdug = Kursdug,
        Kurspot   = Kurspot,   Dpsaldo  = Dpsaldo,   Cena    = Cena,
        Ulaz      = Ulaz,      Izlaz    = Izlaz,     UkupnoD = UkupnoD,
        UkupnoP   = UkupnoP,   Sifra    = Sifra,     Brrac   = Brrac,
        Valuta    = Valuta,    Datpri   = Datpri,    Datpdv  = Datpdv,
        Stanje    = Stanje,    Saldo    = Saldo,     Oznaka  = Oznaka,
        Datum     = Datum,     Vreme    = Vreme,     Skonto  = Skonto,
        Automnal  = Automnal,  Oper     = Oper,      Probni  = Probni,
        Gkonto    = Gkonto,    Arhiva   = Arhiva,    Arhiva2 = Arhiva2,
        Devizno   = Devizno,   Vrsta    = Vrsta,     Imetabele = Imetabele,
        Datrazduz = Datrazduz, Opisu    = Opisu,     Dinrazduz = Dinrazduz,
        Sifprod   = Sifprod,   Dp       = Dp,        Doddug  = Doddug,
        Dodpot    = Dodpot,    Preneto  = Preneto,   Numred  = Numred,
        Idbr      = Idbr,
    };
}
