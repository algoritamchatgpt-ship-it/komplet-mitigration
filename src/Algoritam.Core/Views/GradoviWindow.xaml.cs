using Algoritam.Core.ViewModels;
using System.Windows;

namespace Algoritam.Core.Views;

public partial class GradoviWindow : Window
{
    public GradoviWindow(GradoviViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
