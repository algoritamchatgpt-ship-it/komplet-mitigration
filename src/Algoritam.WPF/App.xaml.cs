using Algoritam.Application;
using Algoritam.Application.Services;
using Algoritam.Infrastructure.Services;
using Algoritam.WPF.Controls;
using Algoritam.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using IPutanjaService = Algoritam.Application.Services.IPutanjaService;
using PutanjaService = Algoritam.Infrastructure.Services.PutanjaService;

namespace Algoritam.WPF;

public partial class App : System.Windows.Application
{
    private static readonly string[] ConfirmButtonTokens =
    {
        "POTVRDI",
        "SACUVAJ",
        "SNIMI",
        "PREUZMI",
        "ULAZ",
        "PRIJAVA",
        "OK",
        "DA"
    };

    private ServiceProvider _serviceProvider = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ButtonShortcutService.Initialize();

        // Globalna navigacija unazad — Backspace na poziciji 0 = Shift+Tab (prethodno polje)
        EventManager.RegisterClassHandler(
            typeof(TextBox),
            UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(OnTextBoxBackspaceNavigation));

        // ESC zatvara radne forme u modulima (ne dotiče Login/FirmaIzbor/MainWindow)
        EventManager.RegisterClassHandler(
            typeof(Window),
            UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(OnWindowEscape));
        EventManager.RegisterClassHandler(
            typeof(Window),
            UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(OnWindowCloseShortcut));
        EventManager.RegisterClassHandler(
            typeof(Window),
            UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(OnWindowShortcutHelp));
        EventManager.RegisterClassHandler(
            typeof(Window),
            UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(OnWindowEnterConfirm));

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                "logs/algoritam-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("=== Aplikacija pokrenuta ===");

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "CRASH - neobradjena greska");
            MessageBox.Show(
                $"Desila se neočekivana greška u programu.\n\n" +
                $"Opis: {args.Exception.Message}\n\n" +
                $"Greška je zabeležena u log fajlu. Ako se problem ponavlja, kontaktirajte tehničku podršku.",
                "Algoritam — Greška",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        PrikaziPocetni();
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<AppState>();
        services.AddSingleton<IPutanjaService, PutanjaService>();
        services.AddSingleton<IFirmaService>(sp =>
            new DbfFirmaService(sp.GetRequiredService<IPutanjaService>()));
        services.AddSingleton<IAuthService>(sp =>
            new SmartAuthService(sp.GetRequiredService<IPutanjaService>()));
        services.AddSingleton<ILoginSessionService>(sp =>
            new FileLoginSessionService(sp.GetRequiredService<IPutanjaService>()));

        services.AddTransient<PocetniViewModel>();
        services.AddTransient<LoginViewModel>(sp => new LoginViewModel(
            sp.GetRequiredService<IAuthService>(),
            sp.GetRequiredService<ILoginSessionService>(),
            sp.GetRequiredService<IPutanjaService>(),
            sp.GetRequiredService<AppState>()));
        services.AddTransient<FirmaIzborViewModel>();
        services.AddTransient<MainViewModel>();
    }

    private void PrikaziPocetni()
    {
        Log.Information("Otvaram pocetni ekran");
        var pocetniVm = _serviceProvider.GetRequiredService<PocetniViewModel>();
        var pocetniWin = new Views.PocetniWindow(pocetniVm);
        pocetniWin.UlazKliknut += () => PrikaziLogin(pocetniWin);
        pocetniWin.Show();
    }

    private void PrikaziLogin(Views.PocetniWindow pocetniWin)
    {
        Log.Information("Otvaram login ekran");
        var vm = _serviceProvider.GetRequiredService<LoginViewModel>();
        var loginWin = new Views.LoginWindow(vm);

        loginWin.PrijavaUspela += () =>
        {
            Log.Information("Prijava uspesna");
            pocetniWin.Close();
            loginWin.Close();
            PrikaziFirmaIzbor();
        };

        loginWin.Show();
    }

    private void PrikaziFirmaIzbor()
    {
        Log.Information("Otvaram izbor firme");
        var vm = _serviceProvider.GetRequiredService<FirmaIzborViewModel>();
        var win = new Views.FirmaIzborWindow(vm);

        win.FirmaIzabrana += () =>
        {
            Log.Information("Firma izabrana");
            win.Close();
            PrikaziGlavniMeni();
        };

        win.OtkacenoPrijavljeni += () =>
        {
            Log.Information("Odjava sa izbora firme");
            win.Close();
            PrikaziPocetni();
        };

        win.Show();
    }

    private void PrikaziGlavniMeni()
    {
        Log.Information("Otvaram glavni meni");
        var vm = _serviceProvider.GetRequiredService<MainViewModel>();
        var win = new Views.MainWindow(vm);

        vm.OdjavaSeTrazena += () =>
        {
            Log.Information("Odjava iz glavnog menija");
            win.Close();
            PrikaziPocetni();
        };

        vm.VratiseFirmaIzboru += () =>
        {
            Log.Information("Povratak na izbor firme");
            win.Close();
            PrikaziFirmaIzbor();
        };

        win.Show();
    }

    private static void OnWindowEscape(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        if (Keyboard.Modifiers != ModifierKeys.None) return;

        if (sender is Window w && IsFeatureWindow(w))
        {
            w.Close();
            e.Handled = true;
        }
    }

    private static void OnWindowCloseShortcut(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.W) return;
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        if (sender is not Window window) return;
        if (!IsFeatureWindow(window)) return;

        window.Close();
        e.Handled = true;
    }

    private static void OnWindowShortcutHelp(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F1) return;
        if (Keyboard.Modifiers != ModifierKeys.None) return;
        if (sender is not Window window) return;
        if (!IsFeatureWindow(window)) return;

        var overview = ButtonShortcutService.BuildShortcutOverview(window);
        MessageBox.Show(window, overview, "Brze precice", MessageBoxButton.OK, MessageBoxImage.Information);
        e.Handled = true;
    }

    private static void OnWindowEnterConfirm(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (Keyboard.Modifiers != ModifierKeys.None) return;
        if (sender is not Window window) return;
        if (!IsFeatureWindow(window)) return;
        if (IsInputEditingControl(Keyboard.FocusedElement as DependencyObject)) return;

        var buttons = GetEligibleButtons(window);
        if (buttons.Count == 0) return;

        foreach (var button in buttons)
        {
            if (!button.IsDefault) continue;
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
            e.Handled = true;
            return;
        }

        var confirmCandidates = new List<Button>();
        foreach (var button in buttons)
        {
            if (IsConfirmButton(button))
                confirmCandidates.Add(button);
        }

        if (confirmCandidates.Count != 1) return;

        var confirmButton = confirmCandidates[0];
        confirmButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, confirmButton));
        e.Handled = true;
    }

    private static void OnTextBoxBackspaceNavigation(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Back) return;
        if (sender is not TextBox tb) return;
        // Aktiviraj samo kada kursor stoji na poziciji 0 bez selekcije —
        // u tom slučaju Backspace ionako ne briše ništa, pa prebacujemo fokus unazad.
        if (tb.SelectionStart != 0 || tb.SelectionLength != 0) return;

        tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
        e.Handled = true;
    }

    private static bool IsFeatureWindow(Window window)
    {
        var type = window.GetType();
        var @namespace = type.Namespace ?? string.Empty;

        if (@namespace.Contains(".Views.Zarade", StringComparison.Ordinal))
            return true;

        return @namespace.Contains("OsnovnaSredstva.Views", StringComparison.Ordinal)
            && type.Name is not ("PocetniWindow" or "LoginWindow" or "FirmaIzborWindow");
    }

    private static List<Button> GetEligibleButtons(Window window)
    {
        var discovered = new List<Button>();
        CollectButtons(window, discovered);

        var eligible = new List<Button>(discovered.Count);
        foreach (var button in discovered)
        {
            if (!button.IsVisible || !button.IsEnabled)
                continue;

            eligible.Add(button);
        }

        return eligible;
    }

    private static void CollectButtons(DependencyObject root, List<Button> buttons)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is Button button && button.GetType() == typeof(Button))
                buttons.Add(button);

            if (child is DependencyObject dependencyObject)
                CollectButtons(dependencyObject, buttons);
        }
    }

    private static bool IsInputEditingControl(DependencyObject? focusTarget)
    {
        var current = focusTarget;
        while (current != null)
        {
            if (current is TextBoxBase
                || current is PasswordBox
                || current is ComboBox
                || current is DatePicker
                || current is DataGrid
                || current is DataGridCell)
            {
                return true;
            }

            current = GetParentObject(current);
        }

        return false;
    }

    private static DependencyObject? GetParentObject(DependencyObject child)
    {
        if (child is FrameworkElement frameworkElement && frameworkElement.Parent is DependencyObject parent)
            return parent;

        if (child is FrameworkContentElement frameworkContentElement
            && frameworkContentElement.Parent is DependencyObject contentParent)
        {
            return contentParent;
        }

        return VisualTreeHelper.GetParent(child);
    }

    private static bool IsConfirmButton(Button button)
    {
        var normalized = NormalizeForTokenMatch(ExtractButtonLabel(button));
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        foreach (var token in ConfirmButtonTokens)
        {
            if (normalized.Contains(token, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string ExtractButtonLabel(Button button)
    {
        var raw = button.Content switch
        {
            null => string.Empty,
            string text => text,
            TextBlock textBlock => textBlock.Text ?? string.Empty,
            AccessText accessText => accessText.Text ?? string.Empty,
            _ => button.Content.ToString() ?? string.Empty
        };

        return raw.Replace("_", string.Empty).Trim();
    }

    private static string NormalizeForTokenMatch(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized.ToUpperInvariant())
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            sb.Append(c);
        }

        return sb.ToString();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.GetService<AppState>()?.Odjavi();
        Log.Information("=== Aplikacija zatvorena ===");
        Log.CloseAndFlush();
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
}
