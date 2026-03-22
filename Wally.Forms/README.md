# Wally.Forms

WinForms desktop GUI for Wally — the AI Actor Environment. Provides a dark-themed, IDE-like interface for managing workspaces, editing actors/loops/wrappers/runbooks, chatting with AI actors, and running commands through an integrated terminal.

**Target:** .NET 8 (Windows) | **Type:** WinForms Application | **Dependencies:** `Wally.Core`

---

## Table of Contents

- [Quick Start](#quick-start)
- [Build & Run](#build--run)
- [UI Overview](#ui-overview)
- [Panels](#panels)
  - [File Explorer](#file-explorer)
  - [Document Tab Host](#document-tab-host)
  - [AI Chat Panel](#ai-chat-panel)
  - [Command Terminal](#command-terminal)
  - [Welcome Panel](#welcome-panel)
- [Tabbed Editors](#tabbed-editors)
  - [Actor Editor](#actor-editor)
  - [Loop Editor](#loop-editor)
  - [Wrapper Editor](#wrapper-editor)
  - [Runbook Editor](#runbook-editor)
  - [Config Editor](#config-editor)
  - [Log Viewer](#log-viewer)
- [Menu Structure](#menu-structure)
- [Keyboard Shortcuts](#keyboard-shortcuts)
- [Theming](#theming)
- [Architecture](#architecture)
- [Project Structure](#project-structure)

---

## Quick Start

1. **Launch** the application (`dotnet run --project Wally.Forms` or run the published exe)
2. **Setup a workspace:** File ? Setup New Workspace (Ctrl+Shift+N), select your codebase root
3. **Explore:** The file explorer (left), chat panel (right), and command terminal (bottom) appear
4. **Chat with actors:** Select an actor, model, and wrapper in the chat panel, type a prompt, and hit Send
5. **Run commands:** Type commands in the terminal (same commands as the CLI)
6. **Edit entities:** Double-click actors, loops, wrappers, or runbooks in the file explorer to open tabbed editors

---

## Build & Run

```sh
# Build
dotnet build Wally.Forms

# Run
dotnet run --project Wally.Forms

# Publish (Windows)
dotnet publish Wally.Forms -c Release

# Publish self-contained
dotnet publish Wally.Forms -c Release -r win-x64 --self-contained
```

---

## UI Overview

```
?????????????????????????????????????????????????????????????????????
?  Menu Bar: File · View · Editors · Workspace                    ?
?  ToolStrip: [Open] [Setup] [Save] [Refresh] [Reload] [Info]    ?
?             [Clear Chat] [Actors] [Config] [Logs]               ?
????????????????????????????????????????????????????????????????????
?           ?  ?? Welcome · ?? Engineer · ? …  ?                  ?
?  File     ??????????????????????????????????   AI Chat        ?
?  Explorer ?                                ?   Panel          ?
?  (left    ?  Tabbed Document Area          ?   (right panel)  ?
?   panel)  ?                                ?                  ?
?           ?  Welcome page when no tabs open ?   Actor dropdown ?
?           ?  Entity editors when tabs open  ?   Model dropdown ?
?           ?                                ?   Loop dropdown  ?
?           ?  · Actor editor (RBA prompts)  ?   Wrapper dropdown?
?           ?  · Loop editor (JSON fields)   ?   Loop checkbox  ?
?           ?  · Wrapper editor (CLI recipe) ?   Max iterations ?
?           ?  · Runbook editor (commands)   ?                  ?
?           ?  · Config editor (wally-config)?   Chat messages  ?
?           ?  · Log viewer (session logs)   ?   area           ?
?           ?                                ?                  ?
?           ?                                ?   Input + Send   ?
????????????????????????????????????????????????????????????????????
?  Command Terminal (bottom panel)                                 ?
?  Interactive REPL — same commands as CLI                         ?
?  wally> run "Review auth" -a Engineer                           ?
????????????????????????????????????????????????????????????????????
?  Status Bar: [WorkSource path] · [Actors: N] · [? session-id]  ?
????????????????????????????????????????????????????????????????????
```

**Panel visibility** is controlled via the View menu. All workspace panels (file explorer, chat, command terminal) appear automatically when a workspace is loaded.

---

## Panels

### File Explorer

Left-docked panel showing the workspace file tree rooted at the WorkSource directory.

- **Double-click** a file to open it:
  - `actor.json` ? opens the Actor editor tab
  - `Loops/*.json` ? opens the Loop editor tab
  - `Wrappers/*.json` ? opens the Wrapper editor tab
  - `Runbooks/*.wrb` ? opens the Runbook editor tab
  - `wally-config.json` ? opens the Config editor tab
  - Other files ? opens with the system's default application
- **F5** or toolbar Refresh button reloads the tree
- Shows the full directory tree structure of your codebase

### Document Tab Host

Centre panel that hosts tabbed documents. Provides:

- **Multiple tabs** — open several editors simultaneously
- **Tab switching** — click tabs or use Ctrl+W to close
- **Dirty tracking** — modified tabs show a visual indicator
- **Close actions** — close individual tabs, close all
- **Welcome tab** — always present (not closeable), shown as the landing page

### AI Chat Panel

Right-docked panel for interactive AI conversations:

- **Actor dropdown** — select which actor to use (or none for direct mode)
- **Model dropdown** — select or type a model identifier
- **Loop dropdown** — select a named loop definition (or none)
- **Wrapper dropdown** — select the LLM wrapper
- **Loop checkbox** — enable inline loop mode
- **Max iterations** — iteration limit for loops
- **Chat message area** — displays conversation history with the AI
- **Input area + Send** — type prompts and execute
- **Clear button** — clears the conversation

All options map directly to `run` command flags. The chat panel calls `WallyCommands.HandleRun` with the selected options.

### Command Terminal

Bottom-docked panel providing an integrated command REPL:

- **Same commands as the CLI** — `run`, `setup`, `load`, `list`, `info`, etc.
- **Tab completion** — for commands and entity names
- **Command history** — navigate with up/down arrows
- **Colored output** — themed output with status messages
- **Workspace change detection** — automatically refreshes panels when workspace state changes

### Welcome Panel

Landing page shown in the centre when no editor tabs are open:

- **Before workspace load:** Shows getting started instructions (File ? Open, File ? Setup)
- **After workspace load:** Shows workspace summary (name, actor count, default model)

---

## Tabbed Editors

Double-click workspace entities in the file explorer, or use the Editors menu:

### Actor Editor

Edit an actor's RBA prompts:

- **Name** — the actor identifier
- **Role** — who the AI is
- **Acceptance Criteria** — what success looks like
- **Intent** — what the actor aims to accomplish
- **Save/Revert** buttons with dirty tracking

### Loop Editor

Edit loop definition fields:

- Name, Description, Actor Name
- Start Prompt (multi-line)
- Continue Prompt Template (multi-line)
- Completed Keyword, Error Keyword
- Max Iterations
- Save/Revert buttons

### Wrapper Editor

Edit LLM wrapper definitions:

- Name, Description
- Executable, Argument Template
- Model Arg Format, Source Path Arg Format
- UseSourcePathAsWorkingDirectory toggle
- CanMakeChanges toggle
- Save/Revert buttons

### Runbook Editor

Edit `.wrb` runbook files:

- Description (first comment line)
- Command list (plain-text editor)
- Save/Revert buttons

### Config Editor

Edit `wally-config.json` settings:

- Folder names (Actors, Loops, Wrappers, Runbooks, Docs, Templates, Logs)
- Default model, wrapper, loop, runbook
- Available models, wrappers, loops, runbooks lists
- Selected (priority) lists
- Max iterations, log rotation
- Save/Revert buttons

### Log Viewer

Browse and inspect session logs:

- Session list with timestamps
- Log entry viewer with category filtering
- JSON-formatted structured log entries

---

## Menu Structure

### File Menu

| Item | Shortcut | Description |
|---|---|---|
| Open Workspace… | Ctrl+O | Browse for an existing `.wally/` folder |
| Setup New Workspace… | Ctrl+Shift+N | Create a new workspace |
| Save Workspace | Ctrl+S | Save config and actors to disk |
| Close Workspace | | Close the active workspace |
| Exit | | Close the application |

### View Menu

| Item | Description |
|---|---|
| Show Explorer | Toggle the file explorer panel |
| Show Chat | Toggle the AI chat panel |
| Show Terminal | Toggle the command terminal |
| Refresh | Refresh the file explorer (F5) |

### Editors Menu

| Item | Description |
|---|---|
| Edit Actors | Open an actor picker ? actor editor |
| Edit Loops | Open a loop picker ? loop editor |
| Edit Wrappers | Open a wrapper picker ? wrapper editor |
| Edit Runbooks | Open a runbook picker ? runbook editor |
| Edit Config | Open the config editor |
| View Logs | Open the log viewer |
| Close All Editors | Close all open editor tabs |

### Workspace Menu

| Item | Description |
|---|---|
| Reload Actors | Re-read actor folders from disk |
| List Actors | List actors in the terminal |
| Workspace Info | Show `info` output in the terminal |
| Verify Workspace | Run `setup --verify` |
| Cleanup Workspace | Delete `.wally/` (with confirmation) |
| Open Workspace Folder | Open `.wally/` in Windows Explorer |

---

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+O` | Open workspace |
| `Ctrl+Shift+N` | Setup new workspace |
| `Ctrl+S` | Save workspace |
| `` Ctrl+` `` | Focus command terminal |
| `Ctrl+1` | Focus file explorer |
| `Ctrl+2` | Focus chat panel |
| `Ctrl+3` | Focus command terminal |
| `Ctrl+W` | Close active editor tab |
| `F5` | Refresh file explorer |

---

## Theming

All UI controls use the centralized `WallyTheme` dark theme. The theme provides:

### Surface Hierarchy (darkest ? lightest)

| Token | Usage |
|---|---|
| `Surface0` | Primary background — deepest panels |
| `Surface1` | Secondary background — panel bodies |
| `Surface2` | Tertiary — toolbars, input rows |
| `Surface3` | Elevated — hover states, selected items |
| `Surface4` | Highest — active highlights |

### Text Hierarchy

| Token | Usage |
|---|---|
| `TextPrimary` | Primary text |
| `TextSecondary` | Secondary/descriptive text |
| `TextMuted` | De-emphasized text, hints |
| `TextDisabled` | Disabled controls |

### Fonts

| Token | Font |
|---|---|
| `FontUI` / `FontUIBold` | Segoe UI 9pt |
| `FontUISmall` / `FontUISmallBold` | Segoe UI 8.25pt |
| `FontMono` / `FontMonoBold` | Cascadia Mono 9.5/10pt |
| `FontMonoSmall` / `FontMonoLarge` | Cascadia Mono 9/10pt |

### Functional Colors

Minimal color accents — the theme is predominantly neutral gray:

| Token | Usage |
|---|---|
| `Accent` | Primary interactive element |
| `Green` | Success/positive states |
| `Red` | Error/destructive states |
| `Yellow` | Warning/running states |

The theme includes:
- `WallyToolStripRenderer` — themed renderer for all menus and toolstrips
- `DarkColorTable` — `ProfessionalColorTable` with dark theme overrides
- `ThemedSplitter` — splitter with hover highlight
- `WallyTheme.ApplyTo(control)` — helper to apply theme defaults to control trees

---

## Architecture

```
????????????????????????????????????????????????
?                 WallyForms                    ?
?              (Main Form)                      ?
?                                              ?
?  ?????????????  ????????????????  ????????????
?  ?FileExplorer?  ?DocumentTabHost?  ?ChatPanel??
?  ?  Panel    ?  ?  (Centre)    ?  ? (Right) ??
?  ?  (Left)   ?  ?  ?????????????  ?        ??
?  ?           ?  ?  ?Editors:  ??  ?        ??
?  ?  Tree view?  ?  ? Actor    ??  ? Actors ??
?  ?  of codebase? ?  ? Loop     ??  ? Models ??
?  ?           ?  ?  ? Wrapper  ??  ? Loops  ??
?  ?           ?  ?  ? Runbook  ??  ? Wrappers??
?  ?           ?  ?  ? Config   ??  ?        ??
?  ?           ?  ?  ? Logs     ??  ?        ??
?  ?           ?  ?  ?????????????  ?        ??
?  ?????????????  ????????????????  ????????????
?  ???????????????????????????????????????????  ?
?  ?         CommandPanel (Bottom)           ?  ?
?  ?         Interactive REPL terminal       ?  ?
?  ???????????????????????????????????????????  ?
?  ???????????????????????????????????????????  ?
?  ?         StatusStrip (Bottom bar)        ?  ?
?  ???????????????????????????????????????????  ?
????????????????????????????????????????????????
                       ?
              ???????????????????
              ? WallyEnvironment?  ? Shared with CommandPanel
              ?                 ?    and ChatPanel
              ???????????????????
```

**Key design decisions:**

1. **Single `WallyEnvironment`** — created once at startup, shared across all panels
2. **CommandPanel uses `WallyCommands.DispatchCommand`** — same dispatch as CLI and runbooks
3. **ChatPanel maps to `WallyCommands.HandleRun`** — dropdown selections map to `run` flags
4. **File explorer detects entity type** — double-clicking files intelligently opens the right editor
5. **Workspace gating** — panels and menus are enabled/disabled based on workspace state
6. **Auto-setup** — if a workspace exists at the exe directory, it's loaded automatically on launch

---

## Project Structure

```
Wally.Forms/
??? Wally.Forms.csproj              ? .NET 8 WinForms app
??? Program.cs                      ? Application entry point
??? Wally.Forms.cs                  ? Main form — layout, events, panel orchestration
??? Wally.Forms.Designer.cs         ? Designer-generated menu/toolbar/shortcut definitions
??? Theme/
?   ??? WallyTheme.cs              ? Centralized dark theme tokens, colors, fonts,
?                                      WallyToolStripRenderer, DarkColorTable,
?                                      ThemedSplitter
??? Controls/
    ??? FileExplorerPanel.cs        ? Left panel — workspace file tree
    ??? ChatPanel.cs                ? Right panel — AI chat with dropdowns
    ??? CommandPanel.cs             ? Bottom panel — interactive command REPL
    ??? WelcomePanel.cs             ? Centre landing page (no workspace / workspace loaded)
    ??? DocumentTabHost.cs          ? Tabbed document container (centre area)
    ??? SetupDialog.cs              ? Modal dialog for workspace setup
    ??? Editors/
        ??? ActorEditorPanel.cs     ? Edit actor RBA prompts
        ??? LoopEditorPanel.cs      ? Edit loop definitions
        ??? WrapperEditorPanel.cs   ? Edit LLM wrapper definitions
        ??? RunbookEditorPanel.cs   ? Edit runbook commands
        ??? ConfigEditorPanel.cs    ? Edit wally-config.json
        ??? LogViewerPanel.cs       ? Browse and inspect session logs
