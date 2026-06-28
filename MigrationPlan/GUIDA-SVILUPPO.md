# EasyCPU.vNext — Guida allo sviluppo

> Runbook operativo per la migrazione da WinForms ad Avalonia 12. Ogni fase
> ha: **obiettivo**, **passi**, **verifica** (come capire che è andata bene) e
> **problemi probabili** con mitigazione. Da leggere insieme a
> `README.reviewed.md` (analisi e correzioni) e `README.md` (piano originale).
>
> Tutti i riferimenti a file sono relativi a `VS Solution/`. Tutti i fatti sono
> stati verificati sul sorgente al 27/06/2026.

---

## Come usare questa guida

- Esegui le fasi **in ordine**: ognuna è un prerequisito della successiva.
- Non passare alla fase successiva se la sezione **Verifica** non è verde.
- Lavora a piccoli commit: un commit per checkbox quando possibile.
- Tieni `EasyCpu.Win` come **riferimento di comportamento** (anche se rimosso dalla soluzione, il codice resta nella cartella e in git) finché la IDE Avalonia non replica le stesse funzioni.

### Criteri trasversali di "fatto" (Definition of Done)

| Criterio | Comando / controllo |
|---|---|
| La soluzione compila | `dotnet build EasyCPU.vNext.slnx -c Debug` → 0 errori |
| Nessun warning di downgrade pacchetti | nessun `NU1605` (è impostato `warnAsError` su NU1605) |
| I test del core passano | `dotnet test` (dopo aver creato il progetto di test, Fase 1) |
| L'app desktop parte | `dotnet run --project EasyCPU.vNext.Desktop` apre la finestra |

---

## Prerequisiti d'ambiente

- **.NET SDK 10.0.101** installato (verrà bloccato da `global.json`). Verifica: `dotnet --version`.
- Workload per gli head mobili/web (necessari solo quando si compilano quegli head):
  - `dotnet workload install android ios wasm-tools`
- Per Android: SDK/JDK configurati (nel repo ci sono già i log `sdkmanager`).
- Per iOS: build solo su macOS con Xcode.
- Editor: Rider o VS 2022/VS Code con estensione Avalonia.

> Nota: negli `obj/` ci sono artefatti sia `net9.0` sia `net10.0`. Dopo aver
> aggiunto `global.json`, esegui una pulizia completa (vedi Fase 0) per evitare
> che restino in giro binari compilati con SDK diversi.

---

## Quadro di partenza (stato reale verificato)

- Tutti gli head Avalonia sono su **11.3.9**; `Directory.Build.props` → `AvaloniaVersion 11.3.8`. Target: **12.0.5**.
- `EasyCPU.vNext` **è il progetto demo di AvaloniaEdit** (decine di `Resources/SampleFiles/*`, `ThemeViewModel`, `MainWindowViewModel`/`MainEditorView`/`MainView` demo). Va svuotato.
- Il core (`Cpu`, `Compiler`, `Parser`) è **interamente statico**.
- Esiste già un meccanismo trap a 1 breakpoint (`Instruction.Trap`, `Cpu.SetTrap`, `inTrap`, `rigaTrap`, `CpuTrapException`): verrà **sostituito** da `HashSet<int> Breakpoints`.
- `EasyCpu.Win`/`Win.Controls` usano il core staticamente: vengono **rimossi** dalla soluzione.
- Pacchetti target (tutti reali su nuget.org, giugno 2026): `Avalonia 12.0.5`, `Avalonia.AvaloniaEdit 12.0.0`, `AvaloniaEdit.TextMate 12.0.0`, `Dock.Avalonia` + sotto-pacchetti `12.0.0.2` (con `Dock.Model.Mvvm`), `CommunityToolkit.Mvvm 8.4.0`. **Non si usa ReactiveUI.**

---

## FASE 0 — Pulizia, versioni e setup

**Obiettivo:** soluzione snella, su Avalonia 12, che compila ancora (con `EasyCPU.vNext` ridotto a scheletro).

### Passi

1. Creare `global.json` nella root di `VS Solution/`:

   ```json
   { "sdk": { "version": "10.0.101", "rollForward": "latestPatch" } }
   ```

2. Rimuovere dalla soluzione (`.sln`) **e** dal disco i progetti morti/legacy:
   - `AvaloniaEdit/` e `AvaloniaEdit.TextMate/` (sorgenti → sostituiti dai NuGet)
   - `EasyCpu.Core/` (copia vecchia di `Common`, non referenziata da nessuno)
   - `EasyCpu.Win/` e `EasyCpu.Win.Controls/`
   - Eliminare anche le solution folder rimaste vuote: `Avalonia Edit`, `Frontend Legacy`, `Backend` (se vuota).

3. `Directory.Build.props`: `<AvaloniaVersion>12.0.5</AvaloniaVersion>`.

4. Allineare **tutti** i `.csproj` ad Avalonia 12.0.5 e CommunityToolkit.Mvvm 8.4.0:
   - `EasyCPU.vNext`: `Avalonia`, `Avalonia.Themes.Fluent` → 12.0.5; togliere i `ProjectReference` ad AvaloniaEdit; aggiungere `Avalonia.AvaloniaEdit 12.0.0`, `AvaloniaEdit.TextMate 12.0.0`, `CommunityToolkit.Mvvm 8.4.0`, e i pacchetti Dock 12.0.0.2 (`Dock.Avalonia`, `Dock.Model.Mvvm`, `Dock.Serializer.SystemTextJson`, `Dock.Avalonia.Themes.Fluent`).
   - `.Desktop`: `Avalonia`, `Avalonia.Desktop`, `Avalonia.Fonts.Inter` → 12.0.5; **rimuovere il `ProjectReference` duplicato** a `EasyCPU.vNext` (compare due volte).
   - `.Browser`: `Avalonia.Browser`, `Avalonia.Fonts.Inter` → 12.0.5.
   - `.Android`: `Avalonia.Android`, `Avalonia.Fonts.Inter` → 12.0.5.
   - `.iOS`: `Avalonia.iOS`, `Avalonia.Fonts.Inter` → 12.0.5.
   - Rimuovere `ReactiveUI.Avalonia` da tutti i `.csproj` (sostituito da CommunityToolkit.Mvvm; non serve nei head perché non è un requisito Avalonia).

