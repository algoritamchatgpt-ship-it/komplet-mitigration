using Algoritam.WPF.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Algoritam.WPF.Views.GlavnaKnjiga;

public partial class GkDbfPregledView : Window
{
    public GkDbfPregledView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is GkDbfPregledViewModel vm)
        {
            vm.PropertyChanged += OnVmPropertyChanged;
            vm.ZatvaranjeZahtevano += Close;
            if (vm.Kolone.Count > 0) GenerujKolone(vm.Kolone);
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GkDbfPregledViewModel.Kolone) &&
            DataContext is GkDbfPregledViewModel vm)
            GenerujKolone(vm.Kolone);
    }

    private void GenerujKolone(IReadOnlyList<string> kolone)
    {
        MainDataGrid.Columns.Clear();
        foreach (var kol in kolone)
            MainDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = kol,
                Binding = new Binding($"[{kol}]")
            });
    }
}
