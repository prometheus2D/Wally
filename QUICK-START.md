# Wally — Quick Setup Guide

Get Wally running in under 5 minutes.

---

## Prerequisites

| Requirement | Version | Check |
|---|---|---|
| .NET SDK | 8.0+ | `dotnet --version` |
| GitHub CLI | Latest | `gh --version` |
| GH Copilot Extension | Latest | `gh copilot --version` |
| Windows | 10/11 | (Required for WinForms GUI; CLI works cross-platform) |

> **Don't have GitHub Copilot?** You can configure a different LLM wrapper after setup. See [Custom Wrappers](#step-5-optional-custom-llm-wrappers).

---

## Step 1: Clone & Build

```sh
git clone https://github.com/prometheus2D/Wally.git
cd Wally
dotnet build
```

---

## Step 2: Set Up a Workspace

Point Wally at the codebase you want to work with. This creates a `.wally/` folder inside it with actors, loops, wrappers, templates, and config.

**CLI:**
```sh
dotnet run --project Wally.Console -- setup C:\repos\MyApp
cd C:\repos\MyApp
```

**GUI:**
```sh
dotnet run --project Wally.Forms
# Then: File ? Setup New Workspace ? select your codebase root
```

After setup, the wally exe is copied to your codebase root so you can run:
```sh
.\wally info
.\wally list
```

---

## Step 3: Run Your First Prompt

**Direct mode** (no actor — prompt sent as-is):
```sh
.\wally run "What does this codebase do?"
```

**With an actor** (adds RBA persona context):
```sh
.\wally run "Review the auth module for security issues" -a Engineer
.\wally run "Write requirements for the search feature" -a BusinessAnalyst
.\wally run "Define success criteria for the payment system" -a Stakeholder
```

**In the GUI:** Select an actor in the chat panel dropdown, type your prompt, and click Send.

---

## Step 4: Explore What's Available

```sh
.\wally list              # See all actors and their prompts
.\wally list-loops        # See loop definitions
.\wally list-wrappers     # See LLM wrappers
.\wally list-runbooks     # See runbook definitions
.\wally info              # Full workspace info
.\wally tutorial          # Step-by-step tutorial
.\wally help              # Full command reference
```

---

## Step 5 (Optional): Custom LLM Wrappers

By default, Wally uses GitHub Copilot via `gh copilot`. To use a different LLM backend:

```sh
# Create a wrapper for Ollama
.\wally add-wrapper OllamaChat -d "Local Ollama" -e ollama -t "run {model} {prompt}"

# Or edit the JSON directly:
# .wally/Wrappers/OllamaChat.json
```

Wrapper JSON format:
```json
{
  "Name": "OllamaChat",
  "Description": "Local Ollama instance",
  "Executable": "ollama",
  "ArgumentTemplate": "run {model} {prompt}",
  "ModelArgFormat": "{model}",
  "SourcePathArgFormat": "",
  "UseSourcePathAsWorkingDirectory": false,
  "CanMakeChanges": false
}
```

---

## Step 6 (Optional): Custom Actors

Create actors tailored to your project:

```sh
.\wally add-actor SecurityAuditor \
  -r "You are a security auditor specializing in web application security" \
  -c "Identify all OWASP Top 10 vulnerabilities with severity ratings" \
  -i "Produce a comprehensive security assessment report"
```

Or create the folder and JSON manually:
```
.wally/Actors/SecurityAuditor/actor.json
```

---

## Step 7 (Optional): Iterative Loops

Run an actor in a loop for deeper analysis:

```sh
# Use a built-in loop
.\wally run "Review the data layer" -a Engineer -l CodeReview

# Inline loop mode
.\wally run "Refactor error handling" -a Engineer --loop -n 5
```

---

## Step 8 (Optional): Runbooks

Chain multiple commands into repeatable workflows:

```sh
# Create a runbook
.\wally add-runbook my-review -d "Custom review pipeline"

# Edit .wally/Runbooks/my-review.wrb:
#   # Custom review pipeline
#   run "{userPrompt}" -a Stakeholder
#   run "{userPrompt}" -a Engineer -l CodeReview

# Run it
.\wally runbook my-review "Review the authentication system"
```

---

## Verify Your Setup

```sh
.\wally setup --verify
```

This checks the workspace structure and reports any issues.

---

## Next Steps

- Read the [full README](README.md) for detailed documentation
- Run `.\wally tutorial` for an in-depth walkthrough
- Explore the [Wally.Core README](Wally.Core/README.md) for API documentation
- Check out document templates in `.wally/Templates/` for structured output formats
- Add documentation files to `.wally/Docs/` — they'll be included in the LLM context

---

## Troubleshooting

| Issue | Solution |
|---|---|
| `gh: command not found` | Install [GitHub CLI](https://cli.github.com/) |
| `gh copilot: unknown command` | Run `gh extension install github/gh-copilot` |
| `No workspace loaded` | Run `wally setup <path>` or `wally load <path>` |
| `Actor not found` | Check `wally list` — names are case-insensitive |
| `Wrapper not found` | Check `wally list-wrappers` — ensure the JSON exists in `Wrappers/` |
| Build errors | Ensure .NET 8 SDK is installed: `dotnet --version` |
