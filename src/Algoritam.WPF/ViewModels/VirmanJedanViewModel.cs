using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Algoritam.WPF.ViewModels;

public partial class VirmanJedanViewModel : ObservableObject
{
    public string FirmaNaziv { get; }
    public string FirmaZiro  { get; }
    public string FirmaMesto { get; }

    [ObservableProperty] private string  _nazRac  = "";
    [ObservableProperty] private string  _ziroRac = "";
    [ObservableProperty] private string  _svrha   = "";
    [ObservableProperty] private decimal _dug;
    [ObservableProperty] private string  _sif1    = "";
    [ObservableProperty] private string  _modelo  = "";
    [ObservableProperty] private string  _pozivO  = "";
    [ObservableProperty] private string  _modelZ  = "";
    [ObservableProperty] private string  _pozivZ  = "";
    [ObservableProperty] private string  _valuta  = "941";
    [ObservableProperty] private DateTime _datDok = DateTime.Today;
    [ObservableProperty] private string  _nazRacZ  = "";
    [ObservableProperty] private string  _ziroRacZ = "";

    public bool Potvrdjeno { get; private set; }
    public event Action? ZatvaranjeZahtevano;

    public VirmanJedanViewModel(VirmanStavka st, string firmaNaziv, string firmaZiro, string firmaMesto)
    {
        FirmaNaziv = firmaNaziv;
        FirmaZiro  = firmaZiro;
        FirmaMesto = firmaMesto;

        NazRac   = st.NazRac;
        ZiroRac  = st.ZiroRac;
        Svrha    = st.Svrha;
        Dug      = st.Dug;
        Sif1     = st.Sif1;
        Modelo   = st.Modelo;
        PozivO   = st.PozivO;
        ModelZ   = st.ModelZ;
        PozivZ   = st.PozivZ;
        Valuta   = string.IsNullOrWhiteSpace(st.Valuta) ? "941" : st.Valuta;
        DatDok   = st.DatDok <= DateTime.MinValue ? DateTime.Today : st.DatDok;
        NazRacZ  = st.NazRacZ;
        ZiroRacZ = st.ZiroRacZ;
    }

    public void CopyBackTo(VirmanStavka st)
    {
        st.NazRac   = NazRac;
        st.ZiroRac  = ZiroRac;
        st.Svrha    = Svrha;
        st.Dug      = Dug;
        st.Sif1     = Sif1;
        st.Modelo   = Modelo;
        st.PozivO   = PozivO;
        st.ModelZ   = ModelZ;
        st.PozivZ   = PozivZ;
        st.Valuta   = Valuta;
        st.DatDok   = DatDok;
        st.NazRacZ  = NazRacZ;
        st.ZiroRacZ = ZiroRacZ;
    }

    [RelayCommand]
    private void Potvrdi()
    {
        Potvrdjeno = true;
        ZatvaranjeZahtevano?.Invoke();
    }

    [RelayCommand]
    private void Odustani()
    {
        Potvrdjeno = false;
        ZatvaranjeZahtevano?.Invoke();
    }

    [RelayCommand]
    private void Stampaj()
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;

        var fd = GradeFlowDocument();
        var paginator = ((IDocumentPaginatorSource)fd).DocumentPaginator;
        paginator.PageSize = new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);
        dlg.PrintDocument(paginator, "Nalog za prenos");
    }

    internal FlowDocument GradeFlowDocument()
    {
        var fd = new FlowDocument
        {
            FontFamily = new FontFamily("Tahoma"),
            FontSize   = 11,
            PagePadding = new Thickness(40, 30, 40, 30),
        };

        void Row(string label, string value, bool bold = false)
        {
            var p = new Paragraph();
            p.Inlines.Add(new Bold(new Run($"{label}: ")) { });
            var r = new Run(value);
            if (bold) r.FontWeight = FontWeights.Bold;
            p.Inlines.Add(r);
            p.Margin = new Thickness(0, 1, 0, 1);
            fd.Blocks.Add(p);
        }

        fd.Blocks.Add(new Paragraph(new Bold(new Run("NALOG ZA PRENOS")))
        {
            FontSize  = 15,
            TextAlignment = TextAlignment.Center,
            Margin    = new Thickness(0, 0, 0, 12),
        });

        fd.Blocks.Add(new Paragraph(new Underline(new Run("PLATILAC (zaduženje)")))
        { FontWeight = FontWeights.Bold, Margin = new Thickness(0, 4, 0, 2) });

        Row("Naziv",  FirmaNaziv);
        Row("Račun",  FirmaZiro);
        Row("Model Z", ModelZ);
        Row("Poziv Z", PozivZ);

        fd.Blocks.Add(new Paragraph(new Underline(new Run("PRIMALAC (odobrenje)")))
        { FontWeight = FontWeights.Bold, Margin = new Thickness(0, 8, 0, 2) });

        Row("Naziv",  NazRac);
        Row("Račun",  ZiroRac);
        Row("Model O", Modelo);
        Row("Poziv O", PozivO);

        fd.Blocks.Add(new Paragraph(new Underline(new Run("DETALJI")))
        { FontWeight = FontWeights.Bold, Margin = new Thickness(0, 8, 0, 2) });

        Row("Iznos (RSD)", Dug.ToString("N2", CultureInfo.CurrentCulture), bold: true);
        Row("Šifra plaćanja", Sif1);
        Row("Svrha plaćanja", Svrha);
        Row("Datum dokumenta", DatDok == DateTime.MinValue ? "" : DatDok.ToString("dd.MM.yyyy"));
        Row("Valuta", Valuta);

        return fd;
    }
}
