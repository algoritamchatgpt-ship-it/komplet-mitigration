using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Generički dijalog za unos novog ili izmenu postojećeg reda u proizvoljnoj DBF tabeli.
/// Dinamički generiše polja iz DBF šeme (reuse OsPolje), isti obrazac kao OsUnosViewModel.
/// </summary>
public partial class DbfUnosIzmenaViewModel : ObservableObject
{
    public string Naslov { get; }
    public bool JeIzmena { get; }
    public Brush BojaHeaderBrush { get; }

    public ObservableCollection<OsPolje> Polja { get; } = [];
    public bool Uspesno { get; private set; }
    public Dictionary<string, object?>? Rezultat { get; private set; }

    [ObservableProperty] private string _statusPoruka = "";

    public DbfUnosIzmenaViewModel(
        DbfTableWriter.DbfSchema schema,
        string naslov,
        Dictionary<string, object?>? postojeciZapis = null,
        string bojaHeader = "#1A237E")
    {
        Naslov = naslov;
        JeIzmena = postojeciZapis is not null;
        BojaHeaderBrush = BrushIzHex(bojaHeader);

        foreach (var field in schema.Fields)
        {
            var polje = new OsPolje(field);
            if (postojeciZapis is not null && postojeciZapis.TryGetValue(field.Name, out var vrednost))
            {
                if (polje.JeDatum)
                    polje.DatumVrednost = vrednost as DateTime?;
                else if (polje.JeBool)
                    polje.BoolVrednost = vrednost is bool b && b;
                else
                    polje.Vrednost = vrednost?.ToString()?.Trim() ?? "";
            }
            Polja.Add(polje);
        }
    }

    private static SolidColorBrush BrushIzHex(string hex)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch { return new SolidColorBrush(Colors.DarkBlue); }
    }

    [RelayCommand]
    private void Sacuvaj(System.Windows.Window? window)
    {
        var zapis = new Dictionary<string, object?>();
        foreach (var polje in Polja)
        {
            object? vrednost;
            if (polje.JeDatum)
                vrednost = polje.DatumVrednost;
            else if (polje.JeBool)
                vrednost = polje.BoolVrednost;
            else if (polje.FieldType is 'N' or 'F')
            {
                var tekst = polje.Vrednost.Replace(',', '.');
                vrednost = decimal.TryParse(tekst, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var d) ? d : (decimal?)0m;
            }
            else
                vrednost = polje.Vrednost;

            zapis[polje.Name] = vrednost;
        }

        Rezultat = zapis;
        Uspesno = true;
        window?.Close();
    }

    [RelayCommand]
    private void Odustani(System.Windows.Window? window) => window?.Close();
}
