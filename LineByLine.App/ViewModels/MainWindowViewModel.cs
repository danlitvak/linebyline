using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
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
        // Note: don't read persisted settings here — the database schema isn't
        // created until the vault is set up or unlocked. Transparency state is
        // synced in GoToJournal, after the settings have been applied.
        _currentScreen = vault.VaultExists()
            ? new LockedScreenViewModel(this, vault)
            : new SetupScreenViewModel(this, vault);
    }

    public void GoToJournal()
    {
        _settings.ApplyAll();
        _isTransparent = _settings.Transparency > 0;
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

    // Fallback level used by the /transparent toggle when no transparency is
    // configured in settings.
    private const int ToggleDefaultTransparency = 40;

    private bool _isTransparent;

    /// <summary>
    /// Quick toggle of a see-through window for note-taking over other windows.
    /// Uses the configured transparency level (or a default), without changing
    /// the saved setting. Returns the new transparency state.
    /// </summary>
    public bool ToggleTransparency()
    {
        _isTransparent = !_isTransparent;
        var level = _isTransparent
            ? (_settings.Transparency > 0 ? _settings.Transparency : ToggleDefaultTransparency)
            : 0;
        _settings.ApplyTransparency(level);
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