5. In `App.xaml`, aggiornare lo `StyleInclude` del tema AvaloniaEdit: oggi punta al progetto sorgente `avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml`. Con il NuGet l'assembly resta `AvaloniaEdit`, quindi l'URI **dovrebbe** restare valido: verificarlo all'avvio (vedi Problemi).

6. Svuotare il demo da `EasyCPU.vNext`: rimuovere `Resources/SampleFiles/*` e il relativo blocco `EmbeddedResource`/`Compile Remove` nel `.csproj`, eliminare `ThemeViewModel`, e ridurre `MainWindowViewModel`/`MainView`/`MainEditorView` a un guscio vuoto (li ricostruiremo nelle fasi 2–5). Mantenere `App.xaml(.cs)`, `Program.cs`, la struttura lifetime desktop/single-view.

7. Pulizia build: `git clean -xdf` (o cancellare tutti i `bin/` `obj/`), poi `dotnet restore`.

### Verifica

- `dotnet build EasyCPU.vNext.slnx -c Debug` → 0 errori, **0 NU1605**.
- `dotnet run --project EasyCPU.vNext.Desktop` → finestra vuota/scheletro che si apre senza crash.
- `git status` mostra rimossi solo i progetti previsti; `.sln` non contiene più i GUID dei progetti eliminati.

### Problemi probabili

- **NU1605 (downgrade)**: un head è rimasto a 11.3.9 o un pacchetto Dock/AvaloniaEdit tira una versione diversa di Avalonia. Allineare tutto a 12.0.5; usare eventualmente `Directory.Packages.props` (Central Package Management) per centralizzare le versioni.
- **`avares://AvaloniaEdit/...` non risolto** dopo il passaggio al NuGet: se il tema non carica, l'editor appare senza stile o l'app lancia `XamlLoadException`. Verificare il nome reale della risorsa nel pacchetto (Rider → "Assembly Explorer", oppure provare `avares://Avalonia.AvaloniaEdit/...`).
- **Riferimenti rotti** dopo la rimozione dei progetti: cercare in tutta la soluzione `EasyCpu.Win`, `AvaloniaEdit\\` (ProjectReference), `EasyCpu.Core`.
- **Workload mancanti**: la `.sln` completa non compila se mancano android/ios/wasm. In sviluppo quotidiano si può lavorare su una solution filter (`.slnf`) con i soli `EasyCPU.vNext` + `.Desktop` + business logic.
- **Build misti net9/net10** residui: se compaiono errori strani, ripulire `bin/obj` (passo 7).

---

## FASE 1 — Refactoring BusinessLogic (da statico a istanze)

**Obiettivo:** `Cpu`, `Compiler`, `Parser` istanziabili, breakpoint multipli, core testabile e cross-platform.

### Passi

1. **`Parser`** (`Parsing/Parser.cs`): rimuovere `static` da stato e metodi.
   - `static string riga` → `string _riga`; `static int indcar` → `int _indcar`; `IndCar` proprietà d'istanza.
   - `SetCode` può restare `static readonly` (è una tabella costante).

2. **`Compiler`** (`Parsing/Compiler.cs`):
   - Rendere i metodi d'istanza; il `Compiler` crea/usa un proprio `Parser`.
   - `TabellaDebug` → `InstrToLineMap` (proprietà `List<int>`, istruzione→riga).
   - Aggiungere `int[] LineToInstrMap` (riga→indice istruzione), costruita a fine compilazione; valore **-1** per righe non eseguibili (vuote, solo-commento, solo-etichetta).
   - **Rimuovere** il parametro `rigaTrap`, la variabile `messoTrap` e `istr.Trap = true`. Firma nuova: `List<Instruction> CompilaCodice(List<string> code, ref List<CompilerError> errori)`.

3. **`Cpu`** (`Processore/Cpu.cs`):
   - Tutti i campi (`ax..ip`, `flags`, `stop`, `loopInfinito`, `memoria`, `Code`, `curIstruzione`, `Stato`, `inTrap`) → d'istanza.
   - Esporre `public short SP => sp;` (oggi manca).
   - Aggiungere `public HashSet<int> Breakpoints { get; } = new();`
   - Sostituire il controllo `Trap()` in `Run()` con `if (Breakpoints.Contains(ip)) throw new CpuTrapException();` (vedi pseudo-codice in `README.reviewed.md` §2).
   - Aggiungere `StepOver()`, `StepOut()`, `RunWhileInside(short limite)` con la logica corretta per **stack discendente** (vedi sotto, "Motore di debug").
   - Rimuovere `SetTrap`, `inTrap`, `Trap()`.

4. **`Instruction`** (`Parsing/Instruction.cs`): rimuovere il campo `Trap` (non più usato). Mantenere `Code`, `indRiga`, operandi.

5. **`Storage`** (`Backend/Local/Storage.cs`):
   - Rimuovere `using System.Drawing;` e i metodi `SalvaStatoFinestre`/`LeggiStatoFinestre`.
   - Può restare `static` (è un servizio I/O senza stato), ma le **path** vanno corrette (vedi punto 6).

6. **Cross-platform delle path (NON nel piano originale, ma necessario):**
   `Common/Ambiente.cs` costruisce le path con separatori Windows hardcoded
   (`+ "\\"`, `+ "\\EasyCPU\\"`, `PATHPROGETTI = "EasyCPU Progetti\\"`). Su
   macOS/Linux questo rompe il salvataggio di opzioni/recenti. Sostituire con
   `Path.Combine(...)` / `Path.DirectorySeparatorChar`. Rimuovere anche il
   `using System.Drawing;` di `Ambiente.cs` (residuo, non più usato dopo la
   rimozione dei riferimenti a `Font`).

7. **Progetto di test**: creare `EasyCpu.Assembler.Tests` (xUnit, `net10.0`) con riferimento ad `Assembler`/`Common`.

### Verifica

