using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class FormiranjeLozinkiWindow : Window
{
    public FormiranjeLozinkiWindow()
    {
        InitializeComponent();
        BtnZatvori.Click += (_, _) => Close();
    }
}
