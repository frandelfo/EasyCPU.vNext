using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EasyCPU.vNext.Views;

public partial class MainEditorView : UserControl
{
    public MainEditorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
