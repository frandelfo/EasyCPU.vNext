# EasyCPU.vNext — Runbook per Claude Code

> Documento operativo **per l'agente** (Claude Code). Lo sviluppatore avvia
> **una fase per volta**; tu esegui solo la fase richiesta, ti fermi al gate di
> verifica e riporti. Non procedere alla fase successiva senza istruzione.
>
> Documenti di riferimento (leggili sempre prima di iniziare):
> - `MigrationPlan/GUIDA-SVILUPPO.md` — runbook umano con fasi, verifiche, problemi.
> - `MigrationPlan/README.reviewed.md` — analisi corretta, pseudo-codice debug.
> - `CLAUDE.md` (root repo) — regole di comportamento da rispettare SEMPRE.

---

## 1. Regole di ingaggio (override su tutto il resto)

Dal `CLAUDE.md` del repo, vincolanti:

1. **Pensa prima di scrivere**: dichiara le assunzioni; se ci sono più interpretazioni, fermati e chiedi invece di scegliere in silenzio.
2. **Semplicità**: il minimo codice che risolve il problema. Niente astrazioni speculative, niente feature non richieste.
3. **Modifiche chirurgiche**: tocca solo ciò che serve alla fase corrente. Non "migliorare" codice adiacente, non riformattare, non rinominare a caso. Ogni riga modificata deve tracciare alla richiesta.
4. **Esecuzione guidata da obiettivi**: trasforma il compito in criteri verificabili (test), poi itera finché passano.

Regole aggiuntive specifiche di questo progetto:

5. **Una fase alla volta.** Non anticipare lavoro di fasi successive. Se noti qualcosa che riguarda un'altra fase, **annotalo** nel report, non implementarlo.
6. **Fermati al gate.** Quando la sezione "Verifica" della fase è verde, **non continuare**: riassumi cosa hai fatto, l'output dei comandi di verifica, e attendi il via per la fase successiva.
7. **Niente git** (commit/add/push/reset/checkout), niente release, niente comandi distruttivi senza richiesta esplicita: ai commit pensa lo sviluppatore (vedi §6).
8. **Se un gate fallisce e non riesci a risolverlo in modo pulito**, fermati e riporta il problema con ipotesi e opzioni, invece di accumulare workaround.
9. **Non disabilitare warning o test** per far passare la build. `NU1605` è errore: va risolto allineando le versioni, non sopprimendolo.

---

## 2. Mappa del progetto

Radice soluzione: `VS Solution/` (contiene `EasyCPU.vNext.slnx`).

```
VS Solution/
├── EasyCpu.Common/        Ambiente (opzioni), Errori, Global, enum FormatoValore
├── EasyCpu.Assembler/     Cpu, Compiler, Parser, Ram, Instruction, Exceptions   ← core
├── EasyCpu.Backend/       Storage (I/O file)
├── EasyCPU.vNext/         UI condivisa Avalonia (oggi = demo AvaloniaEdit da svuotare)
├── EasyCPU.vNext.Desktop/ entry desktop
├── EasyCPU.vNext.Browser/ entry WASM
├── EasyCPU.vNext.Android/  entry Android
├── EasyCPU.vNext.iOS/      entry iOS
└── (da rimuovere in Fase 0) AvaloniaEdit/, AvaloniaEdit.TextMate/, EasyCpu.Core/,
                              EasyCpu.Win/, EasyCpu.Win.Controls/
```

`EasyCpu.Win/` resta in git come **riferimento di comportamento** anche dopo la rimozione dalla `.sln`: consultalo per replicare le funzioni, non modificarlo.

---

## 3. Verità tecniche da non sbagliare

Queste sono già verificate sul sorgente. Non re-inferirle: usale.

