using CommunityToolkit.Mvvm.ComponentModel;

namespace OsnovnaSredstva.Models;

// Legacy: xml_ost.scx (XMLOST) grd0 — kolone Deo1/Aop/Kol/Podatak/Deo2/Sve/Preneto/Idbr,
// fizička tabela xmlost.dbf (alias "xmlbu" u legacy formi).
public class OsXmlOstStavka : ObservableObject
{
    private string _deo1 = "";
    private string _aop = "";
    private string _kol = "";
    private string _podatak = "";
    private string _deo2 = "";
    private string _sve = "";
    private string _preneto = "";
    private int _idbr;

    public string Deo1    { get => _deo1;    set => SetProperty(ref _deo1,    value); }
    public string Aop     { get => _aop;     set => SetProperty(ref _aop,     value); }
    public string Kol     { get => _kol;     set => SetProperty(ref _kol,     value); }
    public string Podatak { get => _podatak; set => SetProperty(ref _podatak, value); }
    public string Deo2    { get => _deo2;    set => SetProperty(ref _deo2,    value); }
    public string Sve     { get => _sve;     set => SetProperty(ref _sve,     value); }
    public string Preneto { get => _preneto; set => SetProperty(ref _preneto, value); }
    public int    Idbr    { get => _idbr;    set => SetProperty(ref _idbr,    value); }
}
