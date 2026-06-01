using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LineByLine.App.Services;

namespace LineByLine.App.ViewModels;

public partial class LockedScreenViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;
    private readonly VaultService _vault;

    [ObservableProperty]
    private string _passphrase = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isUnlocking;

    public LockedScreenViewModel(MainWindowViewModel main, VaultService vault)
    {
        _main = main;
        _vault = vault;
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task UnlockAsync()
    {
        if (string.IsNullOrWhiteSpace(Passphrase) || IsUnlocking)
            return;

        IsUnlocking = true;
        ErrorMessage = null;

        var passphrase = Passphrase;
        Passphrase = string.Empty;

        // Key derivation is intentionally slow; run off the UI thread
        var success = await System.Threading.Tasks.Task.Run(() => _vault.Unlock(passphrase));

        IsUnlocking = false;

        if (success)
            _main.GoToJournal();
        else
            ErrorMessage = "incorrect passphrase";
    }
}
