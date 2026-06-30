#nullable enable
using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using Avalonia.Media;
using AvaloniaEdit.Highlighting;
using EasyCpu.Common;
using EasyCPU.vNext.ViewModels;

namespace EasyCPU.vNext.Views;

public partial class DataEditorView : UserControl
{
    private TextEditor? _editor;

    public DataEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _editor = this.FindControl<TextEditor>("Editor");
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_editor == null) return;
        if (DataContext is DataEditorViewModel vm)
            SetupEditor(vm);
    }

    private void SetupEditor(DataEditorViewModel vm)
    {
        if (_editor == null) return;

        _editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("EasyCPU");

        var settings = SettingsViewModel.Instance;
        ApplyFont(_editor, settings);
        settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SettingsViewModel.FontEditorNome)
                               or nameof(SettingsViewModel.FontEditorSize)
                               or nameof(SettingsViewModel.FontEditorStyle))
                ApplyFont(_editor, settings);
        };

        if (!string.IsNullOrEmpty(vm.SourceText))
            _editor.Document.Text = vm.SourceText;

        _editor.Document.Changed += (_, _) =>
            vm.SourceText = _editor.Document.Text;

        _editor.TextArea.TextEntering += OnTextEntering;

        vm.SetSourceTextAction = text =>
        {
            _editor.Document.Text = text;
            _editor.TextArea.Caret.Line = 1;
            _editor.TextArea.Caret.Column = 1;
        };

        vm.NavigateToLineAction = lineNumber =>
        {
            if (_editor.Document.LineCount == 0) return;
            int n = Math.Clamp(lineNumber, 1, _editor.Document.LineCount);
            _editor.ScrollTo(n, 1);
            var docLine = _editor.Document.GetLineByNumber(n);
            var text    = _editor.Document.GetText(docLine.Offset, docLine.Length);
            int leading = text.Length - text.TrimStart().Length;
            _editor.TextArea.Caret.Line   = n;
            _editor.TextArea.Caret.Column = leading < text.Length ? leading + 1 : 1;
            Avalonia.Threading.Dispatcher.UIThread.Post(() => _editor.TextArea.Focus());
        };
    }

    private static void ApplyFont(TextEditor editor, SettingsViewModel s)
    {
        if (!string.IsNullOrWhiteSpace(s.FontEditorNome))
            editor.FontFamily = new FontFamily(s.FontEditorNome);
        if (s.FontEditorSize > 0)
            editor.FontSize = s.FontEditorSize;
        editor.FontWeight = (s.FontEditorStyle & 1) != 0 ? FontWeight.Bold  : FontWeight.Normal;
        editor.FontStyle  = (s.FontEditorStyle & 2) != 0 ? FontStyle.Italic : FontStyle.Normal;
    }

    private void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (_editor == null) return;
        int margin = Ambiente.MargineSinistro;

        if (e.Text == "\t")
        {
            e.Handled = true;
            int col = _editor.TextArea.Caret.Column - 1;
            int spaces = margin - (col % margin);
            _editor.TextArea.Document.Insert(
                _editor.TextArea.Caret.Offset,
                new string(' ', spaces));
        }
        else if (e.Text == "\n")
        {
            e.Handled = true;
            _editor.TextArea.Document.Insert(
                _editor.TextArea.Caret.Offset,
                Environment.NewLine + new string(' ', margin));
        }
    }
}
