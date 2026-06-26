using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace OsnovnaSredstva.Views;

public partial class OsXmlOstWindow : Window
{
    public OsXmlOstWindow(OsXmlOstViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvaranjeZahtevano += Close;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is OsXmlOstViewModel vm && vm.ImaNeSnimljenih)
        {
            var odg = MessageBox.Show(
                "Imate nesačuvane promjene. Zatvoriti bez čuvanja?",
                "Nesačuvane promjene", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (odg != MessageBoxResult.Yes)
                e.Cancel = true;
        }
        base.OnClosing(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
        base.OnKeyDown(e);
    }
}
