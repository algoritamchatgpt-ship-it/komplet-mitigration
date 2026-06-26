using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdPrenosiView : Window
{
    private LdPrenosiViewModel? _vm;

    public LdPrenosiView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += (_, _) => OtkaciViewModel();
    }

    private void OnZatvaranjeZahtevano()
    {
        Close();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        OtkaciViewModel();

        _vm = e.NewValue as LdPrenosiViewModel;
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
