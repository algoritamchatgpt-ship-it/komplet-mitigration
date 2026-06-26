using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// PUTARK — kartica jednog zapisa putarine.
/// </summary>
public partial class PutarKarticaViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly Dictionary<string, (string relacija, decimal iznos)> _relacijeCache = [];

    [ObservableProperty] private string _redbr = string.Empty;
    [ObservableProperty] private string _putnal = string.Empty;
    [ObservableProperty] private DateTime _datdok = DateTime.Today;
    [ObservableProperty] private string _vreme = string.Empty;
    [ObservableProperty] private string _sifrel = string.Empty;
    [ObservableProperty] private string _relacija = string.Empty;
    [ObservableProperty] private decimal _iznos;
    [ObservableProperty] private string _grupa = string.Empty;
    [ObservableProperty] private decimal _kol = 1m;
    [ObservableProperty] private string _poruka = string.Empty;

    public bool Sačuvano { get; private set; }
    public event Action? ZatvaranjeZahtevano;

    public PutarKarticaViewModel(string folderPath, PutarinaStavka? stavka = null)
    {
        _folderPath = folderPath;
        UcitajRelacijeCache();
        if (stavka != null) UcitajIzStavke(stavka);
    }

    private void UcitajIzStavke(PutarinaStavka s)
    {
        Redbr    = s.Redbr;
        Putnal   = s.Putnal;
        Datdok   = s.Datdok;
        Vreme    = s.Vreme;
        Sifrel   = s.Sifrel;
        Relacija = s.Relacija;
        Iznos    = s.Iznos;
        Grupa    = s.Grupa;
        Kol      = s.Kol;
    }

    public void PrenesiUStavku(PutarinaStavka s)
    {
        s.Redbr    = Redbr;
        s.Putnal   = Putnal;
        s.Datdok   = Datdok;
        s.Vreme    = Vreme;
        s.Sifrel   = Sifrel;
        s.Relacija = Relacija;
        s.Iznos    = Iznos;
        s.Grupa    = Grupa;
        s.Kol      = Kol;
    }

    partial void OnSifrelChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var key = value.Trim().PadLeft(6, ' ');
        if (_relacijeCache.TryGetValue(key.Trim(), out var rel) ||
            _relacijeCache.TryGetValue(value.Trim(), out rel))
        {
            Relacija = rel.relacija;
            Iznos    = rel.iznos;
            Poruka   = $"Relacija: {rel.relacija}";
        }
    }

    [RelayCommand]
    private void IzaberiRelaciju()
    {
        var vm = new PutarRelacijeViewModel(_folderPath);
        var dlg = new Algoritam.WPF.Views.Zarade.PutarRelacijeView { DataContext = vm };
        vm.ZatvaranjeZahtevano += () => dlg.Close();
        dlg.ShowDialog();
        if (!vm.Izabrana || vm.Selektovana == null) return;
        Sifrel   = vm.Selektovana.Sifrel;
        Relacija = vm.Selektovana.Relacija;
        Iznos    = vm.Selektovana.Iznos;
        Poruka   = $"Izabrana relacija: {Relacija}";
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        Sačuvano = true;
        ZatvaranjeZahtevano?.Invoke();
    }

    [RelayCommand]
    private void Izlaz() => ZatvaranjeZahtevano?.Invoke();

    private void UcitajRelacijeCache()
    {
        var path = Path.Combine(_folderPath, "putar0.dbf");
        if (!File.Exists(path)) return;
        try
        {
            foreach (var z in DbfReader.CitajSveZapise(path))
            {
                var sif = Str(z, "SIFREL").Trim();
                if (!string.IsNullOrEmpty(sif))
                    _relacijeCache[sif] = (Str(z, "RELACIJA"), Dec(z, "IZNOS"));
            }
        }
        catch { }
    }

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;
    private static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;
}
