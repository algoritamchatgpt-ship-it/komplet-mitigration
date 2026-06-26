using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NaluniornaltraziWindow : Window
{
    public NaluniornaltraziWindow()
    {
        InitializeComponent();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        TxtTrazi.Focus();
    }

    private void TxtTrazi_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is not null)
        {
            var method = DataContext.GetType().GetMethod("NadjiCommand");
            method?.Invoke(DataContext, null);
        }
    }
}
