using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EasyCPU.vNext.Views;

public partial class DataEditorView : UserControl
{
    public DataEditorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
