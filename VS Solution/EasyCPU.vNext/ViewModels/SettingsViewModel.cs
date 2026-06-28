using CommunityToolkit.Mvvm.ComponentModel;
using EasyCpu.Common;

namespace EasyCPU.vNext.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public static SettingsViewModel Instance { get; } = new();

    [ObservableProperty] private FormatoValore _formatoDati;
    [ObservableProperty] private string _formatoCarZero = "";
    [ObservableProperty] private int _maxNumErrori;
    [ObservableProperty] private int _colonneStack;
    [ObservableProperty] private bool _inizializzaRegistri;
    [ObservableProperty] private int _loopInfinito;
    [ObservableProperty] private int _margineSinistro;
    [ObservableProperty] private bool _mostraMemoria;
    [ObservableProperty] private bool _pienoSchermo;

    private SettingsViewModel()
    {
        _formatoDati = Ambiente.FormatoDati;
        _formatoCarZero = Ambiente.FormatoCarZero ?? "\\0";
        _maxNumErrori = Ambiente.MaxNumErrori;
        _colonneStack = Ambiente.ColonneStack;
        _inizializzaRegistri = Ambiente.InizializzaRegistri;
        _loopInfinito = Ambiente.LoopInfinito;
        _margineSinistro = Ambiente.MargineSinistro;
        _mostraMemoria = Ambiente.MostraMemoria;
        _pienoSchermo = Ambiente.PienoSchermo;
    }
}
