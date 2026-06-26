using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;
using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class NalslagWindow : Window
{
    private readonly string _firmPath;

    public NalslagWindow(NalslagViewModel vm, string firmPath)
    {
        InitializeComponent();
        DataContext = vm;
        _firmPath   = firmPath;
        vm.ZatvoriFormu    += Close;
        vm.OtvoriNalSlag2  += OpenNalSlag2;
    }

    private void OpenNalSlag2(IReadOnlyList<NalslagRow> rows)
    {
        var vm2  = new NalSlag2ViewModel(_firmPath, rows);
        var win2 = new NalSlag2Window(vm2, _firmPath);
        win2.ShowDialog();
    }
}
