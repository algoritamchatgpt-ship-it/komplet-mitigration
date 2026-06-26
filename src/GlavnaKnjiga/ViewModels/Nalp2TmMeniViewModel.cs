using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace GlavnaKnjiga.ViewModels;

public partial class Nalp2TmMeniViewModel : ObservableObject
{
    private static readonly string[] PodrazumevaneOpcije =
    [
        "UPLACENO GOTOVINA",
        "UPLACENO CEKOVI",
        "UPLACENO KARTICA",
        "UPLACENO VIRMAN",
        "UPLACENO OSTALO",
        "IZLAZ",
    ];

    private readonly Nalp2KartMpViewModel _parent;

    public event Action? ZatvoriFormu;

    public ObservableCollection<string> Opcije { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(IzaberiCommand))]
    private string? _selectedOption;

    public Nalp2TmMeniViewModel(
        Nalp2KartMpViewModel parent, string firmPath)
    {
        _parent = parent;
        Opcije = new ObservableCollection<string>(UcitajOpcije(firmPath));
        SelectedOption = Opcije.FirstOrDefault();
    }

    private bool MozeIzaberi() => !string.IsNullOrWhiteSpace(SelectedOption);

    [RelayCommand(CanExecute = nameof(MozeIzaberi))]
    private void Izaberi()
    {
        if (SelectedOption == null) return;
        _parent.PrimeniTmOpis(SelectedOption);
        ZatvoriFormu?.Invoke();
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal static string? FormirajOpis(
        string opcija, DateTime? datum, out bool prazniOpisKnjizenja)
    {
        prazniOpisKnjizenja = false;
        var izbor = opcija.Trim().ToUpperInvariant();
        var datumText = (datum ?? DateTime.Today).ToString("dd.MM");

        return izbor switch
        {
            "UPLACENO GOTOVINA" => $"UPLACENO GOTOVINA {datumText}",
            "UPLACENO CEKOVI" => $"UPLACENO CEKOVI  {datumText}",
            "UPLACENO KARTICA" => $"UPLACENO KARTICA {datumText}",
            "UPLACENO VIRMAN" => $"UPLACENO VIRMAN {datumText}",
            "UPLACENO OSTALO" => $"UPLACENO OSTALO {datumText}",
            "IZLAZ" => VratiIzlaz(out prazniOpisKnjizenja),
            _ => null,
        };
    }

    private static string? VratiIzlaz(out bool prazniOpisKnjizenja)
    {
        prazniOpisKnjizenja = true;
        return null;
    }

    private static IEnumerable<string> UcitajOpcije(string firmPath)
    {
        var path = Path.Combine(firmPath, "tmmeni.dbf");
        if (!File.Exists(path)) return PodrazumevaneOpcije;

        try
        {
            var opcije = new SimpleDbfReader(path).Zapisi()
                .Select(r => r.DajString("OPIS").Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Skip(1)
                .ToList();
            return opcije.Count > 0 ? opcije : PodrazumevaneOpcije;
        }
        catch
        {
            return PodrazumevaneOpcije;
        }
    }
}