- `dotnet build` dei tre progetti business → 0 errori.
- `dotnet test` verde. Test minimi obbligatori:
  - **Step Into**: programma `mov ax,5 / inc ax / stop` → dopo 2 step `AX==6`.
  - **Step Over su `call`**: il `call` e tutta la subroutine vengono eseguiti in un colpo e **`SP` torna al valore pre-call** (controlla il verso dello stack).
  - **Step Out**: partendo da dentro la subroutine, esegue fino al `ret` e `SP` risale di 1.
  - **Breakpoint multipli**: due breakpoint in punti diversi; `Run()` si ferma al primo, poi al secondo.
  - **`LineToInstrMap`**: su un sorgente con righe vuote, commenti e un'etichetta su riga propria, le righe non eseguibili mappano a -1 e le altre all'indice corretto.
  - **Determinismo istanze**: due `Cpu` istanziate in parallelo non condividono stato (regressione contro il vecchio `static`).

### Problemi probabili

- **Verso dello stack invertito**: è l'errore del piano originale. Lo stack **cresce verso il basso** (`Init` SP=256, push `sp--`, pop `sp++`). Se Step Over/Out "scappano" fino a fine programma o non si fermano, è perché le condizioni sono invertite.
- **`StepOver` guarda l'istruzione sbagliata**: dopo uno step `curIstruzione` è quella **già eseguita**; per decidere se è un `call` guardare **`code[ip]`**, non `curIstruzione`.
- **Loop infinito non rilevato durante step**: `RunWhileInside` deve replicare i controlli `IPOverRun` e loop-infinito presenti in `Run()`, altrimenti la `SospendiWindow` non scatta.
- **`Stato` statico residuo** o altri campi dimenticati `static`: causano "stato condiviso" tra istanze. Cercare la keyword `static` in `Cpu.cs` a fine refactor.
- **`Ram` resta con costanti `static readonly`** (`MASSIMO_INDIRIZZO`, `INDIRIZZO_STACK`): va bene, sono costanti; ma `memoria` è già d'istanza — assicurarsi che `Cpu` ne crei una nuova per ogni `Init`.

### Assunzioni e decisioni (registrate durante l'esecuzione — 2026-06-28)

> Queste note documentano scelte fatte durante l'implementazione che non erano
> esplicitate nel piano originale e che impattano le fasi successive.

1. **Contratto `StepInto` dopo un breakpoint (impatta Fase 3 — UI)**
   `Run()` controlla `Breakpoints.Contains(ip)` **prima** di `Fetch`+`Execute`. Quando scatta
   una `CpuTrapException`, `ip` rimane sull'istruzione del breakpoint (non ancora eseguita).
   Per riprendere l'esecuzione, la UI (ViewModel, Fase 3) deve:
   - Chiamare `StepInto()` una volta (avanza oltre il breakpoint senza ri-verifica).
   - Poi chiamare `Run()` o un altro comando di debug.
   Non fare `Run()` direttamente dopo il trap: re-triggherebbe immediatamente lo stesso breakpoint.

2. **`RunWhileInside(short limite)` — semantica: `sp < limite`**
   L'helper privato esegue passi finché `sp < limite`.
   - `StepOver` lo chiama con `limite = S` (SP pre-call): esegue mentre si è dentro la subroutine (`sp < S`), si ferma quando il `ret` riporta `sp == S`.
   - `StepOut` lo chiama con `limite = S + 1` (SP corrente + 1): equivale a "finché `sp ≤ S`", si ferma quando un `ret` porta `sp > S`.
   Questa simmetria (`StepOut = RunWhileInside(sp + 1)`) non era nel piano originale ma è verificata dai test.

3. **`PreparaRiga` invariato — marker commento `//` confermato**
   `AdattaRiga` in `Parser.cs` usava `'` come marcatore di commento (dead code rimosso).
   Il vero marker di commento è `//`, gestito da `Compiler.PreparaRiga`. La logica è stata
   mantenuta identica all'originale. Il `'` dentro `PreparaRiga` serve solo a rilevare
   costanti carattere (per non convertire in minuscolo il char tra apici).

4. **Mapping 0-based/1-based da gestire in Fase 3**
   `indRiga` nel compilatore e `InstrToLineMap`/`LineToInstrMap` usano indici **0-based**.
   AvaloniaEdit usa righe **1-based** (Fase 3, `BreakpointMargin` e `DebugCurrentLineRenderer`).
   La conversione `lineaAvaloniaEdit = indRiga + 1` va fatta **una sola volta** nell'adapter
   UI, mai nel core. Non introdurre offset nei test del core.

---

## FASE 2 — Dock + struttura ViewModel

**Obiettivo:** shell della IDE con pannelli dockabili e ViewModel collegati al core istanziato.

### Passi

- `DockFactory : Factory` che costruisce il layout iniziale (vedi `README.md` §8).
- ViewModel per pannello: `CodeEditorViewModel`, `DataEditorViewModel`, `RegistersViewModel`, `MemoryViewModel`, `StackViewModel`, `ErrorsViewModel`.
- `MainViewModel` con `[RelayCommand]` (CommunityToolkit) per i comandi IDE/debug; possiede l'istanza `Cpu` + `Compiler`.
- Creare qui il **`SettingsViewModel` singleton** (sorgente osservabile delle opzioni, vedi Fase 6) e iniettarlo in `MainViewModel`/pannelli: serve già da Fase 5 per la formattazione dei dump.
- Registrare i `DataTemplate` View↔ViewModel (`ViewLocator` o `DataTemplates` in `App.xaml`).
- Serializzazione layout con `Dock.Serializer.SystemTextJson`.

### Verifica

- L'app mostra i pannelli (anche vuoti) nel layout previsto; i pannelli si possono trascinare/agganciare.
- Spostando un pannello, chiudendo e riaprendo l'app, il layout si ripristina (persistenza, vedi Fase 7 — qui basta che il serializer non lanci).

### Dock 12: architettura con CommunityToolkit.Mvvm

Il progetto usa **`Dock.Model.Mvvm`** — il sotto-package che basa tutte le classi Dock su `ObservableObject` di CommunityToolkit, coerente con la scelta di non usare ReactiveUI.

> Nota: esiste anche `Dock.Model.ReactiveUI` (basa le classi su `ReactiveObject`). Sono alternative mutualmente esclusive: mescolarle genera conflitti di tipo `IFactory` a runtime, non a compile-time. Non aggiungere mai `Dock.Model.ReactiveUI`.

**Regola operativa (corretta dopo verifica sorgenti):**
- Tipi strutturali Dock (Document, Tool, RootDock, ProportionalDock, DocumentDock, ToolDock, ProportionalDockSplitter) → `using Dock.Model.Mvvm.Controls;`
- `Factory` (classe base DockFactory) → `using Dock.Model.Mvvm;`
- **`IRootDock`** → `using Dock.Model.Controls;` (**NON** `Dock.Model.Core` — errore frequente)
- `IDockable`, `IFactory` → `using Dock.Model.Core;`
- `Orientation` per ProportionalDock → `using Dock.Model.Core;` (enum `Orientation.Horizontal/Vertical`)

#### Tipo Dock per ogni pannello

I ViewModel pannello **ereditano** da `Tool` o `Document` (da `Dock.Model.Mvvm.Controls`) — sono gli item Dock, non li contengono. La classe deve essere `partial` per i source generator di CommunityToolkit.

| ViewModel | Tipo Dock | Motivo |
|---|---|---|
| `CodeEditorViewModel` | `Document` | area editing principale; supporta tab multipli |
| `DataEditorViewModel` | `Document` | idem per sezione `.DATA` |
| `RegistersViewModel` | `Tool` | pannello ausiliario, agganciabile ai lati |
| `MemoryViewModel` | `Tool` | idem |
| `StackViewModel` | `Tool` | idem |
| `ErrorsViewModel` | `Tool` | idem (come "Output" in VS) |

`MainViewModel : ObservableObject` — coordina, non è un item Dock.

`DockFactory` eredita da `Factory` di `Dock.Model.Mvvm`:

```csharp
using Dock.Model.Controls;   // IRootDock
using Dock.Model.Core;       // IDockable, Orientation
using Dock.Model.Mvvm;       // Factory
using Dock.Model.Mvvm.Controls; // DocumentDock, ToolDock, ProportionalDock, ecc.

