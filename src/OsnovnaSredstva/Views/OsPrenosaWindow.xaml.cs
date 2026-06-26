using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace OsnovnaSredstva.Views;

public partial class OsPrenosaWindow : Window
{
    public OsPrenosaWindow(OsPrenosaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        vm.Log.CollectionChanged += (_, _) =>
        {
            if (LogBox.Items.Count > 0)
                LogBox.ScrollIntoView(LogBox.Items[LogBox.Items.Count - 1]);
        };

        // Auto-zatvori prozor 1.5s nakon uspješnog prenosa (DispatcherTimer — uvijek na UI threadu)
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(OsPrenosaViewModel.Uspjesno) && vm.Uspjesno)
            {
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(1500)
                };
                timer.Tick += (_, _) => { timer.Stop(); Close(); };
                timer.Start();
            }
        };
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
        base.OnKeyDown(e);
    }
}
