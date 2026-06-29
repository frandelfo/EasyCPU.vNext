#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;
using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Processore;
using EasyCpu.Common;

namespace EasyCPU.vNext.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DockFactory _factory;
    private bool _atBreakpoint;

    public Cpu Cpu { get; } = new();
    public Compiler Compiler { get; } = new();
    public SettingsViewModel Settings { get; }
    public IRootDock? Layout { get; private set; }
    public IFactory DockFactory => _factory;

    // Breakpoints: 1-based line numbers (AvaloniaEdit convention)
    public ObservableCollection<int> Breakpoints { get; } = new();

    [ObservableProperty] private int _currentSourceLine = -1;

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
        Breakpoints.CollectionChanged += (_, _) => SyncBreakpointsToCpu();
    }

    // ── Breakpoint helpers ────────────────────────────────────────────────────

    // Called by BreakpointMargin (click) and ToggleBreakpoint command (F9).
    public void ToggleBreakpointLine(int lineNumber)
    {
        if (Breakpoints.Contains(lineNumber))
            Breakpoints.Remove(lineNumber);
        else
            Breakpoints.Add(lineNumber);
        // CollectionChanged fires → SyncBreakpointsToCpu
    }

    private void SyncBreakpointsToCpu()
    {
        Cpu.Breakpoints.Clear();
        if (Compiler.LineToInstrMap == null) return;
        foreach (int lineNumber in Breakpoints)
        {
            int idx = lineNumber - 1; // AvaloniaEdit 1-based → compiler 0-based
            if (idx >= 0 && idx < Compiler.LineToInstrMap.Length)
            {
                int instrIdx = Compiler.LineToInstrMap[idx];
                if (instrIdx >= 0)
                    Cpu.Breakpoints.Add(instrIdx);
            }
        }
    }

    private void UpdateCurrentSourceLine()
    {
        if (Compiler.InstrToLineMap == null || Cpu.stop)
        {
            CurrentSourceLine = -1;
            return;
        }
        int ip = Cpu.IP;
        if (ip >= 0 && ip < Compiler.InstrToLineMap.Count)
            CurrentSourceLine = Compiler.InstrToLineMap[ip] + 1; // 0-based → 1-based
        else
            CurrentSourceLine = -1;
    }

    // ── Visibilità pannelli ───────────────────────────────────────────────────

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

    [RelayCommand] private void Undo()      => _factory.CodeEditor?.UndoAction?.Invoke();
    [RelayCommand] private void Redo()      => _factory.CodeEditor?.RedoAction?.Invoke();
    [RelayCommand] private void Cut()       => _factory.CodeEditor?.CutAction?.Invoke();
    [RelayCommand] private void Copy()      => _factory.CodeEditor?.CopyAction?.Invoke();
    [RelayCommand] private void Paste()     => _factory.CodeEditor?.PasteAction?.Invoke();
    [RelayCommand] private void SelectAll() => _factory.CodeEditor?.SelectAllAction?.Invoke();
    [RelayCommand] private void Find()      => _factory.CodeEditor?.FindAction?.Invoke();

    // ── Esegui ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Compile()
    {
        var codeEditor = _factory.CodeEditor;
        if (codeEditor == null) return;

        var codeLines = codeEditor.SourceText
            .Replace("\r\n", "\n").Replace("\r", "\n")
            .Split('\n')
            .ToList();

        var dataLines = (_factory.DataEditor?.SourceText ?? "")
            .Replace("\r\n", "\n").Replace("\r", "\n")
            .Split('\n')
            .ToList();

        List<CompilerError> codeErrors = null!;
        var instructions = Compiler.CompilaCodice(codeLines, ref codeErrors);
        if (instructions == null) return; // TODO Fase 5: mostrare errori

        List<CompilerError> dataErrors = null!;
        var memory = Compiler.CompilaDati(dataLines, ref dataErrors);
        if (memory == null) return; // TODO Fase 5: mostrare errori

        _atBreakpoint = false;
        Cpu.Init(instructions, memory, Ambiente.InizializzaRegistri, Ambiente.LoopInfinito);
        SyncBreakpointsToCpu();
        UpdateCurrentSourceLine();
    }

    [RelayCommand]
    private void Run()
    {
        if (Compiler.InstrToLineMap == null) return;
        try
        {
            // Per assunzione Fase 1: dopo CpuTrapException, chiamare StepInto prima di Run
            if (_atBreakpoint)
            {
                _atBreakpoint = false;
                Cpu.StepInto();
                if (Cpu.stop) { UpdateCurrentSourceLine(); return; }
            }
            Cpu.Run();
        }
        catch (CpuTrapException) { _atBreakpoint = true; }
        catch (CpuLoopException) { Cpu.Stop(); } // TODO Fase 6: SospendiWindow
        catch (CpuException) { }                 // TODO Fase 5: mostrare errore
        UpdateCurrentSourceLine();
    }

    [RelayCommand] private void RunUntil() { }

    [RelayCommand]
    private void StepInto()
    {
        if (Compiler.InstrToLineMap == null) return;
        _atBreakpoint = false;
        try
        {
            Cpu.StepInto();
        }
        catch (CpuTrapException) { _atBreakpoint = true; }
        catch (CpuException) { }
        UpdateCurrentSourceLine();
    }

    [RelayCommand]
    private void StepOver()
    {
        if (Compiler.InstrToLineMap == null) return;
        _atBreakpoint = false;
        try
        {
            Cpu.StepOver();
        }
        catch (CpuTrapException) { _atBreakpoint = true; }
        catch (CpuException) { }
        UpdateCurrentSourceLine();
    }

    [RelayCommand]
    private void StepOut()
    {
        if (Compiler.InstrToLineMap == null) return;
        _atBreakpoint = false;
        try
        {
            Cpu.StepOut();
        }
        catch (CpuTrapException) { _atBreakpoint = true; }
        catch (CpuException) { }
        UpdateCurrentSourceLine();
    }

    [RelayCommand]
    private void Stop()
    {
        Cpu.Stop();
        _atBreakpoint = false;
        CurrentSourceLine = -1;
    }

    [RelayCommand]
    private void ToggleBreakpoint()
    {
        int line = _factory.CodeEditor?.CurrentLine ?? 0;
        if (line > 0)
            ToggleBreakpointLine(line);
    }

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
