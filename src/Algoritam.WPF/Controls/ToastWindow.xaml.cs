using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Algoritam.WPF.Controls;

public enum ToastTip { Info, Uspeh, Upozorenje, Greska }

public partial class ToastWindow : Window
{
    private static readonly Queue<ToastWindow> _red = new();
    private static double _offsetY = 0;
    private const double Razmak = 8;

    public static void Pokazi(string poruka, ToastTip tip = ToastTip.Info, int ms = 2800)
    {
        WpfApp.Current.Dispatcher.Invoke(() =>
        {
            var toast = new ToastWindow(poruka, tip);
            _red.Enqueue(toast);
            PrikaziSledeci(toast, ms);
        });
    }

    private static void PrikaziSledeci(ToastWindow toast, int ms)
    {
        var radnaObica = SystemParameters.WorkArea;
        toast.Left = radnaObica.Right - toast.Width - 16;
        toast.Top  = radnaObica.Bottom - toast.Height - 16 - _offsetY;
        _offsetY += toast.Height + Razmak;

        toast.Show();
        toast.Opacity = 0;

        var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
        toast.BeginAnimation(OpacityProperty, fadeIn);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (_, _) =>
            {
                _offsetY -= toast.ActualHeight + Razmak;
                if (_offsetY < 0) _offsetY = 0;
                _red.TryDequeue(out _);
                toast.Close();
            };
            toast.BeginAnimation(OpacityProperty, fadeOut);
        };
        timer.Start();
    }

    public ToastWindow(string poruka, ToastTip tip)
    {
        InitializeComponent();
        PorukaText.Text = poruka;

        (string ikonica, Color boja) = tip switch
        {
            ToastTip.Uspeh      => ("✓", Color.FromRgb(0x2E, 0x7D, 0x32)),
            ToastTip.Upozorenje => ("!", Color.FromRgb(0xE6, 0x51, 0x00)),
            ToastTip.Greska     => ("✕", Color.FromRgb(0xC6, 0x28, 0x28)),
            _                   => ("i", Color.FromRgb(0x15, 0x65, 0xC0)),
        };

        IkonicaText.Text = ikonica;
        IkonicaText.Foreground = Brushes.White;
        ToastBorder.Background = new SolidColorBrush(boja);

        Width = 320;
        SizeToContent = SizeToContent.Height;
    }
}
