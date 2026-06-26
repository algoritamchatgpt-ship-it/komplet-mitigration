using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdPrenosKreditaView : Window
{
    private LdPrenosKreditaViewModel? _vm;

    public LdPrenosKreditaView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += (_, _) => OtkaciViewModel();
    }

    private void OnZatvaranjeZahtevano()
    {
        try
        {
            DialogResult = true;
        }
        catch
        {
            // Ignore when window is not opened as modal dialog.
        }

        Close();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        OtkaciViewModel();

        _vm = e.NewValue as LdPrenosKreditaViewModel;
        if (_vm != null)
            _vm.ZatvaranjeZahtevano += OnZatvaranjeZahtevano;
    }

    private void OtkaciViewModel()
    {
        if (_vm != null)
            _vm.ZatvaranjeZahtevano -= OnZatvaranjeZahtevano;

        _vm = null;
    }
}
