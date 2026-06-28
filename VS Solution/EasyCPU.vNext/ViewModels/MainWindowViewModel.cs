#nullable enable
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;
using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Processore;

namespace EasyCPU.vNext.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DockFactory _factory;

    public Cpu Cpu { get; } = new();
    public Compiler Compiler { get; } = new();
    public SettingsViewModel Settings { get; }
    public IRootDock? Layout { get; private set; }
    public IFactory DockFactory => _factory;

    public MainViewModel(SettingsViewModel settings)
    {
        Settings = settings;
        _factory = new EasyCPU.vNext.DockFactory(this);
        Layout = _factory.CreateLayout();
        _factory.InitLayout(Layout);
        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.Theme))
                NotifyThemeProps();
        };
    }

    // ── Visibilità pannelli ───────────────────────────────────────────────────
    // Called by DockFactory.CloseDockable so the menu stays in sync with X-button closes.
    public void OnPanelVisibilityChanged()
    {
        OnPropertyChanged(nameof(IsCodeEditorVisible));
        OnPropertyChanged(nameof(IsDataEditorVisible));
        OnPropertyChanged(nameof(IsRegistersVisible));
        OnPropertyChanged(nameof(IsStackVisible));
        OnPropertyChanged(nameof(IsMemoryVisible));
        OnPropertyChanged(nameof(IsErrorsVisible));
    }

    private void SetPanelVisible(IDockable? panel, bool visible)
    {
        if (panel is null) return;
        var container = _factory.ContainerFor(panel);
        if (container is null) return;

        var isVisible = _factory.IsPanelVisible(panel);
        if (isVisible == visible) return;

        if (visible)
            _factory.AddDockable(container, panel);
        else
            _factory.RemoveDockable(panel, collapse: false);

        OnPanelVisibilityChanged();
    }

    public bool IsCodeEditorVisible
    {
        get => _factory.IsPanelVisible(_factory.CodeEditor);
        set => SetPanelVisible(_factory.CodeEditor, value);
    }

    public bool IsDataEditorVisible
    {
        get => _factory.IsPanelVisible(_factory.DataEditor);
        set => SetPanelVisible(_factory.DataEditor, value);
    }

    public bool IsRegistersVisible
    {
        get => _factory.IsPanelVisible(_factory.Registers);
        set => SetPanelVisible(_factory.Registers, value);
    }

    public bool IsStackVisible
    {
        get => _factory.IsPanelVisible(_factory.Stack);
        set => SetPanelVisible(_factory.Stack, value);
    }

    public bool IsMemoryVisible
    {
        get => _factory.IsPanelVisible(_factory.Memory);
        set => SetPanelVisible(_factory.Memory, value);
    }

    public bool IsErrorsVisible
    {
        get => _factory.IsPanelVisible(_factory.Errors);
        set => SetPanelVisible(_factory.Errors, value);
    }

    // ── Stato tema (per radio menu) ──────────────────────────────────────────

    public bool IsThemeLight => Settings.Theme == AppTheme.Light;
    public bool IsThemeDark  => Settings.Theme == AppTheme.Dark;
    public bool IsThemeBlue  => Settings.Theme == AppTheme.Blue;

    private void NotifyThemeProps()
    {
        OnPropertyChanged(nameof(IsThemeLight));
        OnPropertyChanged(nameof(IsThemeDark));
        OnPropertyChanged(nameof(IsThemeBlue));
    }

    // ── File ─────────────────────────────────────────────────────────────────

    [RelayCommand] private void New() { }
    [RelayCommand] private void Open() { }
    [RelayCommand] private void Save() { }
    [RelayCommand] private void SaveAs() { }
    [RelayCommand] private void Print() { }

    [RelayCommand]
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    // ── Modifica ──────────────────────────────────────────────────────────────

    [RelayCommand] private void Undo() { }
    [RelayCommand] private void Redo() { }
    [RelayCommand] private void Cut() { }
    [RelayCommand] private void Copy() { }
    [RelayCommand] private void Paste() { }
    [RelayCommand] private void SelectAll() { }
    [RelayCommand] private void Find() { }

    // ── Esegui ───────────────────────────────────────────────────────────────

    [RelayCommand] private void Compile() { }
    [RelayCommand] private void Run() { }
    [RelayCommand] private void RunUntil() { }
    [RelayCommand] private void StepInto() { }
    [RelayCommand] private void StepOver() { }
    [RelayCommand] private void StepOut() { }
    [RelayCommand] private void Stop() { }
    [RelayCommand] private void ToggleBreakpoint() { }

    // ── Finestre ─────────────────────────────────────────────────────────────

    [RelayCommand] private void ToggleCodeEditor() => IsCodeEditorVisible = !IsCodeEditorVisible;
    [RelayCommand] private void ToggleDataEditor()  => IsDataEditorVisible = !IsDataEditorVisible;
    [RelayCommand] private void ToggleRegisters()   => IsRegistersVisible  = !IsRegistersVisible;
    [RelayCommand] private void ToggleStack()       => IsStackVisible      = !IsStackVisible;
    [RelayCommand] private void ToggleMemory()      => IsMemoryVisible     = !IsMemoryVisible;
    [RelayCommand] private void ToggleErrors()      => IsErrorsVisible     = !IsErrorsVisible;

    [RelayCommand]
    private void ResetLayout()
    {
        Layout = _factory.CreateLayout();
        _factory.InitLayout(Layout!);
        OnPropertyChanged(nameof(Layout));
        OnPanelVisibilityChanged();
    }

    // ── Strumenti ────────────────────────────────────────────────────────────

    [RelayCommand] private void ShowOptions() { }
    [RelayCommand] private void SetThemeLight() => Settings.Theme = AppTheme.Light;
    [RelayCommand] private void SetThemeDark()  => Settings.Theme = AppTheme.Dark;
    [RelayCommand] private void SetThemeBlue()  => Settings.Theme = AppTheme.Blue;
}
