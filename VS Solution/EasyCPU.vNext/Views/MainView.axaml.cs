using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EasyCPU.vNext.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