- **TFM**: tutti `net10.0` (head con suffisso piattaforma). SDK pin: `global.json` → 10.0.101.
- **Versioni target**: Avalonia `12.0.5`, AvaloniaEdit `12.0.0` (pacchetto `Avalonia.AvaloniaEdit`), TextMate `AvaloniaEdit.TextMate 12.0.0`, Dock `12.0.0.2` (con `Dock.Model.Mvvm`), `CommunityToolkit.Mvvm 8.4.0`. **Non si usa ReactiveUI.** Allinea TUTTO a 12.0.5 per evitare NU1605.
- **Stack discendente**: SP iniziale = 256 (`Ram.MASSIMO_INDIRIZZO+1`); `push` → `sp--`, `pop` → `sp++`; `call` push, `ret` pop.
- **Fetch/Execute**: `ip++` avviene a fine `Execute()`. Dopo uno step, `curIstruzione` è l'istruzione **già eseguita**; quella da eseguire è `code[ip]`.
- **Righe**: il compilatore usa indici **0-based** (`indRiga`); AvaloniaEdit usa righe **1-based**. Converti sempre, una sola volta, in un punto noto.
- **Memoria**: 0–255; stack 240–255 (`INDIRIZZO_STACK=240`).
- **Set istruzioni autoritativo**: `Parser.SetCode` (35 opcode — vedi Appendice A di `GUIDA-SVILUPPO.md`). Non fidarti di elenchi a memoria.
- **Breakpoint**: nuovo `HashSet<int> Breakpoints` su indici istruzione; il vecchio `Trap`/`SetTrap`/`rigaTrap`/`Instruction.Trap` va rimosso. `CpuTrapException`/`CpuLoopException` restano.
- **Cross-platform**: `Ambiente.cs` ha path con `\\` hardcoded e `using System.Drawing` residuo; `Storage.cs` idem (`System.Drawing`). Vanno corretti (Fase 1).
- **`Ambiente`**: campi `static` pubblici senza notifiche, **e il core li legge staticamente**. Si gestisce in MVVM con un `SettingsViewModel` singleton osservabile con write-through su `Ambiente` (Fase 6); `Ambiente` resta come store di persistenza. Non rendere `Ambiente` non-statico in questa fase.
- **Formato file (deciso)**: il **default di salvataggio è un nuovo formato** (`EasyFileSerializer`, JSON, estensione `.asj`); i vecchi `.as` testuali restano **apribili** (`LegacyAsSerializer`, sola lettura) con autodetect. Non scrivere più nel formato legacy.

---

## 4. Protocollo di verifica (eseguilo a ogni fase)

Comandi (esegui dalla cartella `VS Solution/`):

```bash
dotnet --version                                   # atteso: 10.0.101.x
dotnet restore EasyCPU.vNext.slnx
dotnet build EasyCPU.vNext.slnx -c Debug            # 0 errori, 0 NU1605
dotnet test                                        # dalla Fase 1 in poi
dotnet run --project EasyCPU.vNext.Desktop         # verifica avvio app (manuale)
```

Se mancano i workload mobile/wasm e bloccano la `.sln`, lavora con una solution
filter sui soli progetti rilevanti (Desktop + UI + business logic) e segnalalo.

Per ogni fase, nel report finale includi: comando eseguito + esito sintetico
(es. "build: 0 errori"; "test: 7 passed"). Niente claim non verificati.

---

## 5. Ordini di lavoro per fase

Per ciascuna fase: **fai solo questa**, rispetta la "Definition of Done", fermati al gate.
I dettagli completi (passi puntuali, problemi noti) sono in `GUIDA-SVILUPPO.md`: leggi la fase corrispondente prima di iniziare.

### Fase 0 — Pulizia, versioni, setup
- **Tocca**: `global.json` (nuovo), `*.csproj` di tutti gli head, `Directory.Build.props`, `App.xaml`, `EasyCPU.vNext` (svuota demo), `.sln`.
- **Non toccare**: il core (`Assembler`/`Common`/`Backend`) — solo in Fase 1.
- **DoD**: progetti morti/legacy rimossi; tutto su Avalonia 12.0.5; demo AvaloniaEdit svuotato; soluzione compila.
- **Gate**: `dotnet build` 0 errori, 0 NU1605; `dotnet run` Desktop apre una finestra-scheletro.
- **Attenzione**: `App.xaml` ha uno `StyleInclude` `avares://AvaloniaEdit/...` — verifica che risolva ancora col NuGet; `.Desktop.csproj` ha un `ProjectReference` duplicato da rimuovere.

