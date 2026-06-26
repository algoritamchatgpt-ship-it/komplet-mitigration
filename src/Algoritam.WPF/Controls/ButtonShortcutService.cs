using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace Algoritam.WPF.Controls;

public static class ButtonShortcutService
{
    // Alt+slovo je dovoljno brzo za svakodnevni rad i ne menja postojece F-precice.
    private const ModifierKeys DefaultModifiers = ModifierKeys.Alt;
    private static bool _initialized;
    private static readonly ConditionalWeakTable<Window, ShortcutRegistry> Registries = new();

    public static readonly DependencyProperty DisableAutoShortcutProperty =
        DependencyProperty.RegisterAttached(
            "DisableAutoShortcut",
            typeof(bool),
            typeof(ButtonShortcutService),
            new PropertyMetadata(false));

    private static readonly DependencyProperty IsShortcutAssignedProperty =
        DependencyProperty.RegisterAttached(
            "IsShortcutAssigned",
            typeof(bool),
            typeof(ButtonShortcutService),
            new PropertyMetadata(false));

    public static bool GetDisableAutoShortcut(DependencyObject obj)
        => (bool)obj.GetValue(DisableAutoShortcutProperty);

    public static void SetDisableAutoShortcut(DependencyObject obj, bool value)
        => obj.SetValue(DisableAutoShortcutProperty, value);

    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        EventManager.RegisterClassHandler(
            typeof(Button),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnButtonLoaded),
            handledEventsToo: true);
    }

    private static void OnButtonLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        // Ignore framework internal subclasses (CalendarDayButton, etc.).
        if (button.GetType() != typeof(Button))
            return;

        if (GetDisableAutoShortcut(button))
            return;

        if ((bool)button.GetValue(IsShortcutAssignedProperty))
            return;

        var window = Window.GetWindow(button);
        if (window == null)
            return;

        var registry = Registries.GetValue(window, static _ => new ShortcutRegistry());
        registry.SeedFromWindow(window);

        if (!TryAssignShortcut(button, window, registry, out var shortcutDisplay))
            return;

        button.SetValue(IsShortcutAssignedProperty, true);
        UpdateToolTip(button, shortcutDisplay);
    }

    private static bool TryAssignShortcut(
        Button button,
        Window window,
        ShortcutRegistry registry,
        out string shortcutDisplay)
    {
        shortcutDisplay = string.Empty;

        foreach (var key in EnumerateCandidateKeys(button))
        {
            var token = new ShortcutToken(key, DefaultModifiers);
            if (registry.UsedShortcuts.Contains(token))
                continue;

            var binding = new KeyBinding(new InvokeButtonCommand(button), new KeyGesture(key, DefaultModifiers));
            window.InputBindings.Add(binding);
            registry.UsedShortcuts.Add(token);
            registry.ShortcutDescriptions[token] = BuildButtonActionLabel(button);

            shortcutDisplay = BuildShortcutDisplay(key, DefaultModifiers);
            return true;
        }

        return false;
    }

    private static IEnumerable<Key> EnumerateCandidateKeys(Button button)
    {
        var emitted = new HashSet<Key>();

        foreach (var key in KeysFromText(ExtractLabel(button.Content)))
        {
            if (emitted.Add(key))
                yield return key;
        }

        for (var c = 'A'; c <= 'Z'; c++)
        {
            var key = CharToKey(c);
            if (key.HasValue && emitted.Add(key.Value))
                yield return key.Value;
        }

        for (var c = '1'; c <= '9'; c++)
        {
            var key = CharToKey(c);
            if (key.HasValue && emitted.Add(key.Value))
                yield return key.Value;
        }

        var zeroKey = CharToKey('0');
        if (zeroKey.HasValue && emitted.Add(zeroKey.Value))
            yield return zeroKey.Value;

        for (var i = 1; i <= 12; i++)
        {
            var key = Key.F1 + (i - 1);
            if (emitted.Add(key))
                yield return key;
        }
    }

    private static IEnumerable<Key> KeysFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var normalized = NormalizeShortcutText(text);
        var emitted = new HashSet<Key>();

        foreach (var c in normalized)
        {
            var key = CharToKey(c);
            if (key.HasValue && emitted.Add(key.Value))
                yield return key.Value;
        }
    }

    private static string ExtractLabel(object? content)
    {
        return content switch
        {
            null => string.Empty,
            string s => s.Replace("_", string.Empty).Trim(),
            TextBlock tb => (tb.Text ?? string.Empty).Replace("_", string.Empty).Trim(),
            AccessText at => (at.Text ?? string.Empty).Replace("_", string.Empty).Trim(),
            _ => (content.ToString() ?? string.Empty).Replace("_", string.Empty).Trim()
        };
    }

    private static string NormalizeShortcutText(string value)
    {
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

    private static Key? CharToKey(char c)
    {
        if (c is >= 'A' and <= 'Z')
            return Key.A + (c - 'A');

        if (c is >= '0' and <= '9')
            return Key.D0 + (c - '0');

        return null;
    }

    private static string BuildShortcutDisplay(Key key, ModifierKeys modifiers)
    {
        var keyText = key switch
        {
            >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(CultureInfo.InvariantCulture),
            _ => key.ToString().ToUpperInvariant()
        };

        var parts = new List<string>(4);
        if ((modifiers & ModifierKeys.Control) != 0)
            parts.Add("Ctrl");
        if ((modifiers & ModifierKeys.Alt) != 0)
            parts.Add("Alt");
        if ((modifiers & ModifierKeys.Shift) != 0)
            parts.Add("Shift");
        parts.Add(keyText);

        return string.Join("+", parts);
    }

    public static string BuildShortcutOverview(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var registry = Registries.GetValue(window, static _ => new ShortcutRegistry());
        registry.SeedFromWindow(window);

        var lines = new List<string>
        {
            "Brze precice za ovaj prozor:",
            "- Esc: Izlaz / zatvaranje prozora",
            "- Ctrl+W: Zatvaranje aktivnog prozora",
            "- F1: Prikaz ove pomoci"
        };

        var entries = BuildShortcutEntries(window, registry);
        if (entries.Count == 0)
        {
            lines.Add("- Nema dodatnih precica na ovom ekranu.");
        }
        else
        {
            lines.Add(string.Empty);
            lines.Add("Dodatne precice:");
            foreach (var entry in entries)
            {
                lines.Add($"- {entry.Gesture}: {entry.Description}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static List<ShortcutEntry> BuildShortcutEntries(Window window, ShortcutRegistry registry)
    {
        var seen = new HashSet<ShortcutToken>();
        var entries = new List<ShortcutEntry>();

        foreach (var inputBinding in window.InputBindings)
        {
            if (inputBinding is not KeyBinding keyBinding)
                continue;

            var token = new ShortcutToken(keyBinding.Key, keyBinding.Modifiers);
            if (!seen.Add(token))
                continue;

            var gesture = BuildShortcutDisplay(keyBinding.Key, keyBinding.Modifiers);
            var description = ResolveShortcutDescription(token, keyBinding, registry);
            entries.Add(new ShortcutEntry(gesture, description));
        }

        return entries
            .OrderBy(e => e.Gesture, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ResolveShortcutDescription(
        ShortcutToken token,
        KeyBinding keyBinding,
        ShortcutRegistry registry)
    {
        if (registry.ShortcutDescriptions.TryGetValue(token, out var autoDescription)
            && !string.IsNullOrWhiteSpace(autoDescription))
        {
            return autoDescription;
        }

        if (keyBinding.Command is RoutedUICommand routedUiCommand
            && !string.IsNullOrWhiteSpace(routedUiCommand.Text))
        {
            return routedUiCommand.Text;
        }

        if (keyBinding.Command is RoutedCommand routedCommand)
        {
            if (!string.IsNullOrWhiteSpace(routedCommand.Name))
                return routedCommand.Name;
        }

        if (keyBinding.CommandParameter is string text && !string.IsNullOrWhiteSpace(text))
            return text.Trim();

        return "Komanda";
    }

    private static string BuildButtonActionLabel(Button button)
    {
        var label = ExtractLabel(button.Content);
        return string.IsNullOrWhiteSpace(label)
            ? "Aktiviraj dugme"
            : $"Aktiviraj: {label}";
    }

    private static void UpdateToolTip(Button button, string shortcutDisplay)
    {
        var description = ExtractToolTipText(button.ToolTip);
        if (string.IsNullOrWhiteSpace(description))
        {
            var label = ExtractLabel(button.Content);
            description = string.IsNullOrWhiteSpace(label)
                ? "Akcija dugmeta."
                : $"Akcija: {label}.";
        }

        description = RemoveShortcutLines(description);
        button.ToolTip = $"{description}{Environment.NewLine}Precica: {shortcutDisplay}";
    }

    private static string ExtractToolTipText(object? toolTip)
    {
        return toolTip switch
        {
            null => string.Empty,
            string s => s.Trim(),
            TextBlock tb => tb.Text?.Trim() ?? string.Empty,
            ToolTip tt when tt.Content is string s => s.Trim(),
            ToolTip tt when tt.Content is TextBlock tb => tb.Text?.Trim() ?? string.Empty,
            _ => string.Empty
        };
    }

    private static string RemoveShortcutLines(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        var lines = description.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
        var filtered = new List<string>(lines.Length);

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("Precica:", System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (trimmed.StartsWith("Shortcut:", System.StringComparison.OrdinalIgnoreCase))
                continue;

            filtered.Add(line);
        }

        return string.Join(Environment.NewLine, filtered).TrimEnd();
    }

    private sealed class ShortcutRegistry
    {
        public readonly HashSet<ShortcutToken> UsedShortcuts = new();
        public readonly Dictionary<ShortcutToken, string> ShortcutDescriptions = new();
        private bool _seeded;

        public void SeedFromWindow(Window window)
        {
            if (_seeded)
                return;

            _seeded = true;
            foreach (var inputBinding in window.InputBindings)
            {
                if (inputBinding is KeyBinding keyBinding)
                {
                    UsedShortcuts.Add(new ShortcutToken(keyBinding.Key, keyBinding.Modifiers));
                }
            }
        }
    }

    private readonly record struct ShortcutToken(Key Key, ModifierKeys Modifiers);
    private readonly record struct ShortcutEntry(string Gesture, string Description);

    private sealed class InvokeButtonCommand : ICommand
    {
        private readonly WeakReference<Button> _buttonRef;

        public InvokeButtonCommand(Button button)
        {
            _buttonRef = new WeakReference<Button>(button);
        }

        public event System.EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter)
        {
            if (!_buttonRef.TryGetTarget(out var button))
                return false;

            return button.IsEnabled && button.IsVisible;
        }

        public void Execute(object? parameter)
        {
            if (!_buttonRef.TryGetTarget(out var button))
                return;

            if (!button.IsEnabled || !button.IsVisible)
                return;

            if (button.Command != null)
            {
                var commandParameter = button.CommandParameter;
                if (button.Command.CanExecute(commandParameter))
                    button.Command.Execute(commandParameter);
                return;
            }

            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
        }
    }
}
