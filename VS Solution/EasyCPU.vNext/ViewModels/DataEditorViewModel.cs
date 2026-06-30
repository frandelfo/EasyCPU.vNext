using System;
using Dock.Model.Mvvm.Controls;

namespace EasyCPU.vNext.ViewModels;

public partial class DataEditorViewModel : Document
{
    public string SourceText { get; set; } = "";
    internal Action<string>? SetSourceTextAction;
    internal Action<int>? NavigateToLineAction;
}
