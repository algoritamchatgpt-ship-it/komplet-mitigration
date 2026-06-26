using Algoritam.WPF.Utilities;
using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += (_, _) => WindowPlacement.Restore(this, "MainWindow");
        Closing += (_, _) => WindowPlacement.Save(this, "MainWindow");
    }
}
