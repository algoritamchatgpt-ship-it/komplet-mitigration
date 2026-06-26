using Algoritam.Core.ViewModels;
using System.Windows;

namespace Algoritam.Core.Views;

public partial class PartneriWindow : Window
{
    public PartneriWindow(PartneriViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
