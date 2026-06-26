using Algoritam.Core.ViewModels;
using System.Windows;

namespace Algoritam.Core.Views;

public partial class PartnerKarticaWindow : Window
{
    public PartnerKarticaWindow(PartnerKarticaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
