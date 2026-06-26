using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;

namespace GlavnaKnjiga.ViewModels;

public partial class NalogPregledViewModel : ObservableObject
{
    public event Action? ZatvoriFormu;

    public string Naslov { get; }
    public ObservableCollection<NalogPregledRow> Redovi { get; }
    public decimal UkupnoDug => Redovi.Sum(r => r.Dug);
    public decimal UkupnoPot => Redovi.Sum(r => r.Pot);
    public decimal Saldo => UkupnoDug - UkupnoPot;
    public decimal UkupnoDevDug => Redovi.Sum(r => r.Devdug);
    public decimal UkupnoDevPot => Redovi.Sum(r => r.Devpot);
    public string Status =>
        $"Redova: {Redovi.Count}   Duguje: {UkupnoDug:N2}   Potražuje: {UkupnoPot:N2}   Saldo: {Saldo:N2}";

    [ObservableProperty] private NalogPregledRow? _selectedRow;

    public NalogPregledViewModel(string naslov, IEnumerable<NalogPregledRow> rows)
    {
        Naslov = naslov;
        Redovi = new ObservableCollection<NalogPregledRow>(rows);
        SelectedRow = Redovi.FirstOrDefault();
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal static NalogPregledRow IzNalp(NalpRow r) => new()
    {
        Konto = r.Konto,
        Dug = r.Dug,
        Pot = r.Pot,
        Opis = r.Opis,
        Datdok = r.Datdok,
        Brnal = r.Brnal,
        Dok = r.Dok,
        Mp = r.Mp,
        Mtr = r.Mtr,
        Dev = r.Dev,
        Devkurs = r.Devkurs,
        Devdug = r.Devdug,
        Devpot = r.Devpot,
        Sifra = r.Sifra,
        Brrac = r.Brrac,
        Ulaz = r.Ulaz,
        Izlaz = r.Izlaz,
        Saldo = r.Dpsaldo,
    };

    internal static NalogPregledRow IzNalgk10(Nalgk10Row r) => new()
    {
        Konto = r.Konto,
        Dug = r.Dug,
        Pot = r.Pot,
        Opis = r.Opis,
        Datdok = r.Datdok,
        Brnal = r.Brnal,
    };
}
