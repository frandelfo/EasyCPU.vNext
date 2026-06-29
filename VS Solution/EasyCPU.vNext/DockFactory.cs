#nullable enable
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using EasyCPU.vNext.ViewModels;

namespace EasyCPU.vNext;

public class DockFactory : Factory
{
    private readonly MainViewModel _mainVm;

    public CodeEditorViewModel? CodeEditor { get; private set; }
    public DataEditorViewModel? DataEditor  { get; private set; }
    public RegistersViewModel?  Registers   { get; private set; }
    public StackViewModel?      Stack       { get; private set; }
    public MemoryViewModel?     Memory      { get; private set; }
    public ErrorsViewModel?     Errors      { get; private set; }

    // Container refs — used by panel visibility management
    internal IDock? DocumentContainer { get; private set; }
    internal IDock? ToolContainer     { get; private set; }

    public DockFactory(MainViewModel mainVm) => _mainVm = mainVm;

    // Returns the container that owns the given panel
    internal IDock? ContainerFor(IDockable? d) =>
        d == CodeEditor || d == DataEditor ? DocumentContainer : ToolContainer;

    // Visibility check: is the panel currently in its container's VisibleDockables?
    internal bool IsPanelVisible(IDockable? d) =>
        d is not null && ContainerFor(d)?.VisibleDockables?.Contains(d) == true;

    // Override CloseDockable (X button) to use RemoveDockable with collapse=false.
    // collapse=false prevents the container from being removed from the tree when emptied,
    // which would break AddDockable when trying to show the panel again.
    public override void CloseDockable(IDockable dockable)
    {
        RemoveDockable(dockable, collapse: false);
        _mainVm.OnPanelVisibilityChanged();
    }

    public override IRootDock CreateLayout()
    {
        CodeEditor = new CodeEditorViewModel(_mainVm) { Id = "CodeEditor", Title = "Codice" };
        DataEditor = new DataEditorViewModel { Id = "DataEditor", Title = "Dati" };
        Registers  = new RegistersViewModel  { Id = "Registers",  Title = "Registri" };
        Stack      = new StackViewModel      { Id = "Stack",      Title = "Stack" };
        Memory     = new MemoryViewModel     { Id = "Memory",     Title = "Memoria" };
        Errors     = new ErrorsViewModel     { Id = "Errors",     Title = "Errori" };

        var documentDock = new DocumentDock
        {
            Id = "Documents",
            Title = "Documenti",
            CanCreateDocument = false,
            Proportion = 0.65,
            ActiveDockable = CodeEditor,
            VisibleDockables = CreateList<IDockable>(CodeEditor, DataEditor),
        };

        var toolDock = new ToolDock
        {
            Id = "Tools",
            Title = "Strumenti",
            Proportion = 0.35,
            ActiveDockable = Registers,
            VisibleDockables = CreateList<IDockable>(Registers, Stack, Memory, Errors),
        };

        DocumentContainer = documentDock;
        ToolContainer     = toolDock;

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
