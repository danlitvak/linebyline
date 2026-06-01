using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LineByLine.App.ViewModels;

namespace LineByLine.App.Views;

public partial class SettingsScreenView : UserControl
{
    public SettingsScreenView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SettingInput.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e) => SettingInput.Focus();

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not SettingsScreenViewModel vm) return;

        switch (e.Key)
        {
            case Key.Return when e.KeyModifiers == KeyModifiers.None:
                vm.ApplyCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Tab:
                vm.HandleTab();
                Dispatcher.UIThread.Post(
                    () => SettingInput.CaretIndex = SettingInput.Text?.Length ?? 0,
                    DispatcherPriority.Input);
                e.Handled = true;
                break;
        }
    }
}
