# Wally.Forms — Quick Setup Guide

Get the Wally desktop GUI running in 2 minutes.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) — verify: `dotnet --version`
- **Windows 10/11** — WinForms requires the `net8.0-windows` target
- [GitHub CLI](https://cli.github.com/) with Copilot extension — verify: `gh copilot --version`
  - Install Copilot: `gh extension install github/gh-copilot`
  - *Or configure a different LLM wrapper after setup*

---

## Build & Run

```sh
cd Wally

# Build
dotnet build Wally.Forms

# Run
dotnet run --project Wally.Forms
```

Or publish and run the exe directly:

```sh
# Framework-dependent (requires .NET 8 runtime)
dotnet publish Wally.Forms -c Release

# Self-contained (no runtime needed)
dotnet publish Wally.Forms -c Release -r win-x64 --self-contained
```

---

## First Launch

### 1. Set Up a Workspace

**File ? Setup New Workspace** (or `Ctrl+Shift+N`)

Select your codebase root directory (e.g. `C:\repos\MyApp`). Wally creates a `.wally/` folder inside it with actors, loops, wrappers, templates, and config.

### 2. Explore the Interface

Once a workspace is loaded, three panels appear:

| Panel | Location | Purpose |
|---|---|---|
| **File Explorer** | Left | Browse your codebase file tree |
| **Document Tab Host** | Centre | Tabbed editors for actors, loops, wrappers, runbooks, config, logs |
| **AI Chat Panel** | Right | Chat with AI actors — select actor, model, loop, wrapper |
| **Command Terminal** | Bottom | Interactive REPL — same commands as the CLI |

### 3. Chat with an Actor

In the **Chat Panel** (right):

1. Select an **Actor** from the dropdown (e.g. `Engineer`)
2. Select a **Model** (or leave as default)
3. Select a **Wrapper** (e.g. `Copilot`)
4. Type a prompt: `Review the authentication module for security issues`
5. Click **Send**

### 4. Run Commands

In the **Command Terminal** (bottom), type any Wally command:

```
wally> run "What does this codebase do?"
wally> list
wally> info
wally> runbook full-analysis "Explain the architecture"
```

### 5. Edit Workspace Entities

**Double-click** files in the file explorer to open tabbed editors:

| File | Editor |
|---|---|
| `.wally/Actors/<Name>/actor.json` | Actor editor — edit RBA prompts |
| `.wally/Loops/<Name>.json` | Loop editor — edit iteration settings |
| `.wally/Wrappers/<Name>.json` | Wrapper editor — edit CLI recipe |
| `.wally/Runbooks/<Name>.wrb` | Runbook editor — edit command sequences |
| `.wally/wally-config.json` | Config editor — edit workspace settings |

Or use the **Editors** menu to pick from loaded entities.

---

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+O` | Open an existing workspace |
| `Ctrl+Shift+N` | Setup a new workspace |
| `Ctrl+S` | Save workspace |
| `` Ctrl+` `` | Focus command terminal |
| `Ctrl+1` | Focus file explorer |
| `Ctrl+2` | Focus chat panel |
| `Ctrl+3` | Focus command terminal |
| `Ctrl+W` | Close the active editor tab |
| `F5` | Refresh file explorer |

---

## Menu Reference

| Menu | Key Items |
|---|---|
| **File** | Open Workspace, Setup New, Save, Close, Exit |
| **View** | Toggle Explorer, Chat, Terminal; Refresh |
| **Editors** | Edit Actors, Loops, Wrappers, Runbooks, Config; View Logs; Close All |
| **Workspace** | Reload Actors, List Actors, Info, Verify, Cleanup, Open Folder |

---

## Auto-Setup

If a workspace already exists at the exe's directory (`.wally/` folder with `wally-config.json`), Wally.Forms **automatically loads it on launch**. No manual setup needed for subsequent runs.

---

## Using Without GitHub Copilot

After setup, create a custom wrapper:

1. Open the **Command Terminal** and run:
   ```
   add-wrapper OllamaChat -d "Local Ollama" -e ollama -t "run {model} {prompt}"
   ```
2. Or use **Editors ? Edit Wrappers** to create one visually

---

## Next Steps

- See the full [Wally.Forms README](README.md) for complete panel, editor, and theme documentation
- See the [root README](../README.md) for the overall project documentation
- See the [Wally.Console README](../Wally.Console/README.md) for the CLI command reference
- See the [Wally.Core README](../Wally.Core/README.md) for the library API
- Use the command terminal's `tutorial` command for a step-by-step walkthrough
