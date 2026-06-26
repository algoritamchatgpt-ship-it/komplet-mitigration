using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Views;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalizivViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly int _godina;

    public event Action? ZatvoriFormu;

    public NalizivViewModel(string firmPath, int godina)
    {
        _firmPath = firmPath;
        _godina   = godina;
    }

    [RelayCommand]
    private void BilansUspeha()
    {
        var vm  = new NalbuViewModel(_firmPath, _godina);
        new NalbuWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void ZakljucniList()
    {
        new NalzakljWindow().ShowDialog();
    }

    [RelayCommand]
    private void Dnevnik()
    {
        var vm  = new NaldnevViewModel(_firmPath, _godina);
        new NaldnevWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void SveKartice()
    {
        var vm  = new NalkarticeViewModel(_firmPath, _godina);
        new NalkarticeWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void OdredjeneKartice()
    {
        var vm  = new NalgruViewModel(_firmPath);
        new NalgruWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PripremPregledPoKursu()
    {
        var nalPath  = Path.Combine(_firmPath, "nal.dbf");
        var kursPath = Path.Combine(_firmPath, "kurs.dbf");

        if (!File.Exists(nalPath))
        {
            MessageBox.Show("nal.dbf ne postoji.", "PRIPREMI PREGLED PO KURSU",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!File.Exists(kursPath))
        {
            MessageBox.Show("kurs.dbf ne postoji.", "PRIPREMI PREGLED PO KURSU",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                "Obrada će ažurirati polja KURSDUG i KURSPOT u nal.dbf prema kurs.dbf. Nastaviti?",
                "PRIPREMI PREGLED PO KURSU", MessageBoxButton.YesNo, MessageBoxImage.Question) !=
            MessageBoxResult.Yes)
            return;

        try
        {
            // Load kurs table as date→rate dictionary
            var kursDic = new Dictionary<DateTime, decimal>();
            foreach (var rec in new SimpleDbfReader(kursPath).Zapisi())
            {
                var dat  = rec.DajDate("DATDOK");
                var kurs = rec.DajDecimal("KURS");
                if (dat != null && kurs > 0)
                    kursDic[dat.Value] = kurs;
            }

            var schema = DbfTableWriter.LoadSchema(nalPath);
            var rows = new SimpleDbfReader(nalPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .ToList();

            var promenjeno = 0;
            foreach (var row in rows)
            {
                if (row.Datdok == null) continue;
                var mkurs = kursDic.TryGetValue(row.Datdok.Value, out var k) && k > 0 ? k : 1m;
                row.Kursdug = Math.Round(row.Dug / mkurs, 6);
                row.Kurspot = Math.Round(row.Pot / mkurs, 6);
                promenjeno++;
            }

            DbfTableWriter.WriteTable(nalPath, schema, rows, Nalp2ViewModel.NalpRowFieldMapper);
            MessageBox.Show($"Ažurirano redova: {promenjeno}.",
                "PRIPREMI PREGLED PO KURSU", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška:\n{ex.Message}", "PRIPREMI PREGLED PO KURSU",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void SviNalozi()
    {
        MessageBox.Show(
            "Štampa svih naloga nije implementirana — NALSVE.FRX.",
            "SVI NALOZI", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void BilansUspehaPoMtr()
    {
        var vm  = new NalbumtrViewModel(_firmPath, _godina, 1);
        new NalbumtrWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void SaldoPoMtr()
    {
        var vm  = new NalbumtrViewModel(_firmPath, _godina, 2);
        new NalbumtrWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void ZakljucniListBrzi()
    {
        new NalzakljbWindow().ShowDialog();
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();
}
