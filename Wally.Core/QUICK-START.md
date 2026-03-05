# Wally.Core — Quick Setup Guide

Embed the Wally engine in your own .NET 8 application.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) — verify: `dotnet --version`

---

## Add a Reference

**Project reference** (within the Wally solution):

```xml
<ProjectReference Include="..\Wally.Core\Wally.Core.csproj" />
```

**Or build and reference the DLL directly:**

```sh
cd Wally
dotnet build Wally.Core -c Release
# Output: Wally.Core/bin/Release/net8.0/Wally.Core.dll
```

---

## Minimal Integration (5 Lines)

```csharp
using Wally.Core;

// 1. Create the environment
var env = new WallyEnvironment();

// 2. Set up or load a workspace
env.SetupLocal(@"C:\repos\MyApp");   // Scaffolds .wally/ if needed, then loads

// 3. Run a prompt through an actor
var results = env.RunActor("Review error handling", "Engineer");
foreach (var r in results) Console.WriteLine(r);

// 4. Cleanup
env.Logger.Dispose();
```

---

## Common Operations

### Direct Mode (No Actor)

```csharp
string response = env.ExecutePrompt("What does this codebase do?");
```

### Run with a Specific Actor

```csharp
var actor = env.GetActor("Engineer");
string response = env.ExecuteActor(actor!, "Review the auth module");
```

### Run All Actors

```csharp
var responses = env.RunActors("Summarize the architecture");
```

### Override Model or Wrapper

```csharp
string response = env.ExecuteActor(actor!, "Review auth",
    modelOverride: "claude-sonnet-4",
    wrapperOverride: "AutoCopilot");
```

### Use the Command Dispatcher

The same dispatcher used by the CLI and GUI:

```csharp
// Parse and dispatch any Wally command
string[] args = WallyCommands.SplitArgs("run \"Review auth\" -a Engineer -l CodeReview");
WallyCommands.DispatchCommand(env, args);
```

### Load an Existing Workspace

```csharp
env.LoadWorkspace(@"C:\repos\MyApp\.wally");
```

### Access Workspace Entities

```csharp
var actors   = env.Actors;          // List<Actor>
var loops    = env.Loops;           // List<WallyLoopDefinition>
var runbooks = env.Runbooks;        // List<WallyRunbook>
var wrappers = env.Workspace!.LlmWrappers;  // List<LLMWrapper>
var config   = env.Workspace!.Config;        // WallyConfig
```

### Run a Loop

```csharp
var loopDef = env.GetLoop("CodeReview");
var loop = WallyLoop.FromDefinition(
    loopDef!,
    userPrompt: "Review the data layer",
    actorAction: prompt => env.ExecuteActor(actor!, prompt),
    fallbackMaxIterations: 5);

loop.Run();

foreach (var result in loop.Results)
    Console.WriteLine(result);
Console.WriteLine($"Stopped: {loop.StopReason}");
```

### Run a Runbook

```csharp
WallyCommands.HandleRunbook(env, "full-analysis", "Explain the architecture");
```

---

## Key Types at a Glance

| Type | Namespace | Purpose |
|---|---|---|
| `WallyEnvironment` | `Wally.Core` | Runtime orchestration — workspace + actors + wrappers |
| `WallyCommands` | `Wally.Core` | All command implementations + shared dispatcher |
| `WallyWorkspace` | `Wally.Core` | Workspace layout & loading |
| `WallyConfig` | `Wally.Core` | Configuration model (`wally-config.json`) |
| `Actor` | `Wally.Core.Actors` | RBA prompt pipeline (Role, Criteria, Intent) |
| `LLMWrapper` | `Wally.Core.Providers` | JSON-driven CLI wrapper for LLM backends |
| `WallyLoop` | `Wally.Core` | Iterative execution loop |
| `WallyLoopDefinition` | `Wally.Core` | Serializable loop definition (JSON) |
| `WallyRunbook` | `Wally.Core` | Runbook model & parser (`.wrb` files) |
| `SessionLogger` | `Wally.Core.Logging` | Structured JSON logger with rotation |
| `Role` | `Wally.Core.RBA` | Role prompt component |
| `AcceptanceCriteria` | `Wally.Core.RBA` | Acceptance criteria component |
| `Intent` | `Wally.Core.RBA` | Intent prompt component |

---

## Default Workspace Template

When `SetupLocal()` is called, the `Default/` folder shipped with the library is copied into the new workspace. It includes:

- **3 actors:** Engineer, BusinessAnalyst, Stakeholder
- **4 loops:** SingleRun, CodeReview, Refactor, RequirementsDeepDive
- **2 wrappers:** Copilot (read-only), AutoCopilot (agentic)
- **2 runbooks:** hello-world, full-analysis
- **7 templates:** Architecture, Requirements, Execution Plan, Proposal, Implementation Plan, Bug, Test Plan
- **Docs & Logs** directories

The `Default/` folder is included via `<Content Include="Default\**\*" CopyToOutputDirectory="PreserveNewest" />` in the `.csproj`, so it ships automatically with any project referencing Wally.Core.

---

## Next Steps

- See the full [Wally.Core README](README.md) for detailed API documentation on every type
- See the [root README](../README.md) for the overall project documentation
- See the [Wally.Console README](../Wally.Console/README.md) for the CLI reference
- See the [Wally.Forms README](../Wally.Forms/README.md) for the GUI documentation
