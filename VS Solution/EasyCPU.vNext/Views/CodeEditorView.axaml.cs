using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EasyCPU.vNext.Views;

public partial class CodeEditorView : UserControl
{
    public CodeEditorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
