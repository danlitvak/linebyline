using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LineByLine.App.Views;

public partial class LockedScreenView : UserControl
{
    public LockedScreenView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        PassphraseInput.Focus();
    }
}
