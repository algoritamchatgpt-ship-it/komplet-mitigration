using Algoritam.Core.ViewModels;
using System.Windows;

namespace Algoritam.Core.Views;

public partial class IzvozTabelaWindow : Window
{
    public IzvozTabelaWindow(IzvozTabelaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
