using System.Windows;

namespace Algoritam.WPF.Utilities;

/// <summary>
/// Posrednik za DataBinding u elementima koji nisu u vizualnom stablu (npr. DataGridColumn).
/// Koristiti kao StaticResource unutar DataGrid.Resources da bi kolone mogle da binduju VM.
/// </summary>
public class BindingProxy : Freezable
{
    protected override Freezable CreateInstanceCore() => new BindingProxy();

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy));

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }
}
