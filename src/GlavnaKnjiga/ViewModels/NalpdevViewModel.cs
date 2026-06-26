using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalpdevViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly string _nalPath;
    private List<DbfRecord> _devRecords = new();

    [ObservableProperty] private string _konto = string.Empty;
    [ObservableProperty] private decimal _dug;
    [ObservableProperty] private decimal _pot;
    [ObservableProperty] private string _opis = string.Empty;
    [ObservableProperty] private DateTime? _datdok;
    [ObservableProperty] private string _brnal = string.Empty;
    [ObservableProperty] private string _mp = string.Empty;
    [ObservableProperty] private decimal _mtr;
    [ObservableProperty] private string _dok = string.Empty;

    [ObservableProperty] private string _dev = string.Empty;
    [ObservableProperty] private decimal _devkurs;
    [ObservableProperty] private decimal _devdug;
    [ObservableProperty] private decimal _devpot;

    private decimal _idbr;

    public event Action<bool>? ZatvoriFormu;

    public NalpdevViewModel(string firmPath, string nalPath, string konto, decimal dug, decimal pot,
        string opis, DateTime? datdok, string brnal, string mp, decimal mtr, string dok,
        string dev, decimal devkurs, decimal devdug, decimal devpot, decimal idbr)
    {
        _firmPath = firmPath;
        _nalPath = nalPath;

        Konto = konto;
        Dug = dug;
        Pot = pot;
        Opis = opis;
        Datdok = datdok;
        Brnal = brnal;
        Mp = mp;
        Mtr = mtr;
        Dok = dok;
        Dev = dev;
        Devkurs = devkurs;
        Devdug = devdug;
        Devpot = devpot;
        _idbr = idbr;

        var devPath = Path.Combine(_firmPath, "dev.dbf");
        if (File.Exists(devPath))
            _devRecords = new SimpleDbfReader(devPath).Zapisi().ToList();
    }

    partial void OnDevChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Datdok.HasValue) return;

        var devPath = Path.Combine(_firmPath, "dev.dbf");
        if (!File.Exists(devPath)) return;

        var mDev = value.Trim();
        var mDatStr = Datdok.Value.ToString("yyyyMMdd");

        var rec = _devRecords.FirstOrDefault(r =>
        {
            var dev = r.DajString("DEV").Trim();
            var dat = r.DajDate("DATDOK");
            return dev == mDev && dat.HasValue && dat.Value.Date == Datdok.Value.Date;
        });

        if (rec != null)
        {
            Devkurs = rec.DajDecimal("KURS");
        }
    }

    partial void OnDevdugChanged(decimal value)
    {
        if (value != 0 && Devkurs != 0)
        {
            Dug = value * Devkurs;
            Devpot = 0;
            Pot = 0;
        }
    }

    partial void OnDevpotChanged(decimal value)
    {
        if (value != 0 && Devkurs != 0)
        {
            Pot = value * Devkurs;
            Devdug = 0;
            Dug = 0;
        }
    }

    [RelayCommand]
    private void Snimi()
    {
        try
        {
            var schema = DbfTableWriter.LoadSchema(_nalPath);
            var rows = new SimpleDbfReader(_nalPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .ToList();

            var target = rows.FirstOrDefault(r => r.Idbr == _idbr);
            if (target == null)
            {
                MessageBox.Show("Stavka nije pronađena u bazi.", "NALPDEV",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            target.Dev = Dev;
            target.Devkurs = Devkurs;
            target.Devdug = Devdug;
            target.Devpot = Devpot;
            target.Dug = Dug;
            target.Pot = Pot;

            DbfTableWriter.WriteTable(_nalPath, schema, rows, Nalp2ViewModel.NalpRowFieldMapper);
            ZatvoriFormu?.Invoke(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri snimanju:\n{ex.Message}", "NALPDEV",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke(false);
}