public class DockFactory : Factory
{
    public override IRootDock CreateLayout()
    {
        var codeEditor = new CodeEditorViewModel { Id = "CodeEditor", Title = "Codice" };
        var documentDock = new DocumentDock
        {
            CanCreateDocument = false,
            Proportion = 0.65,
            ActiveDockable = codeEditor,
            VisibleDockables = CreateList<IDockable>(codeEditor, dataEditor)
        };
        // Root: CreateRootDock() factory method; layout: new ProportionalDock(Orientation.Horizontal)
        // Splitter tra dockables: new ProportionalDockSplitter()
    }
}
```

Panel VM con CommunityToolkit — classe `partial`, proprietà con `[ObservableProperty]`:

```csharp
public partial class RegistersViewModel : Tool
{
    [ObservableProperty]
    private string _dump = "";
}
```

`MainViewModel` usa `[RelayCommand]` al posto di `ReactiveCommand`:

```csharp
public partial class MainViewModel : ObservableObject
{
    [RelayCommand] private void StepInto() { /* ... */ }
    [RelayCommand] private void Run() { /* ... */ }
}
```

#### Confine serializzazione (obbligatorio per la persistenza di Fase 7)

`Dock.Serializer.SystemTextJson` serializza i `Tool`/`Document` come parte del layout. Questo impone un confine netto:

- **Nei ViewModel pannello (`Tool`/`Document`) NON mettere**: riferimenti a `Cpu`, liste di `CompilerError`, qualsiasi stato runtime.
- **Mettere solo**: proprietà che descrivono l'aspetto (titolo, visibilità, dimensioni) e comandi che delegano a `MainViewModel`.

Lo stato vivo (dump registri/memoria/stack, errori) vive in `MainViewModel`; i pannelli lo raggiungono via binding/subscribe. Senza questo confine il deserializer crasha al riavvio su riferimenti non serializzabili.

### Assunzioni e decisioni (registrate durante l'esecuzione — 2026-06-28)

1. **`IRootDock` è in `Dock.Model.Controls`** — stessa dll di `Dock.Model.Mvvm` via dipendenza transitiva `Dock.Model`. `Dock.Model.Core` contiene solo interfacce di base (`IDockable`, `IFactory`, `Orientation`). Il codice guida sopra aveva il namespace sbagliato.

2. **Tema Dock via classe XAML** — `<dockTheme:DockFluentTheme />` con `xmlns:dockTheme="clr-namespace:Dock.Avalonia.Themes.Fluent;assembly=Dock.Avalonia.Themes.Fluent"`. La classe è `Dock.Avalonia.Themes.Fluent.DockFluentTheme`, risorsa interna `/DockFluentTheme.axaml`.

3. **`DockControl` richiede sia `Layout` che `Factory` bindati** — senza `Factory` le operazioni di drag/drop non funzionano. Esporre `DockFactory` come proprietà `IFactory` su `MainViewModel`.

4. **ViewLocator `Match`** — limitato a `Tool or Document` (non tutta `ObservableObject`) per evitare che Dock tenti di risolvere le viste per i propri tipi interni.

5. **`SettingsViewModel` write-through rimandato** — per Fase 2, il singleton inizializza solo le proprietà da `Ambiente.*`. I callback `partial void OnXxxChanged` sono Fase 6.

6. **`#nullable enable`** — aggiunto nei file che usano annotazioni nullable (`?`) senza abilitarle nel csproj globale (per non toccare i warning del codice core pre-esistente).

### Problemi probabili

- **Dock 12 + ReactiveUI**: vedi sezione dedicata sopra. Il setup attuale è già corretto.
- **Tema Dock mancante**: includere `Dock.Avalonia.Themes.Fluent` in `App.xaml`, altrimenti i separatori/tab sono invisibili.
- **ViewLocator non trova le View**: i pannelli appaiono come testo "Not Found". Verificare convenzione di naming `XxxViewModel`→`XxxView` e namespace.
- **Serializzazione**: costruttori senza parametri obbligatori per tutti i `Tool`/`Document`; niente riferimenti ciclici nelle proprietà serializzate. Lo stato runtime della CPU non va nei pannelli Dock (vedi confine serializzazione sopra).

---

## FASE 3 — Editor con debug integrato

