using Avalonia;
using Avalonia.Media;
using LineByLine.App.Data;

namespace LineByLine.App.Services;

public class SettingsService
{
    private const string KeyUnlockDelay = "unlock_delay_seconds";
    private const string KeyEntryLimit = "recent_entry_limit";
    private const string KeyFontSize = "font_size";
    private const string KeyAccentColor = "accent_color";

#if DEBUG
    public const int DefaultUnlockDelaySeconds = 15;
#else
    public const int DefaultUnlockDelaySeconds = 86_400;
#endif
    public const int DefaultEntryLimit = 30;

    private readonly SettingsRepository _repo;

    public SettingsService(SettingsRepository repo) => _repo = repo;

    public int UnlockDelaySeconds
    {
        get => _repo.GetInt(KeyUnlockDelay, DefaultUnlockDelaySeconds);
        set => _repo.SetInt(KeyUnlockDelay, value);
    }

    public int EntryLimit
    {
        get => _repo.GetInt(KeyEntryLimit, DefaultEntryLimit);
        set => _repo.SetInt(KeyEntryLimit, value);
    }

    public string FontSizeOption
    {
        get => _repo.Get(KeyFontSize) ?? "medium";
        set => _repo.Set(KeyFontSize, value);
    }

    public string AccentColorOption
    {
        get => _repo.Get(KeyAccentColor) ?? "blue";
        set => _repo.Set(KeyAccentColor, value);
    }

    // Apply all persisted visual settings — call once after vault unlocks.
    public void ApplyAll()
    {
        ApplyFontSize(FontSizeOption);
        ApplyAccentColor(AccentColorOption);
    }

    public void ApplyFontSize(string option)
    {
        var (base_, title, hint) = option switch
        {
            "small" => (11.0, 14.0, 10.0),
            "large" => (15.0, 19.0, 13.0),
            _ => (13.0, 17.0, 11.0),
        };
        var r = Application.Current!.Resources;
        r["FontSizeBase"] = base_;
        r["FontSizeTitle"] = title;
        r["FontSizeHint"] = hint;
    }

    public void ApplyAccentColor(string option)
    {
        var color = option switch
        {
            "green" => Color.Parse("#44aa66"),
            "amber" => Color.Parse("#aa8833"),
            "red" => Color.Parse("#aa4444"),
            "mono" => Color.Parse("#777777"),
            _ => Color.Parse("#5555aa"),
        };
        Application.Current!.Resources["AccentBrush"] = new SolidColorBrush(color);
    }

    public static string FormatDelay(int seconds) => seconds switch
    {
        15 => "15 seconds",
        60 => "1 minute",
        3_600 => "1 hour",
        86_400 => "1 day",
        604_800 => "1 week",
        2_592_000 => "1 month",
        7_776_000 => "3 months",
        31_536_000 => "1 year",
        _ => $"{seconds}s",
    };

    public static int? ParseDelay(string input) => input.Trim().ToLowerInvariant() switch
    {
        "15s" or "15" => 15,
        "1m" or "1min" => 60,
        "1h" => 3_600,
        "1d" => 86_400,
        "1w" => 604_800,
        "1mo" or "1month" => 2_592_000,
        "3mo" or "3months" or "3m" => 7_776_000,
        "1y" => 31_536_000,
        _ => null,
    };

    public static string? ParseFontSize(string input) => input.Trim().ToLowerInvariant() switch
    {
        "small" or "s" => "small",
        "medium" or "med" or "m" => "medium",
        "large" or "lg" or "l" => "large",
        _ => null,
    };

    public static string? ParseAccentColor(string input) => input.Trim().ToLowerInvariant() switch
    {
        "blue" or "b" => "blue",
        "green" or "g" => "green",
        "amber" or "a" => "amber",
        "red" or "r" => "red",
        "mono" or "white" or "w" => "mono",
        _ => null,
    };
}
