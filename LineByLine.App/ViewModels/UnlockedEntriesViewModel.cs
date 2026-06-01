using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LineByLine.App.Crypto;
using LineByLine.App.Data;
using LineByLine.App.Services;

namespace LineByLine.App.ViewModels;

public sealed record UnlockedEntryDisplay(string Timestamp, string Plaintext);

public partial class UnlockedEntriesViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;
    private readonly VaultService _vault;
    private readonly EntryRepository _repo;
    private readonly string? _notebookId;
    private readonly string? _notebookName;

    [ObservableProperty]
    private string _statusMessage = "loading...";

    [ObservableProperty]
    private string _contextHeader = "UNLOCKED";

    public ObservableCollection<UnlockedEntryDisplay> Entries { get; } = new();

    public UnlockedEntriesViewModel(MainWindowViewModel main, VaultService vault, string? notebookId, string? notebookName)
    {
        _main = main;
        _vault = vault;
        _repo = new EntryRepository();
        _notebookId = notebookId;
        _notebookName = notebookName;

        ContextHeader = notebookName != null
            ? $"UNLOCKED  ·  {notebookName}"
            : "UNLOCKED";

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var key = _vault.GetActiveKey();
        var entries = await Task.Run(() => _repo.GetUnlockedEntries(_notebookId));

        if (entries.Count == 0)
        {
            StatusMessage = "no unlocked entries";
            return;
        }

        StatusMessage = string.Empty;
        foreach (var e in entries)
        {
            try
            {
                var plaintext = AesGcmCrypto.DecryptString(e.Nonce, e.EncryptedText, key);
                Entries.Add(new UnlockedEntryDisplay(
                    e.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    plaintext));
            }
            catch { }
        }

        if (Entries.Count == 0)
            StatusMessage = "no unlocked entries";
    }

    [RelayCommand]
    public void Back()
    {
        _main.GoToJournal();
    }
}
