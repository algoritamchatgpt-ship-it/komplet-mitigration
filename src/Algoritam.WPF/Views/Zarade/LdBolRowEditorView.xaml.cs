using System.Collections;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdBolRowEditorView : Window
{
    public string Naslov { get; }
    public IList Stavke { get; }

    public LdBolRowEditorView(string naslov, object stavka)
    {
        Naslov = naslov;
        Stavke = new ArrayList { stavka };

        InitializeComponent();
        Title = Naslov;
        DataContext = this;
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
