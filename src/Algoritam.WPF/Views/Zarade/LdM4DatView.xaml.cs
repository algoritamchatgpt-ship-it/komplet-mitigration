using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdM4DatView : Window
{
    public LdM4DatView(string folderPath)
    {
        InitializeComponent();
        var vm = new LdM4DatViewModel(folderPath);
        vm.ZatvaranjeZahtevano += Close;
        DataContext = vm;
    }
}
