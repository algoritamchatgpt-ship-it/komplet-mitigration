using Microsoft.Win32;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.ViewModels;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;

namespace OsnovnaSredstva.Views;

public partial class PrintPreviewWindow : Window
{
    private readonly Uri _packUri;
    private XpsDocument? _xps;
    private Package? _pkg;
    private MemoryStream? _ms;
    private readonly byte[]? _pdfBytes;
    private readonly string _pdfNaziv;
    private readonly string _naslov;

    public PrintPreviewWindow(
        FlowDocument doc,
        byte[]? pdfBytes = null,
        string pdfNaziv = "izvestaj.pdf",
        string naslov = "")
    {
        InitializeComponent();
        _pdfBytes = pdfBytes;
        _pdfNaziv = pdfNaziv;
        _naslov = naslov;

        if (pdfBytes != null)
        {
            BtnPosaljiMail.Visibility = Visibility.Visible;
            BtnSacuvajPdf.Visibility  = Visibility.Visible;
        }

        _packUri = new Uri($"pack://preview{Guid.NewGuid():N}/");
        try
        {
            _ms  = new MemoryStream();
            _pkg = Package.Open(_ms, FileMode.Create, FileAccess.ReadWrite);
            PackageStore.AddPackage(_packUri, _pkg);
            _xps = new XpsDocument(_pkg, CompressionOption.Fast, _packUri.AbsoluteUri);
            XpsDocument.CreateXpsDocumentWriter(_xps)
                       .Write(((IDocumentPaginatorSource)doc).DocumentPaginator);
            Viewer.Document = _xps.GetFixedDocumentSequence();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greska pri generisanju pregleda:\n{ex.Message}",
                "Pregled", MessageBoxButton.OK, MessageBoxImage.Warning);
            Loaded += (_, _) => Close();
        }
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();

    private void OnSacuvajPdfClick(object sender, RoutedEventArgs e)
    {
        if (_pdfBytes == null) return;

        var dialog = new SaveFileDialog
        {
            Title      = "Sačuvaj PDF",
            Filter     = "PDF fajlovi (*.pdf)|*.pdf",
            FileName   = _pdfNaziv,
            DefaultExt = ".pdf"
        };

        if (dialog.ShowDialog(this) != true) return;

        try
        {
            File.WriteAllBytes(dialog.FileName, _pdfBytes);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Greška pri snimanju:\n{ex.Message}",
                "Sačuvaj PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnPosaljiMailClick(object sender, RoutedEventArgs e)
    {
        if (_pdfBytes == null) return;

        var podesavanjaSvc = new EmailPodesavanjaService();
        var emailSvc = new EmailService(podesavanjaSvc);
        var vm = new PosaljiMailViewModel(emailSvc, podesavanjaSvc, _pdfBytes, _pdfNaziv, _naslov);
        new PosaljiMailWindow(vm, podesavanjaSvc) { Owner = this }.ShowDialog();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Viewer.Document = null;
        _xps?.Close();
        _pkg?.Close();
        _ms?.Dispose();
        try { PackageStore.RemovePackage(_packUri); } catch { }
    }
}
