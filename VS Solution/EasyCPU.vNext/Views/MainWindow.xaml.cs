using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EasyCPU.vNext.ViewModels;

namespace EasyCPU.vNext;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        if (OperatingSystem.IsMacOS())
        {
            this.FindControl<Menu>("MainMenu")!.IsVisible = false;
            DataContextChanged += OnDataContextChanged;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += OnVmPropertyChanged;
            UpdateNativeCheckmarks(vm);
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsCodeEditorVisible)
                           or nameof(MainViewModel.IsDataEditorVisible)
                           or nameof(MainViewModel.IsRegistersVisible)
                           or nameof(MainViewModel.IsStackVisible)
                           or nameof(MainViewModel.IsMemoryVisible)
                           or nameof(MainViewModel.IsErrorsVisible)
            && sender is MainViewModel vm)
        {
            UpdateNativeCheckmarks(vm);
        }
    }

    private void UpdateNativeCheckmarks(MainViewModel vm)
    {
        var nativeMenu = NativeMenu.GetMenu(this);
        if (nativeMenu is null || nativeMenu.Items.Count < 4) return;
        // Root order: File(0), Modifica(1), Esegui(2), Finestre(3), Strumenti(4)
        if (nativeMenu.Items[3] is not NativeMenuItem finestre) return;
        var items = finestre.Menu?.Items;
        if (items is null || items.Count < 6) return;
        SetCheck(items[0], vm.IsCodeEditorVisible);
        SetCheck(items[1], vm.IsDataEditorVisible);
        SetCheck(items[2], vm.IsRegistersVisible);
        SetCheck(items[3], vm.IsStackVisible);
        SetCheck(items[4], vm.IsMemoryVisible);
        SetCheck(items[5], vm.IsErrorsVisible);
    }

    private static void SetCheck(NativeMenuItemBase item, bool value)
    {
        if (item is NativeMenuItem mi)
        {
            mi.ToggleType = MenuItemToggleType.CheckBox;
            mi.IsChecked = value;
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