**Obiettivo:** editor AvaloniaEdit con margine breakpoint cliccabile ed evidenziazione riga corrente.

### Passi

- `BreakpointMargin : AbstractMargin` (margine a sinistra dei numeri di riga): click → toggle in `MainViewModel.Breakpoints` (numeri di riga).
- `DebugCurrentLineRenderer : IBackgroundRenderer`: evidenzia `MainViewModel.CurrentSourceLine`; quando vale -1 non disegna.
- Portare la logica di `EasyEditor` (vedi `README.md` §9) su AvaloniaEdit via `TextArea.TextEntering`/`TextEntered` (Tab = spazi fino al margine, Enter = rientro di `Ambiente.MargineSinistro` spazi) e le conversioni riga↔offset con `Document.GetLineByNumber`/`CaretOffset`.

### Verifica

- Click nel margine aggiunge/toglie il pallino e aggiorna `Breakpoints`.
- Durante il debug, la riga corrente è evidenziata e si sposta a ogni step.
- Tab/Enter rispettano `MargineSinistro` (default 7).
- Un breakpoint su una riga **non eseguibile** non blocca o viene rimappato (coerenza con `LineToInstrMap == -1`).

### Problemi probabili

- **Mapping riga↔indice**: AvaloniaEdit usa righe **1-based**; `indRiga` del compilatore è **0-based**. Sbagliare l'offset di 1 sposta breakpoint ed evidenziazione di una riga. Definire la convenzione una volta e testarla.
- **Invalidazione del renderer**: senza `TextView.InvalidateLayer`/`InvalidateVisual` al cambio di `CurrentSourceLine`, l'evidenziazione "resta indietro". Collegare via `WhenAnyValue(x => x.CurrentSourceLine)`.
- **TextMate vs renderer custom**: l'highlighting TextMate e il `IBackgroundRenderer` lavorano su layer diversi; verificare l'ordine di disegno (il giallo della riga non deve coprire il testo).
- **AvaloniaEdit 12 API**: alcune firme (`AbstractMargin`, `VisualLine`) sono cambiate rispetto a esempi vecchi (11.x). Usare la documentazione 12.

---

## FASE 4 — Syntax highlighting (subset x86 reale)

**Obiettivo:** evidenziazione corretta del linguaggio EasyCPU.

### Passi

- File `.xshd` con il **set istruzioni reale** (vedi Appendice A — 35 opcode), i registri (`ax bx cx dx si di bp sp ip`), l'**indirizzamento indiretto** (`[bx] [bp] [si] [di]` con offset), commenti `//`, costanti char tra apici `'…'`, etichette `token:`, numeri decimali ed esadecimali (suffisso `h`), e il marcatore sezione dati `.DATA`.
- Registrare l'`.xshd` come `EmbeddedResource` e caricarlo all'avvio dell'editor.

### Verifica

- Tutti e 35 gli opcode si colorano; nessuna istruzione "dimenticata" (confronto con Appendice A).
- `//` colora il commento fino a fine riga; `'A'` è trattato come costante char; le etichette e gli indirizzi indiretti sono riconosciuti.

### Problemi probabili

- **Set istruzioni incompleto**: il piano originale (§4) elencava gli opcode a memoria. La fonte autoritativa è `Parser.SetCode` (Appendice A). Mancano facilmente `movs`, `jcxz`, `pushf`, `popf`.
- **Distinzione dati/codice**: il file ha due sezioni separate da `.DATA`; l'highlighting non distingue le sezioni — accettabile, ma documentarlo.
- **Numeri hex**: il parser usa `HexToInt`/suffisso `h`; allineare la regex `.xshd` a quel formato, non a `0x…`.

---

## FASE 5 — Pannelli dockabili (contenuto)

**Obiettivo:** Registri, Memoria, Stack, Errori popolati e reattivi.

### Passi

- `RegistersView`: 9 registri `AX..IP` + flag `Z/S/O`. Riusare `Cpu.DumpRegs()` (formato già pronto) o esporre proprietà tipizzate.
- `MemoryView`: dump `Cpu.DumpMemoria(0, Ram.INDIRIZZO_STACK, colonne)` (indirizzi 0–239).
- `StackView`: dump `Cpu.DumpMemoria(Ram.INDIRIZZO_STACK, Ram.MASSIMO_INDIRIZZO + 1, Ambiente.ColonneStack)` (240–255).
- `ErrorsView`: `DataGrid` di `CompilerError` (usa `Riga+1` per il display, vedi `CompilerError.ToString("T")`); doppio click → posiziona cursore su riga/colonna.
- Aggiornare tutti i pannelli **dopo ogni step/run** (un metodo `RefreshDebugViews()` chiamato dal `MainViewModel`).

### Verifica

- Eseguendo passo-passo, registri/memoria/stack cambiano coerentemente con `EasyCpu.Win` sullo stesso sorgente (confronto a vista).
- Doppio click su un errore porta alla riga giusta.

### Problemi probabili

- **Formato dati Dec/Hex/Car**: dipende da `Ambiente.FormatoDati` (e dalle stringhe `FI/FD/FR`). Il toggle deve rigenerare i dump. Attenzione: in `DumpReg` le etichette `[HEX]/[DEC]` sembrano invertite nel codice originale — decidere se replicare il bug o correggerlo (e documentarlo).
- **Stack range**: lo stack vive in 240–255 e cresce verso il basso; la `StackView` va letta dall'alto (240) verso SP. Non invertire.
- **Reattività**: se i dump sono stringhe ricalcolate, serve sollevare `PropertyChanged`/usare `ObservableCollection`; altrimenti la UI non si aggiorna dopo lo step.

---

## FASE 6 — Dialog e gestione file

**Obiettivo:** Opzioni, finestra loop infinito, apri/salva cross-platform.

### Passi

