using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LineByLine.App.Crypto;
using LineByLine.App.Data;
using LineByLine.App.Models;
using LineByLine.App.Services;

namespace LineByLine.App.ViewModels;

public sealed record SealedEntryDisplay(string Timestamp, string Preview);

public partial class JournalScreenViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;
    private readonly VaultService _vault;
    private readonly SettingsService _settings;
    private readonly EntryRepository _entryRepo;
    private readonly NotebookRepository _notebookRepo;

    private string _currentNotebookId;
    private string _currentNotebookName;
    private string? _currentPageId;
    private string? _currentPageTitle;

    public string CurrentNotebookId => _currentNotebookId;
    public string CurrentNotebookName => _currentNotebookName;

    // Tab autocomplete state
    private static readonly string[] CommandCompletions =
    {
        "/help",
        "/lock",
        "/unlocked",
        "/settings",
        "/transparent",
        "/notebooks",
        "/pages",
        "/trash",
        "/restore last",
        "/export",
        "/delete last",
        "/use ",
        "/page ",
        "/new notebook ",
        "/new page ",
    };
    private string[] _tabMatches = Array.Empty<string>();
    private int _tabIndex = -1;
    private bool _isTabbing;

    [ObservableProperty]
    private string _draftText = string.Empty;

    [ObservableProperty]
    private string? _commandOutput;

    [ObservableProperty]
    private string? _tabSuggestion;

    [ObservableProperty]
    private string _contextDisplay = string.Empty;

    public ObservableCollection<SealedEntryDisplay> RecentEntries { get; } = new();

    public JournalScreenViewModel(MainWindowViewModel main, VaultService vault, SettingsService settings)
    {
        _main = main;
        _vault = vault;
        _settings = settings;
        _entryRepo = new EntryRepository();
        _notebookRepo = new NotebookRepository();

        var defaultNotebook = _notebookRepo.GetAll().FirstOrDefault(n => n.IsDefault)
            ?? throw new InvalidOperationException("No default notebook found.");

        _currentNotebookId = defaultNotebook.Id;
        _currentNotebookName = defaultNotebook.Name;
        _currentPageId = null;
        _currentPageTitle = null;

        UpdateContextDisplay();
        _ = LoadEntriesAsync();
    }

    partial void OnDraftTextChanged(string value)
    {
        if (_isTabbing) return;
        if (!string.IsNullOrEmpty(value))
            CommandOutput = null;
        // Any manual edit resets the tab cycle
        _tabMatches = Array.Empty<string>();
        _tabIndex = -1;
        UpdateTabSuggestion(value);
    }

    private void UpdateTabSuggestion(string input)
    {
        if (!input.StartsWith('/') || input.Length < 2)
        {
            TabSuggestion = null;
            return;
        }

        var matches = CommandCompletions
            .Where(c => c.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.TrimEnd())
            .ToArray();

        TabSuggestion = matches.Length switch
        {
            0 => null,
            1 => $"tab → {matches[0]}",
            _ => $"tab → {string.Join("   ", matches)}",
        };
    }

    public void HandleTab()
    {
        var input = DraftText;
        if (!input.StartsWith('/')) return;

        // Recompute matches if the current input doesn't equal the last tab result
        if (_tabMatches.Length == 0 || _tabIndex < 0 || _tabMatches[_tabIndex] != input)
        {
            _tabMatches = CommandCompletions
                .Where(c => c.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            _tabIndex = -1;
        }

        if (_tabMatches.Length == 0) return;

        _tabIndex = (_tabIndex + 1) % _tabMatches.Length;
        _isTabbing = true;
        DraftText = _tabMatches[_tabIndex];
        _isTabbing = false;

        // Update hint to show cycle position
        UpdateTabSuggestion(_tabMatches[_tabIndex]);
    }

    private void UpdateContextDisplay()
    {
        var context = _currentPageTitle is null
            ? $"notebook: {_currentNotebookName}"
            : $"notebook: {_currentNotebookName} / page: {_currentPageTitle}";
        ContextDisplay = context;
        _ = RefreshEntryCountAsync();
    }

    private async Task RefreshEntryCountAsync()
    {
        var nbId = _currentNotebookId;
        var pgId = _currentPageId;
        var count = await Task.Run(() => _entryRepo.GetEntryCount(nbId, pgId));
        var context = _currentPageTitle is null
            ? $"notebook: {_currentNotebookName}"
            : $"notebook: {_currentNotebookName} / page: {_currentPageTitle}";
        ContextDisplay = $"{context}  ·  {count}";
    }

    private async Task LoadEntriesAsync()
    {
        var notebookId = _currentNotebookId;
        var pageId = _currentPageId;

        var entries = await Task.Run(() => _entryRepo.GetRecentEntries(notebookId, pageId));
        RecentEntries.Clear();
        foreach (var e in entries)
            RecentEntries.Add(ToDisplay(e));
    }

    [RelayCommand]
    public async Task SubmitAsync()
    {
        var plaintext = DraftText;
        if (string.IsNullOrWhiteSpace(plaintext))
            return;

        DraftText = string.Empty;

        var trimmed = plaintext.Trim();
        if (trimmed.StartsWith('/'))
        {
            await HandleCommandAsync(trimmed);
            return;
        }

        await SealEntryAsync(plaintext);
    }

    private async Task HandleCommandAsync(string input)
    {
        var parts = input.Split(' ', 2, StringSplitOptions.TrimEntries);
        var cmd = parts[0].ToLowerInvariant();
        var arg = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        switch (cmd)
        {
            case "/unlocked":
                _main.GoToUnlocked();
                break;

            case "/lock":
                _main.Lock();
                break;

            case "/notebooks":
                ShowNotebooks();
                break;

            case "/pages":
                ShowPages();
                break;

            case "/use":
                await UseAsync(arg);
                break;

            case "/page":
                await SwitchPageAsync(arg);
                break;

            case "/new":
                await NewAsync(arg);
                break;

            case "/delete":
                if (arg.Equals("last", StringComparison.OrdinalIgnoreCase))
                    await DeleteLastAsync();
                else
                    CommandOutput = "usage: /delete last";
                break;

            case "/trash":
                await ShowTrashAsync();
                break;

            case "/restore":
                if (arg.Equals("last", StringComparison.OrdinalIgnoreCase))
                    await RestoreLastAsync();
                else
                    CommandOutput = "usage: /restore last";
                break;

            case "/export":
                await ExportAsync();
                break;

            case "/settings":
                _main.GoToSettings();
                break;

            case "/transparent":
                CommandOutput = _main.ToggleTransparency()
                    ? "transparent overlay on - the window now shows through"
                    : "transparent overlay off";
                break;

            case "/help":
                CommandOutput =
                    "/notebooks              list all notebooks\n" +
                    "/use <notebook>         switch to a notebook\n" +
                    "/new notebook <name>    create a new notebook\n" +
                    "/pages                  list pages in current notebook\n" +
                    "/page <title>           switch to a page\n" +
                    "/new page <title>       create a new page\n" +
                    "/unlocked               read unlocked entries\n" +
                    "/trash                  list soft-deleted entries\n" +
                    "/restore last           restore the last deleted entry\n" +
                    "/export                 export unlocked entries to a file\n" +
                    "/delete last            delete the most recent entry\n" +
                    "/settings               open settings\n" +
                    "/transparent            toggle a see-through overlay window\n" +
                    "/lock                   lock the vault";
                break;

            default:
                CommandOutput = $"unknown command: {parts[0]}";
                break;
        }
    }

    private void ShowNotebooks()
    {
        var notebooks = _notebookRepo.GetAll();
        var sb = new StringBuilder();
        foreach (var nb in notebooks)
        {
            var current = nb.Id == _currentNotebookId ? "> " : "  ";
            var label = nb.IsDefault ? $"{nb.Name} (default)" : nb.Name;
            sb.AppendLine($"{current}{label}");
        }
        CommandOutput = sb.ToString().TrimEnd();
    }

    private void ShowPages()
    {
        var pages = _notebookRepo.GetPages(_currentNotebookId);
        if (pages.Count == 0)
        {
            CommandOutput = $"no pages in {_currentNotebookName}";
            return;
        }

        var sb = new StringBuilder();
        foreach (var p in pages)
        {
            var current = p.Id == _currentPageId ? "> " : "  ";
            sb.AppendLine($"{current}{p.Title}");
        }
        CommandOutput = sb.ToString().TrimEnd();
    }

    private async Task UseAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            CommandOutput = "usage: /use <notebook name>";
            return;
        }

        var notebook = await Task.Run(() => _notebookRepo.GetByName(name));
        if (notebook is null)
        {
            CommandOutput = $"notebook not found: {name}";
            return;
        }

        _currentNotebookId = notebook.Id;
        _currentNotebookName = notebook.Name;
        _currentPageId = null;
        _currentPageTitle = null;
        UpdateContextDisplay();
        await LoadEntriesAsync();
    }

    private async Task SwitchPageAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            CommandOutput = "usage: /page <title>";
            return;
        }

        var page = await Task.Run(() => _notebookRepo.GetPageByTitle(_currentNotebookId, title));
        if (page is null)
        {
            CommandOutput = $"page not found: {title}";
            return;
        }

        _currentPageId = page.Id;
        _currentPageTitle = page.Title;
        UpdateContextDisplay();
        await LoadEntriesAsync();
    }

    private async Task NewAsync(string arg)
    {
        var parts = arg.Split(' ', 2, StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            CommandOutput = "usage: /new notebook <name>  or  /new page <title>";
            return;
        }

        var type = parts[0].ToLowerInvariant();
        var namePart = parts[1];

        if (type == "notebook")
        {
            var existing = await Task.Run(() => _notebookRepo.GetByName(namePart));
            if (existing is not null)
            {
                CommandOutput = $"notebook already exists: {namePart}";
                return;
            }

            var notebook = await Task.Run(() => _notebookRepo.Create(namePart));
            _currentNotebookId = notebook.Id;
            _currentNotebookName = notebook.Name;
            _currentPageId = null;
            _currentPageTitle = null;
            UpdateContextDisplay();
            await LoadEntriesAsync();
        }
        else if (type == "page")
        {
            var existing = await Task.Run(() => _notebookRepo.GetPageByTitle(_currentNotebookId, namePart));
            if (existing is not null)
            {
                CommandOutput = $"page already exists: {namePart}";
                return;
            }

            var page = await Task.Run(() => _notebookRepo.CreatePage(_currentNotebookId, namePart));
            _currentPageId = page.Id;
            _currentPageTitle = page.Title;
            UpdateContextDisplay();
            await LoadEntriesAsync();
        }
        else
        {
            CommandOutput = "usage: /new notebook <name>  or  /new page <title>";
        }
    }

    private async Task DeleteLastAsync()
    {
        var notebookId = _currentNotebookId;
        var pageId = _currentPageId;
        var deletedId = await Task.Run(() => _entryRepo.DeleteLast(notebookId, pageId));

        if (deletedId is null)
        {
            CommandOutput = "nothing to delete";
            return;
        }

        if (RecentEntries.Count > 0)
            RecentEntries.RemoveAt(RecentEntries.Count - 1);
        _ = RefreshEntryCountAsync();
    }

    private async Task ShowTrashAsync()
    {
        var deleted = await Task.Run(() => _entryRepo.GetDeletedEntries(_currentNotebookId, _currentPageId));
        if (deleted.Count == 0)
        {
            CommandOutput = "trash is empty";
            return;
        }
        var sb = new StringBuilder($"{deleted.Count} deleted entr{(deleted.Count == 1 ? "y" : "ies")}:\n\n");
        foreach (var e in deleted)
            sb.AppendLine($"  [{e.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}]  \"{e.SealedPreview[..Math.Min(40, e.SealedPreview.Length)]}...\"");
        sb.Append("\n/restore last  to recover the most recent");
        CommandOutput = sb.ToString();
    }

    private async Task RestoreLastAsync()
    {
        var restoredId = await Task.Run(() => _entryRepo.RestoreLast(_currentNotebookId, _currentPageId));
        if (restoredId is null)
        {
            CommandOutput = "nothing to restore";
            return;
        }
        // Reload the full list so the restored entry appears in correct order
        await LoadEntriesAsync();
        CommandOutput = "entry restored";
    }

    private async Task ExportAsync()
    {
        var nbId = _currentNotebookId;
        var key = _vault.GetActiveKey();
        var entries = await Task.Run(() => _entryRepo.GetUnlockedEntries(nbId));

        if (entries.Count == 0)
        {
            CommandOutput = "no unlocked entries to export";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("LINE BY LINE — EXPORT");
        sb.AppendLine($"Notebook: {_currentNotebookName}");
        sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine(new string('-', 40));

        foreach (var e in entries)
        {
            try
            {
                var plaintext = AesGcmCrypto.DecryptString(e.Nonce, e.EncryptedText, key);
                sb.AppendLine();
                sb.AppendLine($"[{e.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}]");
                sb.AppendLine(plaintext);
                sb.AppendLine(new string('-', 40));
            }
            catch { }
        }

        var safeName = string.Concat(_currentNotebookName.Split(Path.GetInvalidFileNameChars()));
        var filename = $"LineByLine_{safeName}_{DateTime.Now:yyyy-MM-dd}.txt";
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            filename);

        await Task.Run(() => File.WriteAllText(path, sb.ToString()));
        CommandOutput = $"exported {entries.Count} entr{(entries.Count == 1 ? "y" : "ies")} to:\n{path}";
    }

    public async Task SaveInterruptedAsync()
    {
        var plaintext = DraftText.Trim();
        if (string.IsNullOrEmpty(plaintext)) return;

        DraftText = string.Empty;
        await SealEntryAsync(plaintext + " [interrupted]");
    }

    private async Task SealEntryAsync(string plaintext)
    {
        var entryId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.Now;
        var unlockAt = now.AddSeconds(_settings.UnlockDelaySeconds);
        var sealedPreview = SealedPreviewGenerator.Generate(plaintext, entryId, SealedPreviewGenerator.CurrentVersion);
        var key = _vault.GetActiveKey();
        var (nonce, ciphertextWithTag) = AesGcmCrypto.EncryptString(plaintext, key);

        var entry = new Entry
        {
            Id = entryId,
            NotebookId = _currentNotebookId,
            PageId = _currentPageId,
            CreatedAt = now,
            UnlockAt = unlockAt,
            EncryptedText = ciphertextWithTag,
            Nonce = nonce,
            SealedPreview = sealedPreview,
            PreviewVersion = SealedPreviewGenerator.CurrentVersion,
            IsDeleted = false,
        };

        await Task.Run(() => _entryRepo.InsertEntry(entry));
        RecentEntries.Add(ToDisplay(entry));
        _ = RefreshEntryCountAsync();
    }

    [RelayCommand]
    public void Lock()
    {
        DraftText = string.Empty;
        _main.Lock();
    }

    [RelayCommand]
    public void ClearDraft()
    {
        DraftText = string.Empty;
    }

    private static SealedEntryDisplay ToDisplay(Entry entry) =>
        new($"[{entry.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}]",
            $"\"{entry.SealedPreview}\"");
}