### Fase 1 — Core a istanze + breakpoint + fix cross-platform ✅ COMPLETATA (2026-06-28)
- **Tocca**: `EasyCpu.Assembler/*` (Cpu, Compiler, Parser, Instruction), `EasyCpu.Backend/Local/Storage.cs`, `EasyCpu.Common/Ambiente.cs`; nuovo progetto `EasyCpu.Assembler.Tests`.
- **Non toccare**: la UI.
- **DoD**: core istanziabile; `HashSet<int> Breakpoints`; `StepOver/StepOut/RunWhileInside` corretti (stack discendente, guarda `code[ip]`); `InstrToLineMap`+`LineToInstrMap`; rimossi `Trap/SetTrap/rigaTrap`; path con `Path.Combine`; rimossi `using System.Drawing`.
- **Gate**: `dotnet test` verde con i test elencati in `GUIDA-SVILUPPO.md` Fase 1 (Step Into/Over/Out, breakpoint multipli, mapping righe, isolamento istanze).
- **Attenzione**: l'errore classico è invertire il verso dello stack — usa il test `step_over_call` come prova del nove.
- **Assunzioni registrate** (dettagli in `GUIDA-SVILUPPO.md` §Fase 1 → "Assunzioni e decisioni"):
  1. Dopo `CpuTrapException`, la UI deve chiamare `StepInto()` prima di `Run()` (non `Run()` direttamente).
  2. `RunWhileInside(limite)` usa `sp < limite`; `StepOut` passa `sp+1`.
  3. Marker commento `//` confermato in `PreparaRiga`; `'` serve solo per costanti char.
  4. Indici core 0-based; conversione a 1-based (AvaloniaEdit) va fatta solo nella UI in Fase 3.

### Fase 2 — Dock + ViewModel ✅ COMPLETATA (2026-06-28)
- **Tocca**: `EasyCPU.vNext/` (DockFactory, ViewModel dei pannelli, MainViewModel, registrazione DataTemplate, serializer layout).
- **DoD**: shell con pannelli dockabili (anche vuoti) basata su `Dock.Model.Mvvm`; il serializer layout non lancia.
- **Gate**: app avvia, pannelli agganciabili/spostabili.
- **Attenzione**: non aggiungere `Dock.Model.ReactiveUI` o `ReactiveUI.Avalonia`; includi `Dock.Avalonia.Themes.Fluent`; classi VM `partial` per i source generator CommunityToolkit.
- **Assunzioni registrate** (dettagli in `GUIDA-SVILUPPO.md` §Fase 2 → "Assunzioni e decisioni"):
  1. `IRootDock` è in `Dock.Model.Controls`, non `Dock.Model.Core` (che contiene solo `IDockable`/`IFactory`). Aggiungere `using Dock.Model.Controls;` in tutti i file che usano `IRootDock`.
  2. Il tema Dock si include via classe XAML: `<dockTheme:DockFluentTheme />` (`Dock.Avalonia.Themes.Fluent.DockFluentTheme`), non via `StyleInclude`.
  3. `DockControl` in `Dock.Avalonia.Controls`, assembly `Dock.Avalonia`. Richiede sia `Layout` che `Factory` bindati (senza `Factory`, il drag/drop non funziona).
  4. `Factory.CreateLayout()` è virtuale — override obbligatorio. Le factory methods (`CreateRootDock`, `CreateProportionalDock`, ecc.) vanno richiamate sull'istanza per creare i tipi Mvvm corretti.
  5. `SettingsViewModel`: singleton con costruttore privato, inizializzato da `Ambiente.*` dopo `Ambiente.Inizializza()` in `App.OnFrameworkInitializationCompleted`. Il write-through verso `Ambiente` (callback `partial void OnXxxChanged`) è rimandato a Fase 6.
  6. `MainWindowViewModel.cs` è stato riusato (classe rinominata a `MainViewModel`); il file non è stato spostato.
  7. Aggiunta `#nullable enable` nei file che usano annotazioni nullable (`?`) per evitare CS8632.

### Fase 3 — Editor con debug
- **Tocca**: `EasyCPU.vNext/` (BreakpointMargin, DebugCurrentLineRenderer, CodeEditorView, logica EasyEditor su AvaloniaEdit).
- **DoD**: margine breakpoint cliccabile collegato a `MainViewModel.Breakpoints`; evidenziazione riga corrente; Tab/Enter rispettano `MargineSinistro`.
- **Gate**: toggle breakpoint funziona; riga corrente si muove a ogni step; verifica offset 0-based/1-based corretto.

### Fase 4 — Syntax highlighting
- **Tocca**: nuovo `.xshd` (EmbeddedResource) + caricamento nell'editor.
- **DoD**: tutti i 35 opcode di `Parser.SetCode`, registri, indiretti `[bx]/[bp]/[si]/[di]`, commenti `//`, char `'…'`, etichette `:`, hex con suffisso `h`, marcatore `.DATA`.
- **Gate**: nessun opcode non colorato (confronto con Appendice A).

