namespace Algoritam.WPF.Controls;

public static class Toast
{
    public static void Pokazi(string poruka, ToastTip tip = ToastTip.Info, int ms = 2800)
        => ToastWindow.Pokazi(poruka, tip, ms);
}
