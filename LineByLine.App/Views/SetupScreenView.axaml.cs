using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using LineByLine.App.ViewModels;

namespace LineByLine.App.Views;

public partial class SetupScreenView : UserControl
{
    public SetupScreenView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        PassphraseInput.KeyDown += OnPassphraseKeyDown;
        ConfirmInput.KeyDown += OnConfirmKeyDown;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        PassphraseInput.Focus();
    }

    private void OnPassphraseKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return)
        {
            ConfirmInput.Focus();
            e.Handled = true;
        }
    }

    private void OnConfirmKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && DataContext is SetupScreenViewModel vm)
        {
            vm.CreateCommand.Execute(null);
            e.Handled = true;
        }
    }
}