- **`SettingsViewModel` (sorgente osservabile delle opzioni, singleton)**: ViewModel che espone come proprietà osservabili le opzioni configurabili, con write-through su `Ambiente.*`. Tutta la UI fa binding su questo, non su `Ambiente`. **Obbligatorio**: `Ambiente` è fatto di campi `static` senza notifiche. Vedi sotto "Gestione di `Ambiente` e delle opzioni in MVVM".
- `OpzioniWindow`: editing OK/Annulla su una **copia** del `SettingsViewModel`, non su `Ambiente`. Campi: `MaxNumErrori`, `FormatoDati`, `ColonneStack`, `InizializzaRegistri`, `LoopInfinito`, `MargineSinistro`, `FormatoCarZero` (chiavi in Appendice B).
- `SospendiWindow`: 3 pulsanti (Pausa / Continua / Arresta) su `CpuLoopException`.
- Apri/Salva con `IStorageProvider` (Avalonia 12, cross-platform) al posto di `OpenFileDialog`.
- **Nuovo formato file come default + apertura del legacy `.as`** (vedi sotto "Formato file").

### Gestione di `Ambiente` e delle opzioni in MVVM

`Ambiente` è una classe a soli **campi statici pubblici** (es. `public static int MaxNumErrori;`), senza proprietà né `INotifyPropertyChanged`: Avalonia non può fare binding bidirezionale affidabile su campi statici. Inoltre il **core legge le opzioni staticamente** (`Cpu.DumpReg`/`DumpMemoria` usano `Ambiente.FormatoDati`, `Ambiente.FI/FD/FR`): non possiamo eliminare `Ambiente` senza riscrivere il core. Soluzione a basso impatto, pienamente MVVM:

**`SettingsViewModel` — sorgente osservabile unica delle opzioni (singleton).**

1. `SettingsViewModel : ObservableObject` (classe `partial`, CommunityToolkit) con una proprietà `[ObservableProperty]` per ogni opzione configurabile (vedi Appendice B): `FormatoDati`, `FormatoCarZero`, `MaxNumErrori`, `ColonneStack`, `InizializzaRegistri`, `LoopInfinito`, `MargineSinistro`, `MostraMemoria`, font/zoom, `PienoSchermo`.
2. È un **singleton** creato all'avvio (Fase 2, composizione dell'app) e iniettato in `MainViewModel` e nei pannelli che ne dipendono. Tutta la UI fa binding su questo VM, **non** su `Ambiente`.
3. **Caricamento**: all'avvio `Storage.LeggiOpzioni()` popola `Ambiente`, poi `SettingsViewModel` si inizializza da `Ambiente.*`.
4. **Write-through verso il core**: ogni proprietà implementa il write-through nella callback parziale generata da CommunityToolkit (`partial void OnFormatoDatiChanged(FormatoValore value) => Ambiente.FormatoDati = value;`), così il core, che legge `Ambiente` staticamente, resta coerente. In particolare `FormatoDati` deve propagare a `Ambiente.FormatoDati` (che ricalcola `FI/FD/FR`).
5. **Reattività dei pannelli**: i pannelli che mostrano dati formattati (registri/memoria/stack) si sottoscrivono ai cambi rilevanti via `PropertyChanged` (es. `settings.PropertyChanged += (_, e) => { if (e.PropertyName is nameof(FormatoDati) or nameof(ColonneStack)) RefreshDebugViews(); };`) oppure `MainViewModel` chiama direttamente `RefreshDebugViews()` a ogni step/run.
6. **Persistenza**: `SettingsViewModel.Salva()` → `Storage.SalvaOpzioni()`.

**`OpzioniWindow` — editing con pattern OK/Annulla.**

7. La finestra opzioni **non** modifica direttamente il singleton: lavora su una **copia** (clona i valori in un `OptionsEditViewModel`, o usa un meccanismo di snapshot/rollback).
8. Su **OK**: applica i valori al `SettingsViewModel` singleton (che fa write-through su `Ambiente`) e chiama `Salva()`.
9. Su **Annulla**: scarta la copia, nessun effetto.

> Nota architetturale: questo mantiene `Ambiente` come **store di persistenza**
> (DTO statico) e introduce un livello MVVM osservabile sopra di esso. In una
> fase futura si potrà rendere `Ambiente` non statico iniettando le opzioni nel
> core (`Cpu`/`Compiler`), ma **non è richiesto ora** (principio di semplicità).

Verifica: cambiare `FormatoDati` da Dec a Hex e premere OK → i dump cambiano subito e l'opzione è persistita; premere Annulla → nessun cambiamento; riavviare l'app → le opzioni sono ripristinate.

### Formato file: nuovo formato di default + apertura del legacy

**Decisione presa:** si adotta **direttamente un nuovo formato** come default di
salvataggio; il vecchio formato testuale `.as` resta **apribile** (sola lettura
del formato, non più scritto).

Oggi `Storage.Salva`/`Apri` usano un unico formato testuale: righe di codice,
marcatore `.DATA`, righe dati. Nuovo assetto:

1. Definire un'astrazione `ISourceSerializer`:
   `void Save(string path, SourceDocument doc)` e `SourceDocument Load(string path)`.
   `SourceDocument` incapsula `Codice` (righe), `Dati` (righe), `Breakpoints` (numeri di riga), e metadati (versione formato, eventualmente cursore).
2. **`EasyFileSerializer` (nuovo formato, default)**: un unico **JSON** con campo
   `formatVersion`, `code[]`, `data[]`, `breakpoints[]`. Estensione dedicata
   **`.asj`** per distinguerlo dal legacy. (L'estensione è una convenzione: se ne
   preferisci un'altra, è l'unico punto da cambiare.)
