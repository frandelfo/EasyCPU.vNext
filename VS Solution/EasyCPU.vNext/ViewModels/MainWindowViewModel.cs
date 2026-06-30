#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using EasyCpu.Assembler.Memoria;
using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Processore;
using EasyCpu.Backend.Local;
using EasyCpu.Backend.Serializers;
using EasyCpu.Common;
using EasyCPU.vNext.Views;

namespace EasyCPU.vNext.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DockFactory _factory;
    private bool _atBreakpoint;
    private string? _currentFilePath;
    private bool _isLegacyFile;

    public Cpu Cpu { get; } = new();
    public Compiler Compiler { get; } = new();
    public SettingsViewModel Settings { get; }
    public IRootDock? Layout { get; private set; }
    public IFactory DockFactory => _factory;

    // Breakpoints: 1-based line numbers (AvaloniaEdit convention)
    public ObservableCollection<int> Breakpoints { get; } = new();

    [ObservableProperty] private int _currentSourceLine = -1;
    [ObservableProperty] private string _statusMessage = "Pronto";

    public MainViewModel(SettingsViewModel settings)
    {
        Settings = settings;
        _factory = new EasyCPU.vNext.DockFactory(this);
        Layout = _factory.CreateLayout();
        _factory.CurrentLayout = Layout;
        _factory.InitLayout(Layout);
        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.Theme))
                NotifyThemeProps();
        };
        Breakpoints.CollectionChanged += (_, _) => SyncBreakpointsToCpu();
    }

    // ── Breakpoint helpers ────────────────────────────────────────────────────

    public void ToggleBreakpointLine(int lineNumber)
    {
        if (Breakpoints.Contains(lineNumber))
            Breakpoints.Remove(lineNumber);
        else
            Breakpoints.Add(lineNumber);
    }

    private void SyncBreakpointsToCpu()
    {
        Cpu.Breakpoints.Clear();
        if (Compiler.LineToInstrMap == null) return;
        foreach (int lineNumber in Breakpoints)
        {
            int idx = lineNumber - 1;
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
            CurrentSourceLine = Compiler.InstrToLineMap[ip] + 1;
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

    private static readonly string LayoutFilePath =
        Path.Combine(Ambiente.EasyCPUPath, "layout.json");

    private static readonly JsonSerializerOptions LayoutJsonOpts = new()
    {
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private Window? GetOwnerWindow() =>
        (Avalonia.Application.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    private static void SetEditorText(CodeEditorViewModel? vm, string text)
    {
        if (vm is null) return;
        vm.SourceText = text;
        vm.SetSourceTextAction?.Invoke(text);
    }

    private static void SetEditorText(DataEditorViewModel? vm, string text)
    {
        if (vm is null) return;
        vm.SourceText = text;
        vm.SetSourceTextAction?.Invoke(text);
    }

    private void OpenFileFromPath(string path)
    {
        try
        {
            var ser = ISourceSerializer.ForPath(path);
            var (code, data) = ser.Load(path);
            if (_currentFilePath is not null) SaveBreakpoints(_currentFilePath);
            SetEditorText(_factory.CodeEditor, string.Join("\n", code));
            SetEditorText(_factory.DataEditor, string.Join("\n", data));
            _currentFilePath = path;
            _isLegacyFile = !path.EndsWith(".asj", StringComparison.OrdinalIgnoreCase);
            Breakpoints.Clear();
            LoadBreakpoints(path);
            AddToRecentFiles(path);
            StatusMessage = $"Aperto: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore apertura: {ex.Message}";
        }
    }

    [RelayCommand]
    private void New()
    {
        if (_currentFilePath is not null) SaveBreakpoints(_currentFilePath);
        SetEditorText(_factory.CodeEditor, "");
        SetEditorText(_factory.DataEditor, "");
        Breakpoints.Clear();
        _currentFilePath = null;
        _isLegacyFile = false;
        StatusMessage = "Nuovo file";
    }

    [RelayCommand]
    private async Task Open()
    {
        var owner = GetOwnerWindow();
        if (owner is null) return;

        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Apri",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Easy CPU (*.asj; *.as)") { Patterns = ["*.asj", "*.as"] },
                new FilePickerFileType("Tutti i file")           { Patterns = ["*.*"] }
            }
        });
        if (files.Count == 0) return;
        OpenFileFromPath(files[0].Path.LocalPath);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (_currentFilePath is null || _isLegacyFile)
            await SaveToPickedPath();
        else
            SaveToPath(_currentFilePath);
    }

    [RelayCommand]
    private async Task SaveAs() => await SaveToPickedPath();

    private async Task SaveToPickedPath()
    {
        var owner = GetOwnerWindow();
        if (owner is null) return;

        var suggested = _currentFilePath is not null
            ? Path.GetFileNameWithoutExtension(_currentFilePath)
            : "file1";

        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salva come",
            DefaultExtension = ".asj",
            SuggestedFileName = suggested,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Easy CPU JSON (*.asj)") { Patterns = ["*.asj"] }
            }
        });
        if (file is null) return;

        var path = file.Path.LocalPath;
        SaveToPath(path);
        _currentFilePath = path;
        _isLegacyFile = false;
    }

    private void SaveToPath(string path)
    {
        try
        {
            var code = (_factory.CodeEditor?.SourceText ?? "")
                .Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var data = (_factory.DataEditor?.SourceText ?? "")
                .Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            new EasyFileSerializer().Save(path, code, data);
            SaveBreakpoints(path);
            AddToRecentFiles(path);
            StatusMessage = $"Salvato: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore salvataggio: {ex.Message}";
        }
    }

    [RelayCommand] private void Print() { }

    [RelayCommand]
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    // ── File recenti ──────────────────────────────────────────────────────────

    public ObservableCollection<RecentFileItem> RecentFileItems { get; } = new();

    private void AddToRecentFiles(string path)
    {
        Ambiente.AggiungiRecenti(path);
        RefreshRecentFileItems();
    }

    public void RefreshRecentFileItems()
    {
        RecentFileItems.Clear();
        foreach (var path in Ambiente.FileRecenti)
            RecentFileItems.Add(new RecentFileItem(path, OpenFileFromPath));
    }

    [RelayCommand]
    private void OpenRecentFile(string path)
    {
        if (!File.Exists(path))
        {
            StatusMessage = $"File non trovato: {Path.GetFileName(path)}";
            Ambiente.FileRecenti.Remove(path);
            RefreshRecentFileItems();
            return;
        }
        OpenFileFromPath(path);
    }

    // ── Layout persistence ────────────────────────────────────────────────────

    internal void SaveLayout()
    {
        try
        {
            if (Layout is null) return;
            Directory.CreateDirectory(Ambiente.EasyCPUPath);
            var node = _factory.ToDockNode(Layout);
            File.WriteAllText(LayoutFilePath, JsonSerializer.Serialize(node, LayoutJsonOpts));
        }
        catch { }
    }

    internal void LoadLayout()
    {
        try
        {
            if (!File.Exists(LayoutFilePath)) return;
            var node = JsonSerializer.Deserialize<DockNode>(File.ReadAllText(LayoutFilePath), LayoutJsonOpts);
            if (node is null) return;

            var all = new Dictionary<string, IDockable?>
            {
                ["CodeEditor"] = _factory.CodeEditor,
                ["DataEditor"] = _factory.DataEditor,
                ["Registers"]  = _factory.Registers,
                ["Stack"]      = _factory.Stack,
                ["Memory"]     = _factory.Memory,
                ["Errors"]     = _factory.Errors,
            };

            var newLayout = _factory.RebuildLayout(node, all);
            if (newLayout is null) return;
            Layout = newLayout;
            _factory.CurrentLayout = Layout;
            _factory.InitLayout(Layout);
        }
        catch { }
    }

    // ── Breakpoint persistence ────────────────────────────────────────────────

    private void SaveBreakpoints(string filePath)
    {
        try
        {
            var bkptFile = filePath + ".bkpt";
            if (Breakpoints.Count == 0)
            {
                if (File.Exists(bkptFile)) File.Delete(bkptFile);
            }
            else
            {
                File.WriteAllLines(bkptFile, Breakpoints.Select(l => l.ToString()));
            }
        }
        catch { }
    }

    private void LoadBreakpoints(string filePath)
    {
        try
        {
            var bkptFile = filePath + ".bkpt";
            if (!File.Exists(bkptFile)) return;
            foreach (var line in File.ReadAllLines(bkptFile))
                if (int.TryParse(line.Trim(), out int lineNum) && lineNum > 0)
                    Breakpoints.Add(lineNum);
        }
        catch { }
    }

    internal void SaveCurrentBreakpoints()
    {
        if (_currentFilePath is not null)
            SaveBreakpoints(_currentFilePath);
    }

    internal void SaveAll()
    {
        SaveLayout();
        SaveCurrentBreakpoints();
        Storage.SalvaFileRecenti();
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

    private bool DoCompile()
    {
        var codeEditor = _factory.CodeEditor;
        if (codeEditor == null) return false;

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

        List<CompilerError> dataErrors = null!;
        var memory = Compiler.CompilaDati(dataLines, ref dataErrors);

        var ev = _factory.Errors;
        if (ev != null)
        {
            ev.Errors.Clear();
            var sorted = (codeErrors ?? []).Select(e => new CompilerErrorAdapter(e))
                .Concat((dataErrors ?? []).Select(e => new CompilerErrorAdapter(e)))
                .OrderBy(a => a.RigaDisplay)
                .ThenBy(a => a.TipoDisplay);
            foreach (var a in sorted) ev.Errors.Add(a);
        }

        if (instructions == null || memory == null)
        {
            int n = ev?.Errors.Count ?? 0;
            StatusMessage = n == 1 ? "Compilazione: 1 errore" : $"Compilazione: {n} errori";
            IsErrorsVisible = true;
            if (_factory.Errors is { } ep) _factory.SetActiveDockable(ep);
            if (_factory.Registers is { } rv) rv.Dump = "";
            if (_factory.Memory is { } mv) mv.Dump = "";
            if (_factory.Stack is { } sv) sv.Dump = "";
            return false;
        }

        StatusMessage = "Compilazione completata";
        _atBreakpoint = false;
        Cpu.Init(instructions, memory, Ambiente.InizializzaRegistri, Ambiente.LoopInfinito);
        SyncBreakpointsToCpu();
        UpdateCurrentSourceLine();
        return true;
    }

    [RelayCommand]
    private void Compile()
    {
        if (DoCompile())
            RefreshDebugViews();
    }

    [RelayCommand]
    private async Task Run()
    {
        if (_atBreakpoint)
        {
            _atBreakpoint = false;
            try { Cpu.StepInto(); }
            catch (CpuException) { UpdateCurrentSourceLine(); RefreshDebugViews(); return; }
            if (Cpu.stop)
            {
                UpdateCurrentSourceLine();
                RefreshDebugViews();
                StatusMessage = "Esecuzione terminata";
                return;
            }
        }
        else
        {
            if (!DoCompile()) return;
        }

        var owner = GetOwnerWindow();
        while (true)
        {
            try
            {
                Cpu.Run();
                break;
            }
            catch (CpuTrapException) { _atBreakpoint = true; break; }
            catch (CpuLoopException)
            {
                var modo = owner is not null
                    ? await new SospendiWindow().ShowDialog<ModoSospendi>(owner)
                    : ModoSospendi.Arresta;
                if (modo == ModoSospendi.Continua) continue;
                if (modo == ModoSospendi.Pausa) { _atBreakpoint = true; break; }
                Cpu.Stop();
                break;
            }
            catch (CpuException) { break; }
        }

        UpdateCurrentSourceLine();
        RefreshDebugViews();
        if (_atBreakpoint)
            StatusMessage = CurrentSourceLine > 0
                ? $"Breakpoint — riga {CurrentSourceLine}"
                : "Breakpoint raggiunto";
        else if (Cpu.stop)
            StatusMessage = "Esecuzione terminata";
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
        RefreshDebugViews();
        StatusMessage = CurrentSourceLine > 0 ? $"Riga {CurrentSourceLine}" : "Esecuzione terminata";
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
        RefreshDebugViews();
        StatusMessage = CurrentSourceLine > 0 ? $"Riga {CurrentSourceLine}" : "Esecuzione terminata";
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
        RefreshDebugViews();
        StatusMessage = CurrentSourceLine > 0 ? $"Riga {CurrentSourceLine}" : "Esecuzione terminata";
    }

    [RelayCommand]
    private void Stop()
    {
        Cpu.Stop();
        _atBreakpoint = false;
        CurrentSourceLine = -1;
        RefreshDebugViews();
        StatusMessage = "Esecuzione interrotta";
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

    [RelayCommand]
    private async Task ShowOptions()
    {
        var owner = GetOwnerWindow();
        if (owner is null) return;
        var vm = new OpzioniViewModel(Settings);
        var dialog = new OpzioniWindow { DataContext = vm };
        var ok = await dialog.ShowDialog<bool>(owner);
        if (ok)
        {
            vm.ApplyTo(Settings);
            Storage.SalvaOpzioni();
            RefreshDebugViews();
        }
    }

    [RelayCommand] private void SetThemeLight() => Settings.Theme = AppTheme.Light;
    [RelayCommand] private void SetThemeDark()  => Settings.Theme = AppTheme.Dark;
    [RelayCommand] private void SetThemeBlue()  => Settings.Theme = AppTheme.Blue;

    // ── Debug views ───────────────────────────────────────────────────────────

    private void RefreshDebugViews()
    {
        var regs = Cpu.DumpRegs();
        if (_factory.Registers is { } rv)
            rv.Dump = string.Join("\n", regs) +
                      $"\nZ={(Cpu.FlagZero ? 1 : 0)}  S={(Cpu.FlagSegno ? 1 : 0)}  O={(Cpu.FlagOverflow ? 1 : 0)}";

        var mem = Cpu.DumpMemoria(0, Ram.INDIRIZZO_STACK, 8);
        if (_factory.Memory is { } mv)
            mv.Dump = mem is null ? "" : string.Join("\n", mem);

        var stack = Cpu.DumpMemoria(Ram.INDIRIZZO_STACK, Ram.MASSIMO_INDIRIZZO + 1, Ambiente.ColonneStack);
        if (_factory.Stack is { } sv)
            sv.Dump = stack is null ? "" : string.Join("\n", stack);
    }

    public void NavigateToError(CompilerError err)
    {
        int lineNumber = err.Riga + 1;
        if (err.Tipo == CompilerError.CODICE)
        {
            if (_factory.CodeEditor is not { } editor) return;
            _factory.SetActiveDockable(editor);
            editor.NavigateToLineAction?.Invoke(lineNumber);
        }
        else
        {
            if (_factory.DataEditor is not { } editor) return;
            _factory.SetActiveDockable(editor);
            editor.NavigateToLineAction?.Invoke(lineNumber);
        }
    }
}
