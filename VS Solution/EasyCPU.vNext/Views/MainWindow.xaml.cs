using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EasyCPU.vNext.ViewModels;
using EasyCPU.vNext.Views;

namespace EasyCPU.vNext;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.AttachDevTools();
        MainView = this.FindControl<MainView>("MainViewControl")
                   ?? throw new InvalidOperationException("MainView not found.");
    }

    public MainView MainView { get; }

    public MainWindowViewModel ViewModel => MainView.ViewModel;

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
