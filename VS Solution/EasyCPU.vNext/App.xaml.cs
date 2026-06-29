using System.Linq;
using System.Xml;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
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
        RegisterEasyCpuHighlighting();
        Ambiente.Inizializza();
        ApplyTheme(SettingsViewModel.Instance.Theme);
        var mainViewModel = new MainViewModel(SettingsViewModel.Instance);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow { DataContext = mainViewModel };
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            singleView.MainView = new MainView { DataContext = mainViewModel };

        base.OnFrameworkInitializationCompleted();
    }

    private static void RegisterEasyCpuHighlighting()
    {
        using var stream = typeof(App).Assembly
            .GetManifestResourceStream("EasyCPU.vNext.Resources.EasyCPU.xshd");
        if (stream is null) return;
        using var reader = new XmlTextReader(stream);
        var def = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        HighlightingManager.Instance.RegisterHighlighting("EasyCPU", [".as", ".asj"], def);
    }

    public static void ApplyTheme(AppTheme theme)
    {
        if (Current is null) return;

        var ft = Current.Styles.OfType<FluentTheme>().FirstOrDefault();

        // Clear any previous Blue accent palette
        ft?.Palettes.Remove(ThemeVariant.Dark);

        if (theme == AppTheme.Blue)
        {
            if (ft is not null)
                ft.Palettes[ThemeVariant.Dark] = new ColorPaletteResources { Accent = Color.Parse("#007ACC") };
            // Force variant re-evaluation: if already Dark, a Light→Dark toggle
            // makes Avalonia pick up the new palette (same-variant assignment is a no-op).
            Current.RequestedThemeVariant = ThemeVariant.Light;
            Current.RequestedThemeVariant = ThemeVariant.Dark;
        }
        else if (theme == AppTheme.Dark)
        {
            // Force re-eval in case we're coming from Blue (also Dark base)
            Current.RequestedThemeVariant = ThemeVariant.Light;
            Current.RequestedThemeVariant = ThemeVariant.Dark;
        }
        else
        {
            Current.RequestedThemeVariant = ThemeVariant.Light;
        }
    }
}
