using Algoritam.WPF.Utilities;
using System.ComponentModel;
using System.Windows;
using Algoritam.WPF.ViewModels;

namespace Algoritam.WPF.Views.Zarade;

public partial class RadniciView : Window
{
    public RadniciView()
    {
        InitializeComponent();
        Loaded += (_, _) => WindowPlacement.Restore(this, "Radnici");
        Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is RadniciViewModel vm && vm.JeURezimuIzmene)
        {
            var result = MessageBox.Show(
                "Imate nesačuvane izmene. Da li želite da napustite bez čuvanja?",
                "Nesačuvane izmene",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }
        }
        WindowPlacement.Save(this, "Radnici");
    }

    private void Zatvori_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
