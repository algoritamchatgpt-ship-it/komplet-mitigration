using Algoritam.Core.ViewModels;
using System.Windows;

namespace Algoritam.Core.Views;

public partial class FormiranjeLozinkiWindow : Window
{
    public FormiranjeLozinkiWindow(FormiranjeLozinkiViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
