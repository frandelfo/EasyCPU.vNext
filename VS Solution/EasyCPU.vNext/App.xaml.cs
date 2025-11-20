using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data.Core.Plugins;
using Avalonia.Styling;
using EasyCPU.vNext.ViewModels;
using EasyCPU.vNext.Views;

namespace EasyCPU.vNext
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                var window = new MainWindow();
                desktopLifetime.MainWindow = window;
                HookThemeVariant(window.ViewModel);
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewLifetime)
            {
                DisableAvaloniaDataAnnotationValidation();
                var mainView = new MainView();
                singleViewLifetime.MainView = mainView;
                HookThemeVariant(mainView.ViewModel);
            }
            base.OnFrameworkInitializationCompleted();     
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }

        private void HookThemeVariant(MainWindowViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            viewModel.PropertyChanged += MainWindowViewModelOnPropertyChanged;
            UpdateRequestedTheme(viewModel.SelectedTheme);
        }

        private void MainWindowViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not MainWindowViewModel mainWindowViewModel) return;
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedTheme))
            {
                UpdateRequestedTheme(mainWindowViewModel.SelectedTheme);
            }
        }

        private void UpdateRequestedTheme(ThemeViewModel selectedTheme)
        {
            if (selectedTheme == null)
            {
                return;
            }

            RequestedThemeVariant = selectedTheme.ThemeName.ToString().ToLower().Contains("light")
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
        }
    }
}
