using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using EasyCPU.vNext.ViewModels;

namespace EasyCPU.vNext;

public class DockFactory : Factory
{
    private readonly MainViewModel _mainVm;

    public DockFactory(MainViewModel mainVm) => _mainVm = mainVm;

    public override IRootDock CreateLayout()
    {
        var codeEditor = new CodeEditorViewModel { Id = "CodeEditor", Title = "Codice" };
        var dataEditor = new DataEditorViewModel { Id = "DataEditor", Title = "Dati" };
        var registers = new RegistersViewModel { Id = "Registers", Title = "Registri" };
        var stack = new StackViewModel { Id = "Stack", Title = "Stack" };
        var memory = new MemoryViewModel { Id = "Memory", Title = "Memoria" };
        var errors = new ErrorsViewModel { Id = "Errors", Title = "Errori" };

        var documentDock = new DocumentDock
        {
            Id = "Documents",
            Title = "Documenti",
            CanCreateDocument = false,
            Proportion = 0.65,
            ActiveDockable = codeEditor,
            VisibleDockables = CreateList<IDockable>(codeEditor, dataEditor),
        };

        var toolDock = new ToolDock
        {
            Id = "Tools",
            Title = "Strumenti",
            Proportion = 0.35,
            ActiveDockable = registers,
            VisibleDockables = CreateList<IDockable>(registers, stack, memory, errors),
        };

        var mainLayout = new ProportionalDock
        {
            Id = "MainLayout",
            Orientation = Orientation.Horizontal,
            ActiveDockable = documentDock,
            VisibleDockables = CreateList<IDockable>(
                documentDock,
                new ProportionalDockSplitter(),
                toolDock),
        };

        var rootDock = CreateRootDock();
        rootDock.Id = "Root";
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

        return rootDock;
    }
}
