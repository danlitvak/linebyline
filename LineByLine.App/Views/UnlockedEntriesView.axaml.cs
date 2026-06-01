using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace LineByLine.App.Views;

public partial class UnlockedEntriesView : UserControl
{
    public UnlockedEntriesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        EntriesScrollViewer.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => EntriesScrollViewer.Focus(), DispatcherPriority.Input);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.PageUp:
                EntriesScrollViewer.Offset = new Vector(
                    0,
                    Math.Max(0, EntriesScrollViewer.Offset.Y - EntriesScrollViewer.Bounds.Height));
                e.Handled = true;
                break;

            case Key.PageDown:
                EntriesScrollViewer.Offset = new Vector(
                    0,
                    EntriesScrollViewer.Offset.Y + EntriesScrollViewer.Bounds.Height);
                e.Handled = true;
                break;
        }
    }
}
