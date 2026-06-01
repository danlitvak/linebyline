using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LineByLine.App.ViewModels;

namespace LineByLine.App.Views;

public partial class JournalScreenView : UserControl
{
    public JournalScreenView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        InputBox.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        InputBox.Focus();

        if (DataContext is JournalScreenViewModel vm)
            vm.RecentEntries.CollectionChanged += OnEntriesChanged;

        ScrollToBottom();
    }

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
            ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        Dispatcher.UIThread.Post(
            () => EntriesScrollViewer.Offset = new Vector(0, double.MaxValue),
            DispatcherPriority.Background);
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not JournalScreenViewModel vm)
            return;

        switch (e.Key)
        {
            case Key.Return when e.KeyModifiers == KeyModifiers.None:
                vm.SubmitCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Escape:
                vm.ClearDraftCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Tab:
                vm.HandleTab();
                e.Handled = true;
                // Restore focus and caret — in case Avalonia's focus system still moved focus away
                Dispatcher.UIThread.Post(() =>
                {
                    InputBox.Focus();
                    InputBox.CaretIndex = InputBox.Text?.Length ?? 0;
                }, DispatcherPriority.Input);
                break;

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
