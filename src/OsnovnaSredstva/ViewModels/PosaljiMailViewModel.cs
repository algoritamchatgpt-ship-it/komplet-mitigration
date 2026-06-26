using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Services;

namespace OsnovnaSredstva.ViewModels;

public partial class PosaljiMailViewModel : ObservableObject
{
    private readonly EmailService _emailService;
    private readonly EmailPodesavanjaService _podesavanjaService;
    private readonly byte[] _pdfBytes;
    private readonly string _pdfNaziv;

    [ObservableProperty] private string _primaoci = string.Empty;
    [ObservableProperty] private string _predmet = string.Empty;
    [ObservableProperty] private string _tekst = string.Empty;
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private bool _radi;

    public Action? ZatvoriAction { get; set; }
    public Action? OtvoriPodesavanjaAction { get; set; }

    public string PdfNazivPrikaz => $"{_pdfNaziv}  ({_pdfBytes.Length / 1024} KB)";

    public PosaljiMailViewModel(
        EmailService emailService,
        EmailPodesavanjaService podesavanjaService,
        byte[] pdfBytes,
        string pdfNaziv,
        string defaultPredmet = "")
    {
        _emailService = emailService;
        _podesavanjaService = podesavanjaService;
        _pdfBytes = pdfBytes;
        _pdfNaziv = pdfNaziv;
        Predmet = defaultPredmet;
        OsveziStatusPoruku();
    }

    public void OsveziStatusPoruku()
    {
        Poruka = _podesavanjaService.JeKonfigurisano()
            ? "Unesite e-mail adresu primaoca i kliknite Posalji."
            : "SMTP nije podesen. Kliknite 'Podesavanja SMTP' pre slanja.";
    }

    [RelayCommand(CanExecute = nameof(MozePoslati))]
    private async Task PosaljiAsync()
    {
        if (string.IsNullOrWhiteSpace(Primaoci))
        {
            Poruka = "Unesite e-mail adresu primaoca.";
            return;
        }

        Radi = true;
        Poruka = "Slanje u toku...";
        try
        {
            var adrese = Primaoci.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);
            await _emailService.PosaljiAsync(adrese, Predmet, Tekst, _pdfBytes, _pdfNaziv);
            Poruka = $"E-mail uspesno poslat na: {Primaoci.Trim()}";
            await Task.Delay(1500);
            ZatvoriAction?.Invoke();
        }
        catch (Exception ex)
        {
            Poruka = $"Greska: {ex.Message}";
        }
        finally { Radi = false; }
    }

    private bool MozePoslati() => !Radi;

    partial void OnRadiChanged(bool value) => PosaljiCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void OtvoriPodesavanja() => OtvoriPodesavanjaAction?.Invoke();
}
