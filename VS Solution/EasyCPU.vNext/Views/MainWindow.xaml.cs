using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EasyCPU.vNext;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // On macOS the NativeMenu already appears in the system menu bar
        if (OperatingSystem.IsMacOS())
            this.FindControl<Menu>("MainMenu")!.IsVisible = false;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
