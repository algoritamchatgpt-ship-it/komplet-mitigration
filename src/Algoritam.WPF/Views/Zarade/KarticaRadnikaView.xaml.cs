using Algoritam.WPF.Utilities;
using Algoritam.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Algoritam.WPF.Views.Zarade;

public partial class KarticaRadnikaView : Window
{
    public KarticaRadnikaView()
    {
        InitializeComponent();
        Loaded += (_, _) => WindowPlacement.Restore(this, "KarticaRadnika", 980, 820);
        Closing += (_, _) => WindowPlacement.Save(this, "KarticaRadnika");
    }

    // Select all text in TextBoxes when they receive focus (handy for numeric fields)
    protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnPreviewGotKeyboardFocus(e);
        if (e.NewFocus is TextBox tb)
            tb.SelectAll();
    }

    private void Potvrdi_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is KarticaRadnikaViewModel vm)
        {
            vm.PotvrdiCommand.Execute(null);
            if (vm.Potvrdjen)
            {
                DialogResult = true;
                Close();
            }
        }
    }

    private void PotvrdiSaObracunom_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is KarticaRadnikaViewModel vm)
        {
            vm.PotvrdiSaObracunomCommand.Execute(null);
            if (vm.Potvrdjen)
            {
                DialogResult = true;
                Close();
            }
        }
    }

    private void Izlaz_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
