#nullable enable
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using EasyCPU.vNext.ViewModels;

namespace EasyCPU.vNext.Views;

public partial class ErrorsView : UserControl
{
    public ErrorsView()
    {
        InitializeComponent();
        this.FindControl<DataGrid>("ErrorsGrid")
            ?.AddHandler(InputElement.PointerPressedEvent, OnGridPointerPressed,
                         Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount != 2) return;
        if (DataContext is not ErrorsViewModel vm) return;
        if (this.FindControl<DataGrid>("ErrorsGrid") is not { } grid) return;
        if (grid.SelectedItem is not CompilerErrorAdapter item) return;
        vm.MainVm.NavigateToError(item.Source);
    }
}
