using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LineByLine.App.Services;

namespace LineByLine.App.ViewModels;

public partial class SettingsScreenViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;
    private readonly SettingsService _settings;

    private static readonly string[] SettingCompletions = { "delay ", "limit ", "size ", "color " };
    private string[] _tabMatches = Array.Empty<string>();
    private int _tabIndex = -1;

    [ObservableProperty] private string _unlockDelayDisplay = string.Empty;
    [ObservableProperty] private string _entryLimitDisplay = string.Empty;
    [ObservableProperty] private string _fontSizeDisplay = string.Empty;
    [ObservableProperty] private string _accentColorDisplay = string.Empty;
    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string? _feedback;

    public SettingsScreenViewModel(MainWindowViewModel main, SettingsService settings)
    {
        _main = main;
        _settings = settings;
        Refresh();
    }

    private void Refresh()
    {
        UnlockDelayDisplay = SettingsService.FormatDelay(_settings.UnlockDelaySeconds);
        EntryLimitDisplay = _settings.EntryLimit.ToString();
        FontSizeDisplay = _settings.FontSizeOption;
        AccentColorDisplay = _settings.AccentColorOption;
        Feedback = null;
    }

    [RelayCommand]
    public void Apply()
    {
        var input = InputText.Trim();
        InputText = string.Empty;

        if (string.IsNullOrWhiteSpace(input)) { _main.GoToJournal(); return; }

        var parts = input.Split(' ', 2);
        var cmd = parts[0].ToLowerInvariant();
        var arg = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        switch (cmd)
        {
            case "delay":
                var seconds = SettingsService.ParseDelay(arg);
                if (seconds is null)
                    Feedback = $"unknown delay: {arg}   options: 15s 1h 1d 1w 1mo 3mo 1y";
                else
                {
                    _settings.UnlockDelaySeconds = seconds.Value;
                    Refresh();
                    Feedback = $"unlock delay set to {SettingsService.FormatDelay(seconds.Value)}";
                }
                break;

            case "limit":
                if (int.TryParse(arg, out var limit) && limit > 0)
                {
                    _settings.EntryLimit = limit;
                    Refresh();
                    Feedback = $"entry limit set to {limit}";
                }
                else Feedback = $"invalid limit: {arg}   enter a positive number";
                break;

            case "size":
                var sizeOpt = SettingsService.ParseFontSize(arg);
                if (sizeOpt is null)
                    Feedback = $"unknown size: {arg}   options: small  medium  large";
                else
                {
                    _settings.FontSizeOption = sizeOpt;
                    _settings.ApplyFontSize(sizeOpt);
                    Refresh();
                    Feedback = $"font size set to {sizeOpt}";
                }
                break;

            case "color":
                var colorOpt = SettingsService.ParseAccentColor(arg);
                if (colorOpt is null)
                    Feedback = $"unknown color: {arg}   options: blue  green  amber  red  mono";
                else
                {
                    _settings.AccentColorOption = colorOpt;
                    _settings.ApplyAccentColor(colorOpt);
                    Refresh();
                    Feedback = $"accent color set to {colorOpt}";
                }
                break;

            default:
                Feedback = $"unknown setting: {cmd}   try delay / limit / size / color";
                break;
        }
    }

    public void HandleTab()
    {
        var input = InputText;
        if (string.IsNullOrEmpty(input)) return;

        if (_tabMatches.Length == 0 || _tabIndex < 0 || _tabMatches[_tabIndex] != input)
        {
            _tabMatches = SettingCompletions
                .Where(c => c.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            _tabIndex = -1;
        }

        if (_tabMatches.Length == 0) return;

        _tabIndex = (_tabIndex + 1) % _tabMatches.Length;
        InputText = _tabMatches[_tabIndex];
    }

    [RelayCommand]
    public void Back() => _main.GoToJournal();
}
