using Algoritam.Application;
using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Dbf;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// REGISTAR — pregled prenesenih zarada za budžetske korisnike.
/// Čita aktivni ld.dbf i prikazuje podatke u formatu za Centralni registar.
/// </summary>
public partial class LdRegistarViewModel : ObservableObject
{
    private readonly AppState _appState;

    [ObservableProperty] private ObservableCollection<LdRegistarRed> _redovi = [];
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private string _naslovPerioda = string.Empty;
    [ObservableProperty] private string _filterTekst = string.Empty;

    private List<LdRegistarRed> _sviRedovi = [];

    public event Action? ZatvaranjeZahtevano;

    public LdRegistarViewModel(AppState appState)
    {
        _appState = appState;
        _ = UcitajAsync();
    }

    partial void OnFilterTekstChanged(string value) => PrimeniFilter();

    [RelayCommand]
    private async Task UcitajAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        try
        {
            var stavke = await Task.Run(() =>
                LdObracunDbfReader.CitajSve(folder));

            if (stavke.Count == 0)
            {
                Poruka = "Nema podataka u ld.dbf.";
                return;
            }

            var prvaStavka = stavke.FirstOrDefault(s => s.Mesec > 0);
            NaslovPerioda = prvaStavka != null
                ? $"Mesec: {prvaStavka.Mesec:00}  Godina: {prvaStavka.Godina}"
                : string.Empty;

            _sviRedovi = stavke
                .OrderBy(s => s.Broj)
                .Select((s, i) => new LdRegistarRed
                {
                    Rb        = i + 1,
                    Broj      = s.Broj,
                    ImePrez   = s.ImePrez.Trim(),
                    Jmbg      = s.Maticnibr.Trim(),
                    Bruto     = s.Bruto,
                    Neto      = s.Neto,
                    DopRadnik = s.Dopsocr,
                    DopFirma  = s.Dopsocf,
                    Porez     = s.Porez,
                    ZaIsplatu = s.Zaisplatu,
                    Mesec     = s.Mesec,
                    Godina    = s.Godina.Trim(),
                })
                .ToList();

            PrimeniFilter();
            Poruka = $"Učitano {_sviRedovi.Count} radnika.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čitanju: {ex.Message}";
        }
    }

    private void PrimeniFilter()
    {
        var f = FilterTekst.Trim();
        if (string.IsNullOrWhiteSpace(f))
        {
            Redovi = new ObservableCollection<LdRegistarRed>(_sviRedovi);
        }
        else
        {
            var fi = f.ToUpperInvariant();
            Redovi = new ObservableCollection<LdRegistarRed>(
                _sviRedovi.Where(r =>
                    r.ImePrez.ToUpperInvariant().Contains(fi) ||
                    r.Jmbg.Contains(fi)));
        }
    }

    [RelayCommand]
    private void IzvozExcel()
    {
        if (Redovi.Count == 0)
        {
            System.Windows.MessageBox.Show("Nema podataka za izvoz.", "Izvoz Excel",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title     = "Sačuvaj registar zarada",
            Filter    = "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
            FileName  = $"Registar_{NaslovPerioda.Replace("  ", "_").Replace(" ", "").Replace(":", "")}",
            DefaultExt = ".xlsx"
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            if (dlg.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                IzvozCsv(dlg.FileName);
            else
                IzvozXlsx(dlg.FileName);

            Poruka = $"Izvoz završen: {Path.GetFileName(dlg.FileName)}";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Greška pri izvozu:\n{ex.Message}", "Izvoz",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void IzvozXlsx(string putanja)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Registar zarada");

        var zaglavlja = new[] { "R.B.", "BR", "IME I PREZIME", "JMBG / MAT.BR.",
            "BRUTO", "POREZ", "DOP.RADNIK", "DOP.FIRMA", "NETO", "ZA ISPLATU" };

        for (var i = 0; i < zaglavlja.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = zaglavlja[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B5E20");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = 2;
        foreach (var r in Redovi)
        {
            ws.Cell(row, 1).Value = r.Rb;
            ws.Cell(row, 2).Value = r.Broj;
            ws.Cell(row, 3).Value = r.ImePrez;
            ws.Cell(row, 4).Value = r.Jmbg;
            ws.Cell(row, 5).Value = r.Bruto;
            ws.Cell(row, 6).Value = r.Porez;
            ws.Cell(row, 7).Value = r.DopRadnik;
            ws.Cell(row, 8).Value = r.DopFirma;
            ws.Cell(row, 9).Value = r.Neto;
            ws.Cell(row, 10).Value = r.ZaIsplatu;
            for (var col = 5; col <= 10; col++)
                ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
            if (row % 2 == 0)
                ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
            row++;
        }

        // Ukupno red
        var totRow = row;
        ws.Cell(totRow, 3).Value = "UKUPNO:";
        ws.Cell(totRow, 3).Style.Font.Bold = true;
        ws.Cell(totRow, 5).Value = Redovi.Sum(r => r.Bruto);
        ws.Cell(totRow, 6).Value = Redovi.Sum(r => r.Porez);
        ws.Cell(totRow, 7).Value = Redovi.Sum(r => r.DopRadnik);
        ws.Cell(totRow, 8).Value = Redovi.Sum(r => r.DopFirma);
        ws.Cell(totRow, 9).Value = Redovi.Sum(r => r.Neto);
        ws.Cell(totRow, 10).Value = Redovi.Sum(r => r.ZaIsplatu);
        for (var col = 5; col <= 10; col++)
        {
            ws.Cell(totRow, col).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(totRow, col).Style.Font.Bold = true;
        }

        ws.Columns().AdjustToContents();
        ws.Column(3).Width = Math.Min(ws.Column(3).Width, 36);
        ws.Column(4).Width = Math.Min(ws.Column(4).Width, 18);

        wb.SaveAs(putanja);
    }

    private void IzvozCsv(string putanja)
    {
        var sb = new StringBuilder();
        sb.AppendLine("R.B.;BR;IME I PREZIME;JMBG / MAT.BR.;BRUTO;POREZ;DOP.RADNIK;DOP.FIRMA;NETO;ZA ISPLATU");
        foreach (var r in Redovi)
            sb.AppendLine($"{r.Rb};{r.Broj};\"{r.ImePrez}\";{r.Jmbg};{r.Bruto:F2};{r.Porez:F2};{r.DopRadnik:F2};{r.DopFirma:F2};{r.Neto:F2};{r.ZaIsplatu:F2}");
        File.WriteAllText(putanja, sb.ToString(), Encoding.UTF8);
    }

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();
}

public class LdRegistarRed
{
    public int Rb { get; set; }
    public int Broj { get; set; }
    public string ImePrez { get; set; } = string.Empty;
    public string Jmbg { get; set; } = string.Empty;
    public decimal Bruto { get; set; }
    public decimal Neto { get; set; }
    public decimal DopRadnik { get; set; }
    public decimal DopFirma { get; set; }
    public decimal Porez { get; set; }
    public decimal ZaIsplatu { get; set; }
    public int Mesec { get; set; }
    public string Godina { get; set; } = string.Empty;
}
