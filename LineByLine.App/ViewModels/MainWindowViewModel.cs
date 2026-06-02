using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LineByLine.App.Services;
using System.Threading.Tasks;

namespace LineByLine.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly VaultService _vault;
    private readonly SettingsService _settings;

    [ObservableProperty]
    private ViewModelBase _currentScreen;

    public MainWindowViewModel(VaultService vault, SettingsService settings)
    {
        _vault = vault;
        _settings = settings;
        _currentScreen = vault.VaultExists()
            ? new LockedScreenViewModel(this, vault)
            : new SetupScreenViewModel(this, vault);
    }

    public void GoToJournal()
    {
        _settings.ApplyAll();
        CurrentScreen = new JournalScreenViewModel(this, _vault, _settings);
    }

    public void GoToUnlocked()
    {
        string? notebookId = null, notebookName = null;
        if (CurrentScreen is JournalScreenViewModel j)
        {
            notebookId = j.CurrentNotebookId;
            notebookName = j.CurrentNotebookName;
        }
        CurrentScreen = new UnlockedEntriesViewModel(this, _vault, notebookId, notebookName);
    }

    public void GoToSettings()
    {
        CurrentScreen = new SettingsScreenViewModel(this, _settings);
    }

    [RelayCommand]
    public void Lock()
    {
        _vault.Lock();
        CurrentScreen = new LockedScreenViewModel(this, _vault);
    }

    private static readonly Color OpaqueBackground = Color.FromRgb(0x0d, 0x0d, 0x0d);
    // Dark tint kept over the transparent window so light text stays legible
    // against whatever is showing behind it.
    private static readonly Color GlassBackground = Color.FromArgb(0x66, 0x0d, 0x0d, 0x0d);

    private bool _isTransparent;

    /// <summary>
    /// Toggles a see-through window so the app can sit over other windows as
    /// a note-taking overlay. Returns the new transparency state.
    /// </summary>
    public bool ToggleTransparency()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is not { } window)
            return _isTransparent;

        _isTransparent = !_isTransparent;

        if (_isTransparent)
        {
            // Prefer real transparency; fall back to a blur if the OS/compositor
            // doesn't support a fully transparent client area.
            window.TransparencyLevelHint = new[]
            {
                WindowTransparencyLevel.Transparent,
                WindowTransparencyLevel.AcrylicBlur,
            };
            window.Background = new SolidColorBrush(GlassBackground);
        }
        else
        {
            window.TransparencyLevelHint = new[] { WindowTransparencyLevel.None };
            window.Background = new SolidColorBrush(OpaqueBackground);
        }

        return _isTransparent;
    }

    [RelayCommand]
    public async Task EmergencyCloseAsync()
    {
        if (CurrentScreen is JournalScreenViewModel journal)
            await journal.SaveInterruptedAsync();

        _vault.Lock();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
