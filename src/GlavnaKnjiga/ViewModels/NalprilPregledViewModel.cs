using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;

namespace GlavnaKnjiga.ViewModels;

public partial class NalprilPregledViewModel : ObservableObject
{
    public string Naslov { get; }
    public ObservableCollection<NalprilPregledRow> Redovi { get; }
    public decimal UkupnoDugpre => Redovi.Sum(r => r.Dugpre);
    public decimal UkupnoPotpre => Redovi.Sum(r => r.Potpre);
    public decimal UkupnoDug => Redovi.Sum(r => r.Dug);
    public decimal UkupnoPot => Redovi.Sum(r => r.Pot);
    public decimal UkupniSaldo => Redovi.Sum(r => r.Saldo);

    public event Action? ZatvoriFormu;

    public NalprilPregledViewModel(string naslov, IEnumerable<NalprilPregledRow> redovi)
    {
        Naslov = naslov;
        Redovi = new ObservableCollection<NalprilPregledRow>(redovi);
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();
}
