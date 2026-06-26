using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalpepViewModel : ObservableObject
{
    private readonly string _firmPath;

    public event Action? ZatvoriFormu;

    [ObservableProperty] private ObservableCollection<NalpepRow> _redovi = new();
    [ObservableProperty] private NalpepRow? _selektovaniRed;

    [ObservableProperty] private string _lblPartner = string.Empty;
    [ObservableProperty] private string _lblNalog = string.Empty;
    [ObservableProperty] private string _lblKonto = string.Empty;
    [ObservableProperty] private string _lblGrad = string.Empty;
    [ObservableProperty] private string _lblMtr = string.Empty;
    [ObservableProperty] private string _lblRec = string.Empty;
    [ObservableProperty] private string _lblDug = "DUGUJE    ";
    [ObservableProperty] private string _lblPot = "POTRAŽUJE ";
    [ObservableProperty] private string _lblSaldo = "SALDO     ";
    [ObservableProperty] private string _lblDugIznos = string.Empty;
    [ObservableProperty] private string _lblPotIznos = string.Empty;
    [ObservableProperty] private string _lblSaldoIznos = string.Empty;

    private List<DbfRecord> _kontoRecords = new();
    private List<DbfRecord> _mestaRecords = new();
    private List<DbfRecord> _mtrRecords = new();
    private List<DbfRecord> _an0Records = new();

    public NalpepViewModel(string firmPath)
    {
        _firmPath = firmPath;
        Ucitaj();
    }

    private void Ucitaj()
    {
        var nalpepPath = Path.Combine(_firmPath, "nalpep.dbf");
        var kontoPath = Path.Combine(_firmPath, "konto.dbf");
        var mestaPath = Path.Combine(_firmPath, "mesta.dbf");
        var mtrPath = Path.Combine(_firmPath, "mtr.dbf");
        var an0Path = Path.Combine(_firmPath, "an0.dbf");

        if (File.Exists(kontoPath))
            _kontoRecords = new SimpleDbfReader(kontoPath).Zapisi().ToList();
        if (File.Exists(mestaPath))
            _mestaRecords = new SimpleDbfReader(mestaPath).Zapisi().ToList();
        if (File.Exists(mtrPath))
            _mtrRecords = new SimpleDbfReader(mtrPath).Zapisi().ToList();
        if (File.Exists(an0Path))
            _an0Records = new SimpleDbfReader(an0Path).Zapisi().ToList();

        if (!File.Exists(nalpepPath)) return;
        var rows = new SimpleDbfReader(nalpepPath).Zapisi()
            .Select(NalpepRowFromRecord)
            .ToList();
        Redovi = new ObservableCollection<NalpepRow>(rows);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
        AzurirajLblRec();
        AzurirajInfo();
    }

    partial void OnSelektovaniRedChanged(NalpepRow? value)
    {
        AzurirajLblRec();
        AzurirajInfo();
    }

    private void AzurirajLblRec()
    {
        LblRec = SelektovaniRed != null
            ? $"{Redovi.IndexOf(SelektovaniRed) + 1}/{Redovi.Count}"
            : $"0/{Redovi.Count}";
    }

    private void AzurirajInfo()
    {
        if (SelektovaniRed == null)
        {
            LblKonto = "";
            LblNalog = "";
            LblGrad = "";
            LblMtr = "";
            LblPartner = "";
            return;
        }

        var row = SelektovaniRed;

        var kontoRec = _kontoRecords.FirstOrDefault(r => r.DajString("KONTO").Trim() == row.Konto.Trim());
        LblKonto = kontoRec != null ? $"{kontoRec.DajString("KONTO")} {kontoRec.DajString("NAZIV")}" : row.Konto;

        LblNalog = $"nalog za knjiženje {row.Brnal}   datum {row.Datdok:dd.MM.yyyy}";

        var mestaRec = _mestaRecords.FirstOrDefault(r => r.DajString("MESTO").Trim() == row.Mp.Trim());
        LblGrad = mestaRec != null ? $"mesto {row.Mp} {mestaRec.DajString("MESTO")}" : $"mesto {row.Mp}";

        var mtrRec = _mtrRecords.FirstOrDefault(r => r.DajDecimal("MTR") == row.Mtr);
        LblMtr = mtrRec != null ? $"mesto troškova {row.Mtr} {mtrRec.DajString("NAZIV")}" : $"mesto troškova {row.Mtr}";

        var an0Rec = _an0Records.FirstOrDefault(r => r.DajString("SIFRA").Trim() == row.Sifra.Trim());
        LblPartner = an0Rec != null ? $"partner {row.Sifra} {an0Rec.DajString("NAZIV")}" : $"partner {row.Sifra}";

        IzracunajTotale();
    }

    private void IzracunajTotale()
    {
        if (SelektovaniRed == null) return;

        var mBrnal = SelektovaniRed.Brnal.Trim();
        var filtered = Redovi.Where(r => r.Brnal.Trim() == mBrnal).ToList();

        var mDug = filtered.Sum(r => r.Dug);
        var mPot = filtered.Sum(r => r.Pot);

        LblDugIznos = mDug.ToString("N2");
        LblPotIznos = mPot.ToString("N2");

        var saldo = mDug - mPot;
        LblSaldoIznos = saldo.ToString("N2");
    }

    [RelayCommand]
    private void Dodaj()
    {
        var novi = new NalpepRow
        {
            Konto = "9999999999",
            Mp = "   ",
        };
        Redovi.Add(novi);
        SelektovaniRed = novi;
    }

    [RelayCommand]
    private void BrisiPraznine()
    {
        var prazni = Redovi.Where(r =>
            string.IsNullOrWhiteSpace(r.Konto) && r.Dug == 0 && r.Pot == 0).ToList();
        foreach (var p in prazni) Redovi.Remove(p);
        if (SelektovaniRed != null && !Redovi.Contains(SelektovaniRed))
            SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
        AzurirajLblRec();
    }

    [RelayCommand]
    private void BrisiNalog()
    {
        if (SelektovaniRed == null) return;
        var mBrnal = SelektovaniRed.Brnal.Trim();

        if (MessageBox.Show($"Brisanje celog naloga {mBrnal}?",
                "BRISANJE NALOGA", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var toRemove = Redovi.Where(r => r.Brnal.Trim() == mBrnal).ToList();
        foreach (var r in toRemove) Redovi.Remove(r);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
        AzurirajLblRec();
        AzurirajInfo();
    }

    [RelayCommand]
    private void IdiNaVrh() { if (Redovi.Count > 0) SelektovaniRed = Redovi[0]; }

    [RelayCommand]
    private void IdiNaDno() { if (Redovi.Count > 0) SelektovaniRed = Redovi[^1]; }

    [RelayCommand]
    private void IdiGore()
    {
        if (SelektovaniRed == null) return;
        var idx = Redovi.IndexOf(SelektovaniRed);
        if (idx > 0) SelektovaniRed = Redovi[idx - 1];
    }

    [RelayCommand]
    private void IdiDole()
    {
        if (SelektovaniRed == null) return;
        var idx = Redovi.IndexOf(SelektovaniRed);
        if (idx < Redovi.Count - 1) SelektovaniRed = Redovi[idx + 1];
    }

    [RelayCommand]
    private void KontniPlan()
    {
        var vm = new KontoPlanViewModel(_firmPath, "konto.dbf", "KONTNI PLAN");
        new Views.KontoPlanWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PartneriF4()
    {
        var vm = new NalanparViewModel(_firmPath);
        new Views.NalanparWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PreuzimanjeFx()
    {
        var vm = new Nalpep00ViewModel(_firmPath);
        new Views.Nalpep00Window(vm).ShowDialog();
        Osvezi();
    }

    [RelayCommand]
    private void PreuzimanjeRf()
    {
        var vm = new Nalpep00ViewModel(_firmPath);
        vm.Naslov = "PUTANJA ZA PREUZIMANJE IZVODA RF";
        new Views.Nalpep00Window(vm).ShowDialog();
        Osvezi();
    }

    [RelayCommand]
    private void PreuzimanjeXml()
    {
        var vm = new Nalpep00ViewModel(_firmPath);
        vm.Naslov = "PUTANJA ZA PREUZIMANJE IZVODA XML";
        new Views.Nalpep00Window(vm).ShowDialog();
        Osvezi();
    }

    [RelayCommand]
    private void PreuzimanjeXml2()
    {
        var vm = new Nalpep00ViewModel(_firmPath);
        vm.Naslov = "PUTANJA ZA PREUZIMANJE IZVODA XML2";
        new Views.Nalpep00Window(vm).ShowDialog();
        Osvezi();
    }

    [RelayCommand]
    private void OdredjivanjeNaloga()
    {
        if (SelektovaniRed == null) return;

        var maxBrnal = 0m;
        foreach (var r in Redovi)
        {
            if (decimal.TryParse(r.Brnal.Trim(), out var b) && b > maxBrnal)
                maxBrnal = b;
        }

        var newBrnal = (maxBrnal + 1).ToString().PadRight(6);
        var mBrnal = newBrnal.Trim();

        foreach (var r in Redovi)
        {
            if (string.IsNullOrWhiteSpace(r.Brnal.Trim()))
                r.Brnal = mBrnal.PadRight(6);
        }
        AzurirajInfo();
    }

    [RelayCommand]
    private void SifraLostFocus()
    {
        if (SelektovaniRed == null) return;
        var mSifra = SelektovaniRed.Sifra.Trim();
        if (string.IsNullOrEmpty(mSifra)) return;

        var an0Rec = _an0Records.FirstOrDefault(r => r.DajString("SIFRA").Trim() == mSifra);
        if (an0Rec != null)
            SelektovaniRed.Naziv = an0Rec.DajString("NAZIV").TrimEnd();
    }

    [RelayCommand]
    private void Izlaz()
    {
        Snimi();
        ZatvoriFormu?.Invoke();
    }

    private void Snimi()
    {
        var nalpepPath = Path.Combine(_firmPath, "nalpep.dbf");
        if (!File.Exists(nalpepPath)) return;
        var schema = DbfTableWriter.LoadSchema(nalpepPath);
        DbfTableWriter.WriteTable(nalpepPath, schema, Redovi.ToList(), NalpepFieldMapper);
    }

    private void Osvezi()
    {
        var nalpepPath = Path.Combine(_firmPath, "nalpep.dbf");
        if (!File.Exists(nalpepPath)) return;
        var rows = new SimpleDbfReader(nalpepPath).Zapisi()
            .Select(NalpepRowFromRecord)
            .ToList();
        Redovi = new ObservableCollection<NalpepRow>(rows);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
        AzurirajLblRec();
        AzurirajInfo();
    }

    private static NalpepRow NalpepRowFromRecord(DbfRecord rec) => new()
    {
        Idbr = rec.DajDecimal("IDBR"),
        Konto = rec.DajString("KONTO"),
        Dug = rec.DajDecimal("DUG"),
        Pot = rec.DajDecimal("POT"),
        Opis = rec.DajString("OPIS"),
        Sifra = rec.DajString("SIFRA"),
        Naziv = rec.DajString("NAZIV"),
        Brrac = rec.DajString("BRRAC"),
        Datdok = rec.DajDate("DATDOK"),
        Valuta = rec.DajDate("VALUTA"),
        Brnal = rec.DajString("BRNAL"),
        Pozivz = rec.DajString("POZIVZ"),
        Pozivp = rec.DajString("POZIVP"),
        Mp = rec.DajString("MP"),
        Mtr = rec.DajDecimal("MTR"),
        Dok = rec.DajString("DOK"),
        Preneto = rec.DajString("PRENETO"),
    };

    private static object? NalpepFieldMapper(NalpepRow row, string field) => field switch
    {
        "IDBR" => row.Idbr,
        "KONTO" => row.Konto,
        "DUG" => row.Dug,
        "POT" => row.Pot,
        "OPIS" => row.Opis,
        "SIFRA" => row.Sifra,
        "NAZIV" => row.Naziv,
        "BRRAC" => row.Brrac,
        "DATDOK" => row.Datdok,
        "VALUTA" => row.Valuta,
        "BRNAL" => row.Brnal,
        "POZIVZ" => row.Pozivz,
        "POZIVP" => row.Pozivp,
        "MP" => row.Mp,
        "MTR" => row.Mtr,
        "DOK" => row.Dok,
        "PRENETO" => row.Preneto,
        _ => null,
    };
}
