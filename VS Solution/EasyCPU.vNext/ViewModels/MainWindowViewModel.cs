#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;
using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Processore;

namespace EasyCPU.vNext.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public Cpu Cpu { get; } = new();
    public Compiler Compiler { get; } = new();
    public SettingsViewModel Settings { get; }
    public IRootDock? Layout { get; private set; }
    public IFactory DockFactory { get; private set; }

    public MainViewModel(SettingsViewModel settings)
    {
        Settings = settings;
        var factory = new EasyCPU.vNext.DockFactory(this);
        DockFactory = factory;
        Layout = factory.CreateLayout();
        factory.InitLayout(Layout);
    }

    [RelayCommand] private void Compile() { }
    [RelayCommand] private void Run() { }
    [RelayCommand] private void StepInto() { }
    [RelayCommand] private void StepOver() { }
    [RelayCommand] private void StepOut() { }
}