3. **`LegacyAsSerializer` (solo lettura)**: legge il vecchio formato testuale
   `.as`. **Non** viene usato per salvare: aprendo un `.as` legacy e poi salvando,
   il file viene scritto nel nuovo formato (con il nuovo nome/estensione,
   chiedendo conferma all'utente per non sovrascrivere il `.as` originale).
4. **Autodetect in lettura**: riconoscere il formato dal contenuto/estensione
   (prima riga non vuota che inizia con `{` → JSON nuovo; altrimenti legacy),
   così "Apri" gestisce indifferentemente `.asj` e vecchi `.as`.
5. `Storage` diventa una **facciata**: in scrittura usa sempre
   `EasyFileSerializer`; in lettura sceglie il serializer via autodetect.
6. Aggiornare il filtro file dei dialog (`FilePickerFileType`): salvataggio →
   solo nuovo formato (`*.asj`); apertura → nuovo + legacy (`*.asj;*.as`).
   Adeguare di conseguenza `Ambiente.FiltroFileDialog`/`NomeNuovoFile`.

Verifica: salvare un programma → file nel nuovo formato; riaprirlo → codice,
dati e breakpoint identici; aprire un vecchio `.as` legacy → si apre
correttamente; "salva" di un legacy → propone il nuovo formato senza
sovrascrivere il `.as` di partenza.

### Verifica

- Cambiare un'opzione e riaprire l'app → opzione persistita (file `.opt`).
- Provocare un loop infinito (es. `jmp` su sé stesso, default `LoopInfinito=65535`) → compare `SospendiWindow`.
- Apri/Salva funzionano su desktop **e** (almeno apri) su browser/mobile.

### Problemi probabili

- **`Ambiente` è tutto `static` con campi pubblici (non proprietà, niente `INotifyPropertyChanged`)**: il binding bidirezionale diretto in `OpzioniWindow` **non funziona** come ci si aspetta. Soluzione: un `OptionsViewModel` wrapper con proprietà osservabili che legge/scrive `Ambiente.*` su OK. Da pianificare, non è banale.
- **Persistenza path**: se non si è corretta `Ambiente` (Fase 1, punto 6), il salvataggio opzioni fallisce o scrive in path Windows su macOS/Linux.
- **`IStorageProvider` su WASM**: l'accesso ai file nel browser è sandboxed (File System Access API): "salva su disco" funziona diversamente; i **file recenti per path** non hanno senso nel browser. Prevedere un fallback (download/upload, o storage del browser).
- **Cultura/formattazione numerica**: i dump usano `string.Format` con format string custom; verificare che la cultura non introduca separatori inattesi.

---

## FASE 7 — Persistenza e file recenti

**Obiettivo:** layout Dock persistente, file recenti, opzioni salvate.

### Passi

- Salvare/caricare `layout.json` in `Environment.GetFolderPath(SpecialFolder.ApplicationData)/EasyCPU/` (con `Path.Combine`).
- Menu "File recenti" da `Ambiente.FileRecenti` (max `MAXFILERECENTI = 10`).
- **Persistenza breakpoint** (dettaglio sotto).

### Persistenza dei breakpoint

I breakpoint vivono come numeri di riga in `MainViewModel.Breakpoints`. Due strategie, scegliere in base al formato file:

- **Sidecar (default, indipendente dal formato)**: salvare un file accanto al sorgente, `nomefile.as.bkpt`, con un numero di riga per riga. Si salva quando i breakpoint cambiano o alla chiusura del file; si carica all'apertura del sorgente. Vantaggio: non tocca il formato `.as`, funziona anche col formato legacy.
- **Inline (solo formato JSON)**: se si usa il `JsonAsSerializer` (vedi Fase 6), includere `breakpoints[]` dentro `SourceDocument`. Vantaggio: un solo file; svantaggio: non disponibile col formato legacy.

Regole comuni:

1. Su WASM (browser) non c'è una path sidecar stabile: persistere i breakpoint nello storage del browser o dentro il documento JSON.
2. Caricamento **tollerante**: se il `.bkpt` è assente o disallineato (sorgente modificato fuori dall'IDE), non far crashare; ignorare le righe non valide.
3. Convertire sempre riga↔indice via `Compiler.LineToInstrMap` al momento di passarli alla CPU; non persistere gli indici istruzione (cambiano a ogni compilazione), ma i **numeri di riga**.
4. Quando l'utente salva-con-nome o cambia formato, rigenerare/spostare anche il sidecar.

### Verifica

- Chiudere/riaprire: layout, ultimo file e breakpoint ripristinati.
- File recenti aggiornato e deduplicato (`AggiungiRecenti` sposta in cima).

### Problemi probabili

- **Layout incompatibile** dopo modifiche ai ViewModel: un `layout.json` vecchio può far crashare la deserializzazione. Prevedere try/catch → fallback al layout di default + versionamento del file.
- **`.bkpt` disallineato** se il sorgente è stato modificato fuori dall'IDE: i numeri di riga non corrispondono più. Accettabile, ma non far crashare il caricamento.
- **`MAXFILERECENTI` non applicato**: nel codice attuale il troncamento della lista è **commentato** (`AggiungiRecenti`), quindi la lista cresce all'infinito. Decidere se ripristinare il limite.

---

## Motore di debug — dettaglio tecnico (criticità n.1)

Questa è la parte più facile da sbagliare. Riferimento completo in `README.reviewed.md` §2.

**Fatti del simulatore (verificati):**

- Memoria 0–255; area stack 240–255; `SP` iniziale = 256 (`MASSIMO_INDIRIZZO + 1`).
- **Stack discendente**: `push` fa `sp--`, `pop` fa `sp++`, `call`→push, `ret`→pop.
- `ip` viene incrementato **alla fine** di `Execute()`. Dopo uno step, `curIstruzione` è l'istruzione **appena eseguita**; quella **da eseguire** è `code[ip]`.

**Regole corrette:**

- **Step Over**: se `code[ip].Code == "call"`, salva `S = sp`, esegui la call, poi continua **finché `sp < S`** (sei dentro la subroutine); ti fermi quando `sp` torna a `S`. Altrimenti = Step Into.
- **Step Out**: salva `S = sp`, esegui **finché `sp <= S`**; ti fermi quando un `ret` porta `sp` a `S+1`.
- In entrambi i loop: controlla `Breakpoints.Contains(ip)` a ogni ciclo, gestisci `IPOverRun` e il loop infinito come in `Run()`.

**Test di non-regressione del debug (obbligatori):**

```
; step_over_call: SP deve tornare uguale prima/dopo
        mov ax, 0
        call inc_ax      ; <-- Step Over qui: esegue tutta la subroutine
        stop
inc_ax: inc ax
        ret
```

Atteso: dopo lo Step Over sul `call`, `AX==1`, `IP` punta a `stop`, e `SP` è
identico al valore che aveva prima del `call`. Se `SP` è diverso o l'IP è finito
nella subroutine, la logica è invertita.

---

## Aspetti cross-platform da sorvegliare (trasversale)

| Aspetto | Rischio | Mitigazione |
|---|---|---|
| Path con `\\` hardcoded in `Ambiente.cs` | Salvataggi rotti su macOS/Linux | `Path.Combine` / `DirectorySeparatorChar` (Fase 1.6) |
| `using System.Drawing` in `Storage.cs` e `Ambiente.cs` | `System.Drawing.Common` non è cross-platform | Rimuoverli (sono residui dopo la rimozione di `Rectangle`/`Font`) |
| `OpenFileDialog`/WinForms | Non esiste fuori Windows | `IStorageProvider` Avalonia |
| Accesso file su WASM | Sandbox browser | Fallback download/upload, niente path persistenti |
| Font `Courier new` (default in `Ambiente`) | Potrebbe non esistere su tutte le piattaforme | Font monospace di fallback / Inter già inclusa |
| Workload android/ios/wasm | Build `.sln` fallisce se mancano | `.slnf` per lo sviluppo desktop quotidiano |

---

## Segnali che qualcosa non va (red flags)

- Step Over/Out non si fermano o entrano nella subroutine → **verso stack invertito** o si guarda `curIstruzione` invece di `code[ip]`.
- Due esecuzioni "si ricordano" lo stato precedente → campi `static` dimenticati nel core.
- Breakpoint o riga evidenziata sfasati di una riga → confusione 0-based (compiler) vs 1-based (AvaloniaEdit).
- `NU1605` in build → versioni Avalonia non allineate tra head/pacchetti.
- Opzioni non si salvano su Mac/Linux → path Windows non corrette.
- L'editor compare senza colori/stile → `StyleInclude` AvaloniaEdit o `.xshd` non caricati.
- Pannelli "Not Found" → ViewLocator/DataTemplate non registrati.
- `OpzioniWindow` non aggiorna `Ambiente` → binding su campi statici senza wrapper VM.

---

## Matrice di verifica finale (gate per dichiarare "fatto")

| # | Gate | Come verificare |
|---|---|---|
| 1 | Soluzione compila, 0 NU1605 | `dotnet build` |
| 2 | Core testato | `dotnet test` verde (Step Into/Over/Out, breakpoint, mapping) |
| 3 | Desktop avvia e apre/salva `.as` | run manuale |
| 4 | Debug end-to-end | breakpoint + step su un programma con `call`, confronto con `EasyCpu.Win` |
| 5 | Pannelli reattivi | registri/memoria/stack/errori coerenti a ogni step |
| 6 | Persistenza | layout + recenti + breakpoint sopravvivono al riavvio |
| 7 | Cross-platform | opzioni si salvano su macOS/Linux; Desktop + (almeno) Browser girano |
| 8 | Opzioni in MVVM | `SettingsViewModel` osservabile; cambio `FormatoDati` OK/Annulla funziona e persiste |
| 9 | Formato file | salva nel **nuovo** formato di default; apre `.asj` e vecchi `.as` (autodetect); round-trip senza perdite |
| 10 | Breakpoint persistenti | i breakpoint sopravvivono a chiusura/riapertura del file |

---

## Appendice A — Set istruzioni reale (`Parser.SetCode`)

35 opcode (numero operandi tra parentesi):

`mov`(2) `movs`(0) `add`(2) `sub`(2) `cmp`(2) `and`(2) `or`(2) `xor`(2)
`not`(1) `neg`(1) `mul`(1) `div`(1) `inc`(1) `dec`(1) `push`(1) `pop`(1)
`pushf`(0) `popf`(0) `call`(1) `jcxz`(1) `je`(1) `jg`(1) `jl`(1) `jle`(1)
`jge`(1) `jmp`(1) `jne`(1) `jo`(1) `jno`(1) `js`(1) `jns`(1) `ret`(0)
`nop`(0) `stop`(0) `shl`(2) `shr`(2)

Registri: `ax bx cx dx si di bp sp ip`.
Indirizzamento: diretto, costante, memoria, etichetta, indiretto `[si] [di] [bx] [bp]` (con offset).
Flag: `ZF`(1) `SF`(2) `OF`(4).

## Appendice B — Opzioni utente (`Ambiente`)

| Proprietà | Chiave file `.opt` | Default |
|---|---|---|
| `FormatoDati` | `FORMATO_DATI` | `Dec` |
| `FormatoCarZero` | `FORMATO_CAR_ZERO` | `\0` |
| `MaxNumErrori` | `MAX_ERRORI` | 5 |
| `ColonneStack` | `COLONNE_STACK` | 1 |
| `InizializzaRegistri` | `INIZIALIZZA_REGISTRI` | true |
| `LoopInfinito` | `LOOP_INFINITO` | 65535 |
| `MargineSinistro` | `MARGINE_SINISTRO` | 7 |
| `MostraMemoria` | `MOSTRA_MEMORIA` | true |
| `FontEditorNome/Size/Style/Zoom` | `FONT_EDITOR_*` | Courier new / 14 / 0 / 1.0 |
| `PienoSchermo` | `PIENOSCHERMO` | false |

## Appendice C — Costanti memoria e formato file

- `Ram.MASSIMO_INDIRIZZO = 255`, `Ram.INDIRIZZO_STACK = 240`. SP iniziale = 256.
- File sorgente `.as`: righe di codice, poi marcatore `.DATA`, poi righe dati (gestito da `Storage.Apri/Salva`).
- Commenti `//` (troncati da `PreparaRiga`); costanti char tra apici `'…'`; etichette `nome:`.
- Sidecar breakpoint: `nomefile.as.bkpt` (un numero di riga per riga).

---

## Sequenza riassuntiva

```
Fase 0  Pulizia + Avalonia 12 + svuota demo   → build verde
Fase 1  Core a istanze + breakpoint + path fix → dotnet test verde
Fase 2  Dock + ViewModel                        → shell dockabile
Fase 3  Editor + margine + renderer debug       → breakpoint cliccabili, riga evidenziata
Fase 4  Syntax highlighting (.xshd)             → 35 opcode colorati
Fase 5  Pannelli (reg/mem/stack/errori)         → reattivi a ogni step
Fase 6  Dialog + IStorageProvider               → opzioni/loop/apri-salva
Fase 7  Persistenza (layout/recenti/bkpt)       → tutto sopravvive al riavvio
```
