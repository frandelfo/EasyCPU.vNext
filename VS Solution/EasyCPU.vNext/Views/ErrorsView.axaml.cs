using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EasyCPU.vNext.Views;

public partial class ErrorsView : UserControl
{
    public ErrorsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