### Fase 5 — Pannelli (contenuto)
- **Tocca**: `RegistersView/MemoryView/StackView/ErrorsView` + `RefreshDebugViews()` nel MainViewModel.
- **DoD**: dump reattivi a ogni step; riusa `Cpu.DumpRegs`/`DumpMemoria`; doppio click errore → riga.
- **Gate**: confronto a vista con `EasyCpu.Win` sullo stesso sorgente.

### Fase 6 — Dialog, opzioni (MVVM), formato file
- **Tocca**: `SettingsViewModel` (singleton, creato in Fase 2) + `OpzioniWindow`, `SospendiWindow`, integrazione `IStorageProvider`, astrazione `ISourceSerializer` con `EasyFileSerializer` (nuovo, default) e `LegacyAsSerializer` (sola lettura) in `EasyCpu.Backend`; adeguare `Ambiente.FiltroFileDialog`/`NomeNuovoFile`.
- **DoD**:
  - `SettingsViewModel` osservabile come sorgente unica delle opzioni, con write-through su `Ambiente.*` (il core legge `Ambiente` staticamente); `OpzioniWindow` edita una **copia** con OK/Annulla; su OK applica + `Storage.SalvaOpzioni()` + refresh pannelli.
  - Salvataggio sempre nel **nuovo formato**; apertura con autodetect di `.asj` (nuovo) e `.as` (legacy); salvare un legacy aperto propone il nuovo formato senza sovrascrivere l'originale.
  - Apri/Salva con `IStorageProvider`; loop infinito → `SospendiWindow`.
- **Gate**: cambio `FormatoDati` OK/Annulla ok e persistito (anche dopo riavvio); salva nuovo formato + apri legacy ok; round-trip senza perdite.
- **Attenzione**: niente binding diretto su `Ambiente` (campi statici); non rendere `Ambiente` non-statico ora. WASM: file sandboxed, recenti-per-path non validi → fallback.

### Fase 7 — Persistenza
- **Tocca**: salvataggio/caricamento `layout.json`, file recenti, persistenza breakpoint (sidecar `.as.bkpt` o inline nel formato JSON).
- **DoD**: layout + recenti + breakpoint sopravvivono al riavvio; caricamento tollerante a file assenti/disallineati; persisti **numeri di riga**, non indici istruzione.
- **Gate**: chiudi/riapri → tutto ripristinato; `layout.json` vecchio non fa crashare (fallback al default).

---

## 6. Commit — NON automatici

- **Non fare commit in automatico.** Ai commit ci pensa lo sviluppatore, volta per volta.
- A fine fase lascia il working tree con le modifiche **non committate**; nel report indica i file toccati così che lo sviluppatore possa revisionarli e committare lui.
- Non fare `git add`/`git commit`/`git push`/`git reset`/`git checkout` né altri comandi git che alterano lo stato, salvo richiesta esplicita.
- Se serve un messaggio di commit, **proponilo nel report** (in italiano, imperativo, con prefisso fase, es. `fase1: Cpu istanziabile + SP pubblico`) ma non eseguirlo.

---

## 7. Quando fermarti e chiedere

Interrompi e chiedi allo sviluppatore (non decidere da solo) se:

- Un'API Avalonia/Dock/AvaloniaEdit 12 differisce dagli esempi e ci sono più modi ragionevoli di adattarla.
- Una scelta cambia lo **schema di persistenza** oltre il formato già deciso (nuovo `.asj` di default + lettura legacy `.as`), o tocca la compatibilità dei dati utente in modo non previsto.
- Per far passare un gate servirebbe modificare codice fuori dallo scope della fase.
- Emerge un comportamento del core che diverge da `EasyCpu.Win` e non è chiaro quale sia quello voluto (es. il possibile bug etichette `[HEX]/[DEC]` in `DumpReg`).
- Servono workload/SDK non presenti, o un head non compila per ragioni d'ambiente.

In tutti gli altri casi procedi con l'opzione più semplice e conforme a `CLAUDE.md`, dichiarando l'assunzione nel report.

---

## 8. Formato del report di fine fase

Alla fine di ogni fase, produci:

1. **Cosa è stato fatto** (elenco puntuale, mappato ai DoD).
2. **File toccati** (path).
3. **Verifica**: comandi eseguiti + esito (build/test/run).
4. **Assunzioni fatte** e cose annotate per fasi successive.
5. **Stato del gate**: VERDE (pronto per la fase successiva) o BLOCCATO (con motivo e opzioni).

Poi **fermati** e attendi il via per la fase successiva.
