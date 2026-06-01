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
