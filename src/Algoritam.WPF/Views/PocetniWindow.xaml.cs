using Algoritam.WPF.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace Algoritam.WPF.Views;

public partial class PocetniWindow : Window
{
    public event Action? UlazKliknut;

    public PocetniWindow(PocetniViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        BtnUlaz.Click    += (_, _) => UlazKliknut?.Invoke();
        BtnZatvori.Click += (_, _) => System.Windows.Application.Current.Shutdown();

        MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        };
    }
}
