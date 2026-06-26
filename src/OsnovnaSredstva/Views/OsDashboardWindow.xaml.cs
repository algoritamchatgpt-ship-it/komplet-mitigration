using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class OsDashboardWindow : Window
{
    public OsDashboardWindow(OsDashboardViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
