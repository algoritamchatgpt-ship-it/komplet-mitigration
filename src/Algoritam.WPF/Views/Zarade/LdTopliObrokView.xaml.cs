using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdTopliObrokView : Window
{
    private LdTopliObrokViewModel? _vm;

    public LdTopliObrokView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += (_, _) => OtkaciViewModel();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        OtkaciViewModel();
        _vm = e.NewValue as LdTopliObrokViewModel;
        if (_vm != null)
            _vm.ZatvaranjeZahtevano += Close;
    }

    private void OtkaciViewModel()
    {
        if (_vm != null)
            _vm.ZatvaranjeZahtevano -= Close;
        _vm = null;
    }
}
