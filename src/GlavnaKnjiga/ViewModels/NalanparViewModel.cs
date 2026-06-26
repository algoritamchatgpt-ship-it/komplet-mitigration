using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>
/// Transcripcija NALANPAR.SCX — PARAMETRI KNJIŽENJA ANALITIKE.
/// Čita i snima anpar.dbf (jedna row). Init: USE &KANPAR ALIAS ANPAR.
/// Unload: saves PKPLANS=KPLANS, PKONDOPU=KONDOPU, PKTKR=KTKR (globali → mi ih samo čitamo/snimamo).
/// </summary>
public partial class NalanparViewModel : ObservableObject
{
    private readonly string? _dbfPath;

    // Sva polja čitljiva/upisiva, ControlSource bound 1:1
    [ObservableProperty] private string _knjan    = string.Empty; // C1 KNJIŽENJE IZVODA=D RAČUNA=N
    [ObservableProperty] private string _vrnal    = string.Empty; // C3 UOBIČAJENA VRSTA NALOGA
    [ObservableProperty] private string _konto    = string.Empty; // C10 PRVI KONTO KOD IZVODA
    [ObservableProperty] private string _kplans   = string.Empty; // C1 SILAZAK U KONTNI PLAN
    [ObservableProperty] private string _kondopu  = string.Empty; // C1 DOPUNI KONTO DO 10 CIFARA
    [ObservableProperty] private string _ktkr     = string.Empty; // C1 KNJIŽI RAZDUŽENJE KOD T.K.
    [ObservableProperty] private string _kanautoz = string.Empty; // C1 AUTOMATSKI ZATVARAJ ANALITIKU
    [ObservableProperty] private string _knacin   = string.Empty; // C1 Promeni način knjiženja

    public event Action? ZatvoriFormu;

    public NalanparViewModel(string firmPath)
    {
        _dbfPath = Path.Combine(firmPath, "anpar.dbf");
        if (!File.Exists(_dbfPath)) { _dbfPath = null; return; }
        try
        {
            var r = new SimpleDbfReader(_dbfPath);
            foreach (var rec in r.Zapisi())
            {
                Knjan    = rec.DajString("KNJAN").Trim();
                Vrnal    = rec.DajString("VRNAL").Trim();
                Konto    = rec.DajString("KONTO").Trim();
                Kplans   = rec.DajString("KPLANS").Trim();
                Kondopu  = rec.DajString("KONDOPU").Trim();
                Ktkr     = rec.DajString("KTKR").Trim();
                Kanautoz = rec.DajString("KANAUTOZ").Trim();
                Knacin   = rec.DajString("KNACIN").Trim();
                break;
            }
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri čitanju anpar.dbf: {ex.Message}"); }
    }

    // txtVrnal.LostFocus: REPLACE VRNAL WITH STR(VAL(VRNAL),3,0)
    public void OnVrnalLostFocus()
    {
        if (int.TryParse(Vrnal, out var n))
            Vrnal = n.ToString().PadLeft(3);
    }

    [RelayCommand]
    private void Izlaz()
    {
        Snimi();
        ZatvoriFormu?.Invoke();
    }

    // Snimanje — REPLACE svakog polja nazad u anpar.dbf
    private void Snimi()
    {
        if (_dbfPath == null) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            // anpar je single-row fajl; čuvamo ga kao listu od 1 elementa
            var rows = new[] { this };
            DbfTableWriter.WriteTable(_dbfPath, schema, rows, (vm, field) => field switch
            {
                "KNJAN"    => (object)vm.Knjan,
                "VRNAL"    => vm.Vrnal,
                "KONTO"    => vm.Konto,
                "KPLANS"   => vm.Kplans,
                "KONDOPU"  => vm.Kondopu,
                "KTKR"     => vm.Ktkr,
                "KANAUTOZ" => vm.Kanautoz,
                "KNACIN"   => vm.Knacin,
                _          => null
            });
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri snimanju anpar.dbf: {ex.Message}"); }
    }
}
