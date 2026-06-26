using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using Algoritam.Infrastructure.Migration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Safe varijanta pregleda tabela zarada — skenira DBF fajlove i po potrebi kopira
/// LD tabele iz template-a. (Aplikacija koristi DBF direktno — nema SQLite uvoza.)
/// </summary>
public partial class LdFormiranjeTabelaSafeViewModel : ObservableObject
{
    private readonly AppState _appState;

    [ObservableProperty] private string _naslov = "PREGLED DBF FAJLOVA ZARADA (SAFE)";
    [ObservableProperty] private string _poruka = "Kliknite PROCENI za pregled dostupnih DBF fajlova zarada.";
    [ObservableProperty] private bool _radi;
    [ObservableProperty] private int _progres;
    [ObservableProperty] private int _maksProgres = 1;
    [ObservableProperty] private int _ukupnoIzvora;
    [ObservableProperty] private int _postojecihStavki;
    [ObservableProperty] private int _ukupnoUneto;
    [ObservableProperty] private int _ukupnoIzvezeno;
    [ObservableProperty] private int _brojFajlovaIzvoza;
    [ObservableProperty] private string _backupPutanja = string.Empty;

    public LdFormiranjeTabelaSafeViewModel(AppState appState)
    {
        _appState = appState;
    }

    [RelayCommand]
    private void KreirajLdTabele()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var firmaRoot = Directory.GetParent(folder)?.FullName;
        var template = FoxWorkspaceSupport.FindTemplateF1(firmaRoot);
        if (string.IsNullOrWhiteSpace(template) || !Directory.Exists(template))
        {
            BackupPutanja = string.Empty;
            UkupnoIzvezeno = 0;
            BrojFajlovaIzvoza = 0;
            Poruka = "Template F1 nije pronađen. Dodajte templates/F1 ili old-project/F1.";
            return;
        }

        BackupPutanja = template;
        var copied = FoxWorkspaceSupport.CopyLdTablesFromTemplate(template, folder, overwrite: false);
        UkupnoIzvezeno = copied;
        BrojFajlovaIzvoza = copied;
        if (copied == 0)
        {
            Poruka = FoxWorkspaceSupport.ContainsAnyLdTable(folder)
                ? "LD tabele vec postoje u folderu firme."
                : "Nije kopirana nijedna LD tabela (proverite template).";
            return;
        }

        Poruka = $"Kreirane/kopirane LD tabele: {copied}. Template: {template}";
    }

    [RelayCommand]
    private Task IzveziFoxAsync()
    {
        Poruka = "Aplikacija koristi DBF direktno — izvoz nije potreban.";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ProceniAsync()
    {
        await SkenirajAsync();
    }

    [RelayCommand]
    private async Task FormirajSafeAsync()
    {
        await SkenirajAsync();
    }

    private async Task SkenirajAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        Radi = true;
        Progres = 0;

        try
        {
            var popis = await Task.Run(() => LdObracunDbfReader.PopisFajlova(folder));
            UkupnoIzvora = popis.Count;
            UkupnoUneto = popis.Sum(p => p.BrojZapisa);
            PostojecihStavki = UkupnoUneto;
            MaksProgres = Math.Max(popis.Count, 1);
            Progres = popis.Count;

            Poruka = popis.Count == 0
                ? "Nije pronađen nijedan LD*/LDP*/LDB*/LDI*/LDR* fajl u folderu firme."
                : $"Pronađeno fajlova: {popis.Count}, ukupno zapisa: {UkupnoUneto}. Aplikacija čita DBF direktno.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška skeniranja: {ex.Message}";
        }
        finally
        {
            Radi = false;
        }
    }
}
