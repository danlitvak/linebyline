using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using LineByLine.App.ViewModels;

namespace LineByLine.App.Views;

public partial class MainWindow : Window
{
    private bool _shortcutsBound;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_shortcutsBound || DataContext is not MainWindowViewModel vm)
            return;

        // Ctrl on Windows/Linux, Cmd on macOS — sourced from the platform itself.
        var command = PlatformSettings?.HotkeyConfiguration.CommandModifiers
                      ?? KeyModifiers.Control;

        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.L, command),
            Command = vm.LockCommand
        });
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.W, command),
            Command = vm.EmergencyCloseCommand
        });

        _shortcutsBound = true;
    }
}
