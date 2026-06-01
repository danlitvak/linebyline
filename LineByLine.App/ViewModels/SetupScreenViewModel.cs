using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LineByLine.App.Services;

namespace LineByLine.App.ViewModels;

public partial class SetupScreenViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;
    private readonly VaultService _vault;

    [ObservableProperty]
    private string _passphrase = string.Empty;

    [ObservableProperty]
    private string _confirmPassphrase = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isCreating;

    public SetupScreenViewModel(MainWindowViewModel main, VaultService vault)
    {
        _main = main;
        _vault = vault;
    }

    [RelayCommand]
    public async System.Threading.Tasks.Task CreateAsync()
    {
        if (IsCreating) return;

        if (string.IsNullOrWhiteSpace(Passphrase))
        {
            ErrorMessage = "passphrase cannot be empty";
            return;
        }

        if (Passphrase != ConfirmPassphrase)
        {
            ErrorMessage = "passphrases do not match";
            ConfirmPassphrase = string.Empty;
            return;
        }

        IsCreating = true;
        ErrorMessage = null;

        var passphrase = Passphrase;
        Passphrase = string.Empty;
        ConfirmPassphrase = string.Empty;

        await System.Threading.Tasks.Task.Run(() => _vault.CreateVault(passphrase));

        IsCreating = false;
        _main.GoToJournal();
    }
}
