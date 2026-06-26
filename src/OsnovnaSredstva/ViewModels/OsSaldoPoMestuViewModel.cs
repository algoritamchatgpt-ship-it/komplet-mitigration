using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Views;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

/// <summary>OSPOPIS31 — saldo osnovnih sredstava grupisan po mestu.</summary>
public partial class OsSaldoPoMestuViewModel : ObservableObject
{
    private readonly IReadOnlyList<OsKartica> _kartice;

    public event Action? ZatvaranjeZahtevano;

    [ObservableProperty] private string _konto = string.Empty;
    [ObservableProperty] private string _poruka =
        "Unesite konto ili ostavite prazno za sva konta.";

    public OsSaldoPoMestuViewModel(IEnumerable<OsKartica> kartice)
    {
        _kartice = kartice.ToList();
    }

    [RelayCommand]
    private void Saldo()
    {
        var konto = Konto.Trim();
        var filtrirane = string.IsNullOrWhiteSpace(konto)
            ? _kartice
            : _kartice
                .Where(k => string.Equals(
                    k.Konto?.Trim(),
                    konto,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (filtrirane.Count == 0)
        {
            Poruka = $"Za konto '{konto}' nema kartica.";
            return;
        }

        var vm = OsSaldoViewModel.PoMestu(
            _kartice,
            string.IsNullOrWhiteSpace(konto) ? null : konto);
        var win = new OsSaldoWindow(vm)
        {
            Owner = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive),
        };
        win.ShowDialog();

        Poruka = string.IsNullOrWhiteSpace(konto)
            ? $"Prikazan saldo za sva konta: {filtrirane.Count} kartica."
            : $"Prikazan saldo za konto {konto}: {filtrirane.Count} kartica.";
    }

    [RelayCommand]
    private void Izlaz() => ZatvaranjeZahtevano?.Invoke();
}
