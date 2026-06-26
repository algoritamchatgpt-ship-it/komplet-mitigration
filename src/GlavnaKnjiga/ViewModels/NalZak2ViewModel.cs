using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALZAK2 — zaključni list results + print buttons</summary>
public partial class NalZak2ViewModel : ObservableObject
{
    public event Action? ZatvoriFormu;

    private void Stub(string caption) =>
        MessageBox.Show($"{caption} — u pripremi.", "Zaključni list", MessageBoxButton.OK, MessageBoxImage.Information);

    [RelayCommand] private void Klase1()                => Stub("KLASE 1");
    [RelayCommand] private void Grupe2()                => Stub("GRUPE 2");
    [RelayCommand] private void Sintetika3()            => Stub("SINTETIKA 3");
    [RelayCommand] private void Sintetika4()            => Stub("SINTETIKA 4");
    [RelayCommand] private void Analitika6()            => Stub("ANALITIKA 6");
    [RelayCommand] private void Analitika10()           => Stub("ANALITIKA 10");
    [RelayCommand] private void ExcelKlase1()           => Stub("EXPORT U EXCEL KLASE 1");
    [RelayCommand] private void ExcelGrupe2()           => Stub("EXPORT U EXCEL GRUPE 2");
    [RelayCommand] private void ExcelSintetika3()       => Stub("EXPORT U EXCEL SINTETIKA 3");
    [RelayCommand] private void ExcelSintetika4()       => Stub("EXPORT U EXCEL SINTETIKA 4");
    [RelayCommand] private void ExcelAnalitika6()       => Stub("EXPORT U EXCEL ANALITIKA 6");
    [RelayCommand] private void ExcelAnalitika2()       => Stub("EXPORT U EXCEL ANALITIKA 2");
    [RelayCommand] private void TxtAnalitika2()         => Stub("EXPORT U TXT ANALITIKA 2");
    [RelayCommand] private void SveSaNazivimaKonta()    => Stub("SVE SA NAZIVIMA KONTA");
    [RelayCommand] private void SveBezNazivaKonta()     => Stub("SVE BEZ NAZIVA KONTA");
    [RelayCommand] private void AktivniKontniPlan()     => Stub("AKTIVNI KONTNI PLAN");
    [RelayCommand] private void Izlaz()                 => ZatvoriFormu?.Invoke();
}
