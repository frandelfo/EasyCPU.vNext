using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EasyCpu.Common;
using EasyCPU.vNext.ViewModels;
using EasyCPU.vNext.Views;

namespace EasyCPU.vNext;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Ambiente.Inizializza();
        var mainViewModel = new MainViewModel(SettingsViewModel.Instance);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow { DataContext = mainViewModel };
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            singleView.MainView = new MainView { DataContext = mainViewModel };

        base.OnFrameworkInitializationCompleted();
    }
}
