using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using LineByLine.App.Data;
using LineByLine.App.Services;
using LineByLine.App.ViewModels;
using LineByLine.App.Views;

namespace LineByLine.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vault = new VaultService();
            var settings = new SettingsService(new SettingsRepository());
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(vault, settings),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
